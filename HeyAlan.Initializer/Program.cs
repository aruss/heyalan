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
using HeyAlan;
using HeyAlan.Email;
using HeyAlan.Configuration;
using HeyAlan.Data;
using HeyAlan.Data.Entities;
using HeyAlan.Messaging;
using HeyAlan.Newsletter;
using HeyAlan.TelegramIntegration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Wolverine;
using Wolverine.RabbitMQ;
public class Program
{
    //private static readonly Guid AdminUserId = Guid.Parse("52299db7-d2bc-4ab3-9dc5-d9dadd40c37d");
    //private static readonly Guid SubscriptionId = Guid.Parse("81c0b65c-1325-48a3-9389-2369173dff7a");
    // private static readonly Guid AgentId = Guid.Parse("b4099979-fceb-41e1-bfb6-135f3ccb1701");
    // private static readonly string TelegramBotToken = "7592736264:AAGpsXEe03dUe3O5WWCjDYtemWmpwvCoFVE";
    private const int DatabaseMaxRetryAttempts = 10;
    private const int RabbitMaxRetryAttempts = 1;
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
        builder.AddMessagingServices();

        #region Database and Migrations 

        // Standard .NET way to get a connection string. 
        // Aspire injects "ConnectionStrings__postgres", so we look for "postgres".
        var connectionString = builder.Configuration.GetConnectionString("heyalan");

        if (String.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'postgres' not found.");
        }

        builder.Services.AddDbContext<MainDataContext>(o => o
            .UseNpgsql(connectionString, npgsqlBuilder =>
                {
                    npgsqlBuilder.MigrationsAssembly("HeyAlan.Initializer");
                    npgsqlBuilder.MigrationsHistoryTable($"{Constants.TablePrefix}_migration_history");
                })
        // .UseModel(HeyAlan.Data.CompiledModels.MainDataContextModel.Instance)
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

        string? rabbitConnectionString = builder.Configuration.GetConnectionString("rabbitmq");
        if (String.IsNullOrWhiteSpace(rabbitConnectionString))
        {
            throw new InvalidOperationException("RabbitMQ connection string 'rabbitmq' is missing.");
        }

        RabbitMqStartupGuard.EnsureReachable(rabbitConnectionString, TimeSpan.FromSeconds(3));

        builder.UseWolverine(options =>
        {
            options.UseRabbitMq(rabbitConnectionString).AutoProvision();

            options.ListenToRabbitQueue("email-send-requested");
            options.ListenToRabbitQueue("incoming-message");
            options.ListenToRabbitQueue("telegram-outgoing-messages");
            options.ListenToRabbitQueue("newsletter-subscription");

            options.PublishMessage<EmailSendRequested>().ToRabbitQueue("email-send-requested");
            options.PublishMessage<IncomingMessage>().ToRabbitQueue("incoming-message");
            options.PublishMessage<OutgoingTelegramMessage>().ToRabbitQueue("telegram-outgoing-messages");
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
            host,
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

            //string adminEmail = configuration["ADMIN_EMAIL"] ?? "admin@heyalan.ai";
            //string adminPassword = configuration["ADMIN_PASSWORD"] ?? "admin@heyalan.ai";

            //Console.WriteLine("[DB] Seeding admin user...");
            //await SeedAdminUserAsync(services, adminEmail, adminPassword, token);

            //Console.WriteLine("[DB] Seeding basic data and registering Telegram webhook...");
            //await SeedBasicData(services, token);

            Console.WriteLine("[DB] Database lane complete.");
        }, cancellationToken);
    }

    private static async Task ExecuteRabbitLaneAsync(
        IHost host,
        ResiliencePipeline pipeline,
        CancellationToken cancellationToken)
    {
        await pipeline.ExecuteAsync(async token =>
        {
            Console.WriteLine("[Rabbit] Starting topology lane.");
            await DeployRabbitMqTopologyAsync(host, token);
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
        IHost host, CancellationToken cancellationToken)
    {
        try
        {
            IServiceProvider services = host.Services;
            var configuration = services.GetRequiredService<IConfiguration>();

            var managementUrl = configuration["RABBITMQ_MANAGEMENT_URL"];
            var rabbitUser = configuration["RABBITMQ_USER"];
            var rabbitPass = configuration["RABBITMQ_PASS"];
            var vhostName = configuration["RABBITMQ_VHOST"] ?? "heyalan"; // Fallback just in case

            // 2. Ensure the Virtual Host exists BEFORE Wolverine connects
            if (!String.IsNullOrWhiteSpace(managementUrl) && !String.IsNullOrWhiteSpace(rabbitUser) && !String.IsNullOrWhiteSpace(rabbitPass))
            {
                Console.WriteLine($"[Rabbit] Ensuring virtual host '{vhostName}' exists via Management API...");
                await EnsureVirtualHostExistsAsync(
                    managementUrl, rabbitUser, rabbitPass, vhostName, cancellationToken);
            }
            else
            {
                Console.WriteLine("[Rabbit] Warning: Management API configuration missing. Skipping VHost creation.");
            }

            Console.WriteLine("[Rabbit] Deploying Wolverine resources...");
            await host.StartAsync(cancellationToken);
            await host.StopAsync(cancellationToken);
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


