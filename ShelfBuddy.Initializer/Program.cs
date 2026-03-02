using MassTransit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Polly;
using Polly.Retry;
using ShelfBuddy;
using ShelfBuddy.Configuration;
using ShelfBuddy.Consumers;
using ShelfBuddy.Core.Conversations;
using ShelfBuddy.Data;
using ShelfBuddy.Data.Entities;
using ShelfBuddy.TelegramIntegration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class Program
{
    //private static readonly Guid AdminUserId = Guid.Parse("52299db7-d2bc-4ab3-9dc5-d9dadd40c37d");
    //private static readonly Guid SubscriptionId = Guid.Parse("81c0b65c-1325-48a3-9389-2369173dff7a");
    // private static readonly Guid AgentId = Guid.Parse("b4099979-fceb-41e1-bfb6-135f3ccb1701");
    // private static readonly string TelegramBotToken = "7592736264:AAGpsXEe03dUe3O5WWCjDYtemWmpwvCoFVE";
    private const int DatabaseMaxRetryAttempts = 10;
    private const int RabbitMaxRetryAttempts = 8;
    private static readonly TimeSpan DatabaseRetryDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RabbitRetryDelay = TimeSpan.FromSeconds(1);

    private static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration
            .AddYamlFile("./config.yaml", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables();

        builder.Services.AddServiceDiscovery();
        AppOptions appOptions = builder.Configuration.TryGetAppOptions();
        builder.Services.AddSingleton(appOptions);
        builder.Services.AddScoped<IConversationStore, ConversationStore>();

        #region Database and Migrations 

        // Standard .NET way to get a connection string. 
        // Aspire injects "ConnectionStrings__postgres", so we look for "postgres".
        var connectionString = builder.Configuration.GetConnectionString("shelfbuddy");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'postgres' not found.");
        }

        builder.Services.AddDbContext<MainDataContext>(o => o
            .UseNpgsql(connectionString, npgsqlBuilder =>
                {
                    npgsqlBuilder.MigrationsAssembly("ShelfBuddy.Initializer");
                    npgsqlBuilder.MigrationsHistoryTable($"{Constants.TablePrefix}_migration_history");
                })
        // .UseModel(ShelfBuddy.Data.CompiledModels.MainDataContextModel.Instance)
        );

        #endregion

        #region Identity

        builder.Services.AddDataProtection()
            .PersistKeysToDbContext<MainDataContext>();

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<MainDataContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        builder.Services
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
        });

        // for development phase use simple passwords
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 1;
                options.Password.RequiredUniqueChars = 0;
            });
        }

        #endregion

        #region RabbitMQ

        builder.Services.AddMassTransit(x =>
        {
            x.SetKebabCaseEndpointNameFormatter();

            // 1. Register the EXACT same consumers here as you do in your Web API.
            // This allows MassTransit to calculate the required queues and exchanges.
            x.AddConsumer<IncomingMessageConsumer>();
            x.AddConsumer<OutgoingTelegramMessageConsumer>();

            

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitConnectionString = builder.Configuration.GetConnectionString("rabbitmq");
                var vhostName = builder.Configuration["RABBITMQ_VHOST"] ?? "shelfbuddy";

                var uriBuilder = new UriBuilder(rabbitConnectionString);
                // uriBuilder.Path = vhostName;

                // cfg.Host(rabbitConnectionString);
                cfg.Host(new Uri($"{uriBuilder}/{vhostName}"));

                // 2. The magic flag: Tells MassTransit NOT to start consuming messages
                cfg.DeployTopologyOnly = true;

                cfg.ConfigureEndpoints(context);
            });
        });

        #endregion

        builder.AddTelegramServices();

        var host = builder.Build();

        ResiliencePipeline databasePipeline = BuildDatabasePipeline();
        ResiliencePipeline rabbitTopologyPipeline = BuildRabbitTopologyPipeline();

        using CancellationTokenSource startupCts = new();

        Task databaseLaneTask = ExecuteDatabaseLaneAsync(
            host.Services,
            builder.Configuration,
            databasePipeline,
            startupCts.Token);

        Task rabbitLaneTask = ExecuteRabbitLaneAsync(
            host.Services,
            rabbitTopologyPipeline,
            startupCts.Token);

        await WaitForStartupLanesAsync(databaseLaneTask, rabbitLaneTask, startupCts);

        Console.WriteLine("Initializer startup complete.");
        return 0;
    }

    private static ResiliencePipeline BuildDatabasePipeline()
    {
        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>(),
                MaxRetryAttempts = DatabaseMaxRetryAttempts,
                Delay = DatabaseRetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = static args =>
                {
                    Console.WriteLine(
                        $"[DB] Retry {args.AttemptNumber + 1} after {args.RetryDelay}. Cause: {args.Outcome.Exception?.Message}");
                    return default;
                }
            })
            .Build();

        return pipeline;
    }

    private static ResiliencePipeline BuildRabbitTopologyPipeline()
    {
        ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(exception => exception is not OperationCanceledException),
                MaxRetryAttempts = RabbitMaxRetryAttempts,
                Delay = RabbitRetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = static args =>
                {
                    Console.WriteLine(
                        $"[Rabbit] Retry {args.AttemptNumber + 1} after {args.RetryDelay}. Cause: {args.Outcome.Exception?.Message}");
                    return default;
                }
            })
            .Build();

        return pipeline;
    }

    private static async Task ExecuteDatabaseLaneAsync(
        IServiceProvider rootServices,
        IConfiguration configuration,
        ResiliencePipeline pipeline,
        CancellationToken cancellationToken)
    {
        await pipeline.ExecuteAsync(async token =>
        {
            using IServiceScope scope = rootServices.CreateScope();
            IServiceProvider services = scope.ServiceProvider;

            Console.WriteLine("[DB] Starting database lane.");

            MainDataContext db = services.GetRequiredService<MainDataContext>();
            Console.WriteLine("[DB] Applying migrations...");
            await db.Database.MigrateAsync(token);
            Console.WriteLine("[DB] Migrations complete.");

            string adminEmail = configuration["ADMIN_EMAIL"] ?? "admin@shelfbuddy.ai";
            string adminPassword = configuration["ADMIN_PASSWORD"] ?? "admin@shelfbuddy.ai";

            //Console.WriteLine("[DB] Seeding admin user...");
            //await SeedAdminUserAsync(services, adminEmail, adminPassword, token);

            //Console.WriteLine("[DB] Seeding basic data and registering Telegram webhook...");
            //await SeedBasicData(services, token);

            Console.WriteLine("[DB] Database lane complete.");
        }, cancellationToken);
    }

    private static async Task ExecuteRabbitLaneAsync(
        IServiceProvider rootServices,
        ResiliencePipeline pipeline,
        CancellationToken cancellationToken)
    {
        await pipeline.ExecuteAsync(async token =>
        {
            using IServiceScope scope = rootServices.CreateScope();
            IServiceProvider services = scope.ServiceProvider;

            Console.WriteLine("[Rabbit] Starting topology lane.");
            await DeployRabbitMqTopologyAsync(services, token);
            Console.WriteLine("[Rabbit] Topology lane complete.");
        }, cancellationToken);
    }

    private static async Task WaitForStartupLanesAsync(
        Task databaseLaneTask,
        Task rabbitLaneTask,
        CancellationTokenSource startupCts)
    {
        Task[] laneTasks = [databaseLaneTask, rabbitLaneTask];

        try
        {
            await Task.WhenAll(laneTasks);
        }
        catch
        {
            startupCts.Cancel();

            try
            {
                await Task.WhenAll(laneTasks);
            }
            catch
            {
                // Swallow to inspect lane task exceptions below and throw a clearer startup error.
            }

            Exception? databaseError = databaseLaneTask.Exception?.GetBaseException();
            Exception? rabbitError = rabbitLaneTask.Exception?.GetBaseException();

            if (databaseError != null && rabbitError != null)
            {
                throw new AggregateException(
                    "Initializer startup failed in database and RabbitMQ lanes.",
                    databaseError,
                    rabbitError);
            }

            if (databaseError != null)
            {
                throw new InvalidOperationException(
                    "Initializer startup failed in database lane.",
                    databaseError);
            }

            if (rabbitError != null)
            {
                throw new InvalidOperationException(
                    "Initializer startup failed in RabbitMQ topology lane.",
                    rabbitError);
            }

            throw;
        }
    }

    private static async Task EnsureVirtualHostExistsAsync(
        string managementUrl, string username, string password, string vhost, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();

        var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);

        var encodedVhost = Uri.EscapeDataString(vhost);

        // 1. Create the Virtual Host
        var vhostResponse = await client.PutAsync(
            $"{managementUrl}/api/vhosts/{encodedVhost}",
            null,
            cancellationToken);

        vhostResponse.EnsureSuccessStatusCode();

        // 2. Grant permissions
        var permissions = new { configure = ".*", write = ".*", read = ".*" };
        var content = new StringContent(JsonSerializer.Serialize(permissions), Encoding.UTF8, "application/json");

        var permResponse = await client.PutAsync(
            $"{managementUrl}/api/permissions/{encodedVhost}/{username}",
            content,
            cancellationToken);

        permResponse.EnsureSuccessStatusCode();
    }

    private static async Task DeployRabbitMqTopologyAsync(
        IServiceProvider services, CancellationToken cancellationToken)
    {
        try
        {
            var configuration = services.GetRequiredService<IConfiguration>();

            var managementUrl = configuration["RABBITMQ_MANAGEMENTURL"];
            var rabbitUser = configuration["RABBITMQ_USER"];
            var rabbitPass = configuration["RABBITMQ_PASS"];
            var vhostName = configuration["RABBITMQ_VHOST"] ?? "shelfbuddy"; // Fallback just in case

            // 2. Ensure the Virtual Host exists BEFORE MassTransit connects
            if (!string.IsNullOrEmpty(managementUrl) && !string.IsNullOrEmpty(rabbitUser) && !string.IsNullOrEmpty(rabbitPass))
            {
                Console.WriteLine($"[Rabbit] Ensuring virtual host '{vhostName}' exists via Management API...");
                await EnsureVirtualHostExistsAsync(
                    managementUrl, rabbitUser, rabbitPass, vhostName, cancellationToken);
            }
            else
            {
                Console.WriteLine("[Rabbit] Warning: Management API configuration missing. Skipping VHost creation.");
            }

            Console.WriteLine("[Rabbit] Deploying MassTransit topology...");

            IBusControl busControl = services.GetRequiredService<IBusControl>();

            await busControl.DeployAsync(cancellationToken);
            Console.WriteLine("[Rabbit] Topology successfully deployed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Rabbit] Failed to deploy topology: {ex.Message}");
            throw;
        }
    }

    /*
    private static async Task SeedAdminUserAsync(
           IServiceProvider services,
           string adminEmail,
           string adminPassword,
           CancellationToken cancellationToken)
    {
        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        ApplicationUser? existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser != null)
        {
            return;
        }

        ApplicationUser adminUser = new ApplicationUser
        {
            Id = Program.AdminUserId,
            DisplayName = "Admin",
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        IdentityResult createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            string errorSummary = string.Join("; ", createResult.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Admin user seeding failed: {errorSummary}");
        }
    }

    private static async Task SeedBasicData(IServiceProvider services, CancellationToken ct)
    {
        MainDataContext dbContext = services.GetRequiredService<MainDataContext>();
        ITelegramService telegramService = services.GetRequiredService<ITelegramService>();

        await telegramService.RegisterWebhookAsync(TelegramBotToken, ct);

        if (dbContext.Subscriptions.Any())
        {
            return;
        }

        Agent agent = new Agent
        {
            Id = Program.AgentId,
            Name = "Alan",
            Description = "Sales agent for propane partners",
            TelegramBotToken = TelegramBotToken
        };

        Subscription subscription = new Subscription
        {
            Id = Program.SubscriptionId,
            SubscriptionCreditBalance = 10000,
            TopUpCreditBalance = 0,
        };

        subscription.Agents.Add(agent);

        subscription.SubscriptionUsers.Add(new SubscriptionUser
        {
            UserId = Program.AdminUserId,
            SubscriptionId = Program.SubscriptionId,
            Role = SubscriptionUserRole.Owner
        });

        dbContext.Subscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

    }
    */
}

