using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Minio;
using Minio.DataModel.Args;
using Npgsql;
using Polly;
using Polly.Retry;
using SquareBuddy;
using SquareBuddy.Configuration;
using SquareBuddy.Data;
using SquareBuddy.Data.Entities;

public class Program
{
    private static readonly Guid AdminUserId = Guid.Parse("06d51587-a7f0-45b6-a27d-a04850a597cd");
    private static readonly Guid SubscriptionId = Guid.Parse("37721905-a317-44bf-b9ca-c2defcab11ea");
    private static readonly Guid BoardId = Guid.Parse("37f68864-9557-4cf7-adac-53eb909be65a");
    private static readonly Guid BoardConfigId = Guid.Parse("d2290da0-79fc-422d-add2-81164bd9fcad");

    private static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Configuration
            .AddYamlFile("./config.yaml", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables();

        builder.Services.AddServiceDiscovery();

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

        #region Minio

        MinioOptions minioOptions = builder.Configuration.TryGetMinioOptions();

        builder.Services.AddScoped(sp =>
        {
            Uri endpoint = minioOptions.Endpoint!;

            return new MinioClient()
                .WithEndpoint(endpoint.Authority)
                .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
                .WithSSL(endpoint.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                .Build();
        });

        #endregion

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

            // 3. MinIO Initialization
            var minio = services.GetRequiredService<IMinioClient>();
            Console.WriteLine($"Checking bucket '{minioOptions.Bucket}'...");

            bool found = await minio.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(minioOptions.Bucket),
                token
            );

            if (!found)
            {
                Console.WriteLine($"Bucket '{minioOptions.Bucket}' not found. Creating...");
                await minio.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(minioOptions.Bucket),
                    token
                );
                Console.WriteLine($"Bucket '{minioOptions.Bucket}' created.");
            }
            else
            {
                Console.WriteLine($"Bucket '{minioOptions.Bucket}' already exists.");
            }

            // 4. Seed default dev data 
            await SeedBasicData(services, token);
        });

        Console.WriteLine("Migrations complete.");
        return 0;
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

    private static async Task SeedBasicData(IServiceProvider services,  CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<MainDataContext>();

        if (dbContext.Subscriptions.Any())
        {
            return;
        }

        var board = new Board
        {
            Id = Program.BoardId,
            Name = "Default Board",
            SubscriptionId = Program.SubscriptionId,
        };

        board.Configs.Add(new BoardConfig
        {
            Id = Program.BoardConfigId,
            BoardId = Program.BoardId,
            AgeGroup = AgeGroup.OneToThree,
            Language = "de",
        });

        var subscription = new Subscription
        {
            Id = Program.SubscriptionId,
            SubscriptionCreditBalance = 10000,
            TopUpCreditBalance = 0,
        };

        subscription.Boards.Add(board);


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

