namespace BuyAlan.Data;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Wolverine.EntityFrameworkCore;
using BuyAlan.Data.Entities;
using BuyAlan;

public class MainDataContext :
    IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IDataProtectionKeyContext
{
    private const string WolverineSchema = "wolverine";

    public MainDataContext(DbContextOptions<MainDataContext> options) : base(options)
    {

    }

    public DbSet<Subscription> Subscriptions { get; set; } = null!;

    public DbSet<SubscriptionUser> SubscriptionUsers { get; set; } = null!;

    public DbSet<SubscriptionInvitation> SubscriptionInvitations { get; set; } = null!;

    public DbSet<CreditTransaction> CreditTransactions { get; set; } = null!;

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public DbSet<Agent> Agents { get; set; } = null!;

    public DbSet<Conversation> Conversations { get; set; } = null!;

    public DbSet<ConversationMessage> ConversationMessages { get; set; } = null!;

    public DbSet<SubscriptionSquareConnection> SubscriptionSquareConnections { get; set; } = null!;

    public DbSet<SubscriptionOnboardingState> SubscriptionOnboardingStates { get; set; } = null!;

    public DbSet<SubscriptionCatalogSyncState> SubscriptionCatalogSyncStates { get; set; } = null!;

    public DbSet<SubscriptionCatalogProduct> SubscriptionCatalogProducts { get; set; } = null!;

    public DbSet<SubscriptionCatalogProductLocation> SubscriptionCatalogProductLocations { get; set; } = null!;

    public DbSet<SquareWebhookReceipt> SquareWebhookReceipts { get; set; } = null!;

    public DbSet<AgentCatalogProductAccess> AgentCatalogProductAccesses { get; set; } = null!;

    public DbSet<AgentSalesZipCode> AgentSalesZipCodes { get; set; } = null!;

    public DbSet<SmsConsent> SmsConsents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        #region Wolverine

        builder.MapWolverineEnvelopeStorage(WolverineSchema);

        #endregion

        #region Data Protection Keys

        builder.Entity<DataProtectionKey>();

        #endregion

        #region Identity

        builder.Entity<ApplicationRole>();

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.DisplayName).IsRequired();
            entity.Property(e => e.ActiveSubscriptionId).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.ActiveSubscriptionId);

            entity
                .HasOne(e => e.ActiveSubscription)
                .WithMany()
                .HasForeignKey(e => e.ActiveSubscriptionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        #endregion

        builder.Entity<SubscriptionUser>(entity =>
        {
            entity.HasKey(e => new { e.SubscriptionId, e.UserId });
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.HasIndex(e => e.UserId);

            entity
                .HasOne(e => e.Subscription)
                .WithMany(e => e.SubscriptionUsers)
                .HasForeignKey(e => e.SubscriptionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.User)
                .WithMany(e => e.SubscriptionUsers)
                .HasForeignKey(e => e.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SubscriptionInvitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.SentAtUtc).IsRequired();
            entity.Property(e => e.AcceptedAtUtc).IsRequired(false);
            entity.Property(e => e.RevokedAtUtc).IsRequired(false);
            entity.Property(e => e.ExpiresAtUtc).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => new { e.SubscriptionId, e.Email });
            entity.HasIndex(e => e.InvitedByUserId);

            entity
                .HasOne(e => e.Subscription)
                .WithMany(e => e.Invitations)
                .HasForeignKey(e => e.SubscriptionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.InvitedByUser)
                .WithMany()
                .HasForeignKey(e => e.InvitedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Application entities 
        builder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.SubscriptionCreditBalance).IsRequired();
            entity.Property(e => e.TopUpCreditBalance).IsRequired();

            entity
                .HasMany(e => e.Agents)
                .WithOne(e => e.Subscription)
                .HasForeignKey(e => e.SubscriptionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.SquareConnection)
                .WithOne(e => e.Subscription)
                .HasForeignKey<SubscriptionSquareConnection>(e => e.SubscriptionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.OnboardingState)
                .WithOne(e => e.Subscription)
                .HasForeignKey<SubscriptionOnboardingState>(e => e.SubscriptionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.CatalogSyncState)
                .WithOne(e => e.Subscription)
                .HasForeignKey<SubscriptionCatalogSyncState>(e => e.SubscriptionId)
                .HasConstraintName("fk_subscription_catalog_sync_state_subscription")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CreditTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.Source).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.StripeEventId).IsUnique();

            entity
                .HasOne(e => e.Subscription)
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => new { e.SubscriptionId, e.Id });
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.Property(e => e.Personality).IsRequired(false);
            entity.Property(e => e.Description).IsRequired(false);
            entity.Property(e => e.PersonalityPromptRaw).IsRequired(false);
            entity.Property(e => e.PersonalityPromptSanitized).IsRequired(false);

            entity.Property(e => e.TwilioPhoneNumber).IsRequired(false);
            entity.Property(e => e.TelegramBotToken).IsRequired(false);
            entity.Property(e => e.WhatsappNumber).IsRequired(false);

            entity.HasIndex(e => e.TelegramBotToken)
                .HasFilter("\"TelegramBotToken\" IS NOT NULL")
                .IsUnique();

            entity.HasIndex(e => e.TwilioPhoneNumber)
                .HasFilter("\"TwilioPhoneNumber\" IS NOT NULL");

            entity.HasIndex(e => e.WhatsappNumber)
                .HasFilter("\"WhatsappNumber\" IS NOT NULL");
        });

        builder.Entity<SubscriptionSquareConnection>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);
            entity.Property(e => e.SquareMerchantId).IsRequired();
            entity.Property(e => e.EncryptedAccessToken).IsRequired();
            entity.Property(e => e.EncryptedRefreshToken).IsRequired();
            entity.Property(e => e.AccessTokenExpiresAtUtc).IsRequired();
            entity.Property(e => e.Scopes).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.DisconnectedAtUtc).IsRequired(false);

            entity.HasIndex(e => e.SubscriptionId).IsUnique();
            entity.HasIndex(e => e.SquareMerchantId);

            entity
                .HasOne(e => e.ConnectedByUser)
                .WithMany()
                .HasForeignKey(e => e.ConnectedByUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SubscriptionOnboardingState>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CurrentStep).IsRequired();
            entity.Property(e => e.PrimaryAgentId).IsRequired(false);
            entity.Property(e => e.StartedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.CompletedAt).IsRequired(false);

            entity.HasIndex(e => e.SubscriptionId).IsUnique();

            entity
                .HasOne(e => e.PrimaryAgent)
                .WithMany()
                .HasForeignKey(e => e.PrimaryAgentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<SubscriptionCatalogSyncState>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId);
            entity.Property(e => e.LastSyncedBeginTimeUtc).IsRequired(false);
            entity.Property(e => e.NextScheduledSyncAtUtc).IsRequired(false);
            entity.Property(e => e.LastSyncStartedAtUtc).IsRequired(false);
            entity.Property(e => e.LastSyncCompletedAtUtc).IsRequired(false);
            entity.Property(e => e.LastTriggerSource).IsRequired(false);
            entity.Property(e => e.SyncInProgress).IsRequired();
            entity.Property(e => e.PendingResync).IsRequired();
            entity.Property(e => e.LastErrorCode).IsRequired(false);
            entity.Property(e => e.LastErrorMessage).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.SubscriptionId, e.NextScheduledSyncAtUtc });
        });

        builder.Entity<SubscriptionCatalogProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => new { e.SubscriptionId, e.Id });
            entity.Property(e => e.SquareItemId).IsRequired();
            entity.Property(e => e.SquareVariationId).IsRequired();
            entity.Property(e => e.ItemName).IsRequired();
            entity.Property(e => e.VariationName).IsRequired();
            entity.Property(e => e.Description).IsRequired(false);
            entity.Property(e => e.Sku).IsRequired(false);
            entity.Property(e => e.BasePriceAmount).IsRequired(false);
            entity.Property(e => e.BasePriceCurrency).IsRequired(false);
            entity.Property(e => e.IsSellable).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired();
            entity.Property(e => e.SquareUpdatedAtUtc).IsRequired(false);
            entity.Property(e => e.SquareVersion).IsRequired(false);
            entity.Property(e => e.SearchText).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.SubscriptionId, e.SquareVariationId })
                .HasDatabaseName("ix_catalog_product_subscription_variation")
                .IsUnique();
            entity.HasIndex(e => new { e.SubscriptionId, e.SquareItemId })
                .HasDatabaseName("ix_catalog_product_subscription_item");
            entity.HasIndex(e => new { e.SubscriptionId, e.IsDeleted, e.IsSellable, e.ItemName, e.Id })
                .HasDatabaseName("ix_catalog_product_subscription_active_name");
            entity.HasIndex(e => new { e.SubscriptionId, e.SearchText })
                .HasDatabaseName("ix_catalog_product_subscription_search");

            entity
                .HasOne(e => e.Subscription)
                .WithMany(e => e.CatalogProducts)
                .HasForeignKey(e => e.SubscriptionId)
                .HasConstraintName("fk_catalog_product_subscription")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SubscriptionCatalogProductLocation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SquareVariationId).IsRequired();
            entity.Property(e => e.LocationId).IsRequired();
            entity.Property(e => e.PriceOverrideAmount).IsRequired(false);
            entity.Property(e => e.PriceOverrideCurrency).IsRequired(false);
            entity.Property(e => e.IsAvailableForSale).IsRequired();
            entity.Property(e => e.IsSoldOut).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.SubscriptionId, e.SquareVariationId, e.LocationId })
                .HasDatabaseName("ix_catalog_product_location_variation_location")
                .IsUnique();
            entity.HasIndex(e => new { e.SubscriptionId, e.LocationId, e.Id })
                .HasDatabaseName("ix_catalog_product_location_location");
            entity.HasIndex(e => new { e.SubscriptionId, e.SubscriptionCatalogProductId })
                .HasDatabaseName("ix_catalog_product_location_product");

            entity
                .HasOne(e => e.Subscription)
                .WithMany(e => e.CatalogProductLocations)
                .HasForeignKey(e => e.SubscriptionId)
                .HasConstraintName("fk_catalog_product_location_subscription")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.SubscriptionCatalogProduct)
                .WithMany(e => e.Locations)
                .HasForeignKey(e => new { e.SubscriptionId, e.SubscriptionCatalogProductId })
                .HasPrincipalKey(e => new { e.SubscriptionId, e.Id })
                .HasConstraintName("fk_catalog_product_location_product")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SquareWebhookReceipt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventId).IsRequired();
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.MerchantId).IsRequired();
            entity.Property(e => e.ReceivedAtUtc).IsRequired();
            entity.Property(e => e.IsProcessed).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasIndex(e => new { e.SubscriptionId, e.ReceivedAtUtc, e.Id });
            entity.HasIndex(e => new { e.SubscriptionId, e.IsProcessed, e.ReceivedAtUtc });

            entity
                .HasOne(e => e.Subscription)
                .WithMany(e => e.SquareWebhookReceipts)
                .HasForeignKey(e => e.SubscriptionId)
                .HasConstraintName("fk_square_webhook_receipt_subscription")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AgentCatalogProductAccess>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.SubscriptionId, e.AgentId, e.SubscriptionCatalogProductId })
                .HasDatabaseName("ix_agent_catalog_access_agent_product")
                .IsUnique();
            entity.HasIndex(e => new { e.SubscriptionId, e.AgentId, e.Id })
                .HasDatabaseName("ix_agent_catalog_access_agent");
            entity.HasIndex(e => new { e.SubscriptionId, e.SubscriptionCatalogProductId, e.AgentId })
                .HasDatabaseName("ix_agent_catalog_access_product");

            entity
                .HasOne(e => e.Agent)
                .WithMany(e => e.CatalogProductAccesses)
                .HasForeignKey(e => new { e.SubscriptionId, e.AgentId })
                .HasPrincipalKey(e => new { e.SubscriptionId, e.Id })
                .HasConstraintName("fk_agent_catalog_access_agent")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.SubscriptionCatalogProduct)
                .WithMany(e => e.AgentProductAccesses)
                .HasForeignKey(e => new { e.SubscriptionId, e.SubscriptionCatalogProductId })
                .HasPrincipalKey(e => new { e.SubscriptionId, e.Id })
                .HasConstraintName("fk_agent_catalog_access_product")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AgentSalesZipCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ZipCodeNormalized).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.SubscriptionId, e.AgentId, e.ZipCodeNormalized })
                .HasDatabaseName("ix_agent_sales_zip_agent_zip")
                .IsUnique();
            entity.HasIndex(e => new { e.SubscriptionId, e.AgentId, e.Id })
                .HasDatabaseName("ix_agent_sales_zip_agent");

            entity
                .HasOne(e => e.Agent)
                .WithMany(e => e.SalesZipCodes)
                .HasForeignKey(e => new { e.SubscriptionId, e.AgentId })
                .HasPrincipalKey(e => new { e.SubscriptionId, e.Id })
                .HasConstraintName("fk_agent_sales_zip_agent")
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SmsConsent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhoneNumber).IsRequired();
            entity.Property(e => e.TransactionalConsent).IsRequired();
            entity.Property(e => e.MarketingConsent).IsRequired();
            entity.Property(e => e.ConsentSource).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        builder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParticipantExternalId).IsRequired();
            entity.Property(e => e.Channel).IsRequired();
            entity.Property(e => e.LastMessagePreview).IsRequired(false);
            entity.Property(e => e.LastMessageAt).IsRequired(false);
            entity.Property(e => e.LastMessageRole).IsRequired(false);
            entity.Property(e => e.UnreadCount).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.AgentId, e.ParticipantExternalId, e.Channel }).IsUnique();
            entity.HasIndex(e => new { e.AgentId, e.LastMessageAt, e.Id })
                .IsDescending(false, true, true);
            entity.HasIndex(e => new { e.AgentId, e.UnreadCount });

            entity
                .HasOne(e => e.Agent)
                .WithMany()
                .HasForeignKey(e => e.AgentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ConversationMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.From).IsRequired();
            entity.Property(e => e.To).IsRequired();
            entity.Property(e => e.OccurredAt).IsRequired();
            entity.Property(e => e.IsRead).IsRequired();
            entity.Property(e => e.ReadAt).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => new { e.ConversationId, e.OccurredAt, e.Id })
                .IsDescending(false, true, true);
            entity.HasIndex(e => new { e.ConversationId, e.IsRead, e.Role });

            entity
                .HasOne(e => e.Conversation)
                .WithMany(e => e.Messages)
                .HasForeignKey(e => e.ConversationId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.Agent)
                .WithMany()
                .HasForeignKey(e => e.AgentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Apply postgres nameing convetion to all table names and primary keys and indexes 
        this.ApplyPostgresNamingConvention(builder);
    }

    public void ApplyPostgresNamingConvention(ModelBuilder builder)
    {
        string prefix = Constants.TablePrefix.ToSnakeCase();
        if (!String.IsNullOrWhiteSpace(prefix)) {
            prefix = $"{prefix}_";
        }

        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (String.Equals(entity.GetSchema(), WolverineSchema, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Prefix Table Name
            var currentTableName = entity.GetTableName();
            entity.SetTableName($"{prefix}{currentTableName?.ToSnakeCase()}");

            // Prefix Primary Key Constraints
            foreach (var key in entity.GetKeys())
            {
                key.SetName($"{prefix}{key.GetName()?.ToSnakeCase()}");
            }

            // Prefix Foreign Key Constraints
            foreach (var fk in entity.GetForeignKeys())
            {
                fk.SetConstraintName($"{prefix}{fk.GetConstraintName()?.ToSnakeCase()}");
            }

            // Prefix Indexes (Addresses your previous RoleNameIndex error)
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName($"{prefix}{index.GetDatabaseName()?.ToSnakeCase()}");
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.OnBeforeSave();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override int SaveChanges()
    {
        this.OnBeforeSave();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        this.OnBeforeSave();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void OnBeforeSave()
    {
        var now = DateTime.UtcNow;

        foreach (EntityEntry? entry in ChangeTracker.Entries())
        {
            // Use 'is' pattern matching which is highly AOT optimized
            if (entry.State == EntityState.Added && entry.Entity is IEntityWithId idEntity)
            {
                if (idEntity.Id == Guid.Empty)
                {
                    idEntity.Id = Guid.NewGuid();
                }
            }

            if (entry.Entity is IEntityWithAudit auditEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditEntity.CreatedAt = now;
                }

                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    auditEntity.UpdatedAt = now;
                }
            }
        }
    }
}
