namespace SquareBuddy.Data;
SquareBuddySquareBuddy
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SquareBuddy.Data.Entities;
using SquareBuddy.Shared;

public class MainDataContext :
    IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IDataProtectionKeyContext
{
    public MainDataContext(DbContextOptions<MainDataContext> options) : base(options)
    {

    }

    public DbSet<Board> Boards => Set<Board>();

    public DbSet<BoardConfig> BoardConfigs => Set<BoardConfig>();

    public DbSet<StoryRequest> StoryRequests => Set<StoryRequest>();

    public DbSet<StoryRequestChunk> StoryRequestChunks => Set<StoryRequestChunk>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<SubscriptionUser> SubscriptionUsers => Set<SubscriptionUser>();

    public DbSet<CreditTransaction> CreditTransactions => Set<CreditTransaction>();

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

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
                .HasMany(e => e.Boards)
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

        builder.Entity<Board>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity
                .HasMany(e => e.Stories)
                .WithOne(e => e.Board)
                .HasForeignKey(e => e.BoardId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasMany(e => e.Configs)
                .WithOne(e => e.Board)
                .HasForeignKey(e => e.BoardId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BoardConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AgeGroup).IsRequired();
            entity.Property(e => e.Language).IsRequired();
            entity.Property(e => e.Voice);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.ProducerUserPrompt);
            entity.Property(e => e.EvaluatorUserPrompt);
            entity.Property(e => e.ProducerUserPromptCompiled);
            entity.Property(e => e.EvaluatorUserPromptCompiled);

            entity
                .HasMany(e => e.Stories)
                .WithOne(e => e.Config)
                .HasForeignKey(e => e.ConfigId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasIndex(e => new { e.BoardId, e.CreatedAt }) // Composite Index
                .IsDescending(false, true); // Sort CreatedAt Descending
        });

        builder.Entity<StoryRequest>(entity =>
        {
            // entity.ToTable($"{Constants.TablePrefix}_stories");
            entity.HasKey(e => e.Id);

            entity
                .Property(e => e.Status)
                .IsRequired();

            entity
                .Property(e => e.Input)
                .HasColumnType("text")
                .IsRequired();

            entity
                .Property(e => e.SceneGraph)
                .HasColumnType("text")
                .IsRequired();

            entity
                .Property(e => e.Title)
                .HasColumnType("text")
                .IsRequired();

            entity
                .Property(e => e.CreatedWith)
                .HasColumnType("text")
                .IsRequired();

            entity
                .Property(e => e.Duration)
                .HasColumnType("integer")
                .IsRequired();

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.BoardId, e.CreatedAt });

            entity
                .HasMany(e => e.Chunks)
                .WithOne(e => e.StoryRequest)
                .HasForeignKey(e => e.StoryRequestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // This explicitly tells EF Core (even in AOT) that no shadow properties exist,
            // preventing it from trying to build a dynamic factory.
            entity.Ignore("TempId"); // Dummy ignore to ensure metadata is touched
        });

        builder.Entity<StoryRequestChunk>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Sequence).IsRequired();

            entity
                .Property(e => e.Text)
                .HasColumnType("text")
                .IsRequired();

            entity
                .Property(e => e.AudioObjectKey)
                .HasColumnType("text")
                .IsRequired();

            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.StoryRequestId);
            entity.HasIndex(e => new { e.StoryRequestId, e.Sequence }).IsUnique();
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
