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
using SquareBuddy;
using SquareBuddy.Configuration;
using SquareBuddy.Core.Conversations;
using SquareBuddy.Data;
using SquareBuddy.Data.Entities;
using SquareBuddy.Consumers;
using SquareBuddy.TelegramIntegration;

public class Program
{
    private static readonly Guid AdminUserId = Guid.Parse("52299db7-d2bc-4ab3-9dc5-d9dadd40c37d");
    private static readonly Guid SubscriptionId = Guid.Parse("81c0b65c-1325-48a3-9389-2369173dff7a");
    private static readonly Guid AgentId = Guid.Parse("b4099979-fceb-41e1-bfb6-135f3ccb1701");
    private static readonly string TelegramBotToken = "7592736264:AAGpsXEe03dUe3O5WWCjDYtemWmpwvCoFVE";

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
        var connectionString = builder.Configuration.GetConnectionString("squarebuddydb");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'postgres' not found.");
        }

        builder.Services.AddDbContext<MainDataContext>(o => o
            .UseNpgsql(connectionString, npgsqlBuilder =>
                {
                    npgsqlBuilder.MigrationsAssembly("SquareBuddy.Initializer");
                    npgsqlBuilder.MigrationsHistoryTable($"{Constants.TablePrefix}_migration_history");
                })
        // .UseModel(SquareBuddy.Data.CompiledModels.MainDataContextModel.Instance)
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
                cfg.Host(rabbitConnectionString);

                // 2. The magic flag: Tells MassTransit NOT to start consuming messages
                cfg.DeployTopologyOnly = true;

                cfg.ConfigureEndpoints(context);
            });
        });

        #endregion

        builder.AddTelegram();

        var host = builder.Build();

        // 1. Define the modern Resilience Pipeline
        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                // Handle database-specific connection errors
                ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>(),
                MaxRetryAttempts = 10,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .Build();

        // 2. Execute migrations through the pipeline
        await pipeline.ExecuteAsync(async token =>
        {
            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            // 1. Database Migrations
            Console.WriteLine("Attempting to apply migrations...");
            var db = scope.ServiceProvider.GetRequiredService<MainDataContext>();
            await db.Database.MigrateAsync(token);
            Console.WriteLine("Migrations complete.");

            // 2. Admin user seed
            string adminEmail = builder.Configuration["ADMIN_EMAIL"] ?? "admin@squarebuddy.ai";
            string adminPassword = builder.Configuration["ADMIN_PASSWORD"] ?? "admin@squarebuddy.ai";

            await SeedAdminUserAsync(services, adminEmail, adminPassword, token);

            await DeployRabbitMqTopology(services);

            // 4. Seed default dev data 
            await SeedBasicData(services, token);
        });

        Console.WriteLine("Migrations complete.");
        return 0;
    }

    private static async Task DeployRabbitMqTopology(IServiceProvider services)
    {
        var busControl = services.GetRequiredService<IBusControl>();

        try
        {
            // This connects to RabbitMQ, creates all exchanges/queues/bindings, and returns
            await busControl.DeployAsync();
            Console.WriteLine("RabbitMQ Topology successfully deployed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to deploy RabbitMQ Topology: {ex.Message}");
            throw;
        }
    }

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
        var dbContext = services.GetRequiredService<MainDataContext>();
        var telegramService = services.GetRequiredService<ITelegramService>();

        await telegramService.RegisterWebhookAsync(TelegramBotToken, ct);

        if (dbContext.Subscriptions.Any())
        {
            return;
        }

        var agent = new Agent
        {
            Id = Program.AgentId,
            Name = "Alan",
            Description = "Sales agent for propane partners",
            TelegramBotToken = TelegramBotToken
        };

        var subscription = new Subscription
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
}

