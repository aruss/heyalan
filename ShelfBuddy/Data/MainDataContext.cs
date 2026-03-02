namespace ShelfBuddy.Data;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ShelfBuddy.Data.Entities;
using ShelfBuddy;

public class MainDataContext :
    IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IDataProtectionKeyContext
{
    public MainDataContext(DbContextOptions<MainDataContext> options) : base(options)
    {

    }

    public DbSet<Subscription> Subscriptions { get; set; } = null!;

    public DbSet<SubscriptionUser> SubscriptionUsers { get; set; } = null!;

    public DbSet<CreditTransaction> CreditTransactions { get; set; } = null!;

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public DbSet<Agent> Agents { get; set; } = null!;

    public DbSet<Conversation> Conversations { get; set; } = null!;

    public DbSet<ConversationMessage> ConversationMessages { get; set; } = null!;

    public DbSet<SubscriptionSquareConnection> SubscriptionSquareConnections { get; set; } = null!;

    public DbSet<SubscriptionOnboardingState> SubscriptionOnboardingStates { get; set; } = null!;

    public DbSet<SubscriptionOnboardingStepState> SubscriptionOnboardingStepStates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        #region Data Protection Keys

        builder.Entity<DataProtectionKey>();

        #endregion

        #region Identity

        builder.Entity<ApplicationRole>();

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.DisplayName).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
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
                .HasMany(e => e.OnboardingStepStates)
                .WithOne(e => e.Subscription)
                .HasForeignKey(e => e.SubscriptionId)
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

        builder.Entity<SubscriptionOnboardingStepState>(entity =>
        {
            entity.HasKey(e => new { e.SubscriptionId, e.Step });
            entity.Property(e => e.Step).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CompletedAt).IsRequired(false);
            entity.Property(e => e.SkippedAt).IsRequired(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.SubscriptionId);
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

        foreach (var entity in builder.Model.GetEntityTypes())
        {
            // Prefix Table Name
            var currentTableName = entity.GetTableName();
            entity.SetTableName($"{prefix}_{currentTableName?.ToSnakeCase()}");

            // Prefix Primary Key Constraints
            foreach (var key in entity.GetKeys())
            {
                key.SetName($"{prefix}_{key.GetName()?.ToSnakeCase()}");
            }

            // Prefix Foreign Key Constraints
            foreach (var fk in entity.GetForeignKeys())
            {
                fk.SetConstraintName($"{prefix}_{fk.GetConstraintName()?.ToSnakeCase()}");
            }

            // Prefix Indexes (Addresses your previous RoleNameIndex error)
            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName($"{prefix}_{index.GetDatabaseName()?.ToSnakeCase()}");
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
