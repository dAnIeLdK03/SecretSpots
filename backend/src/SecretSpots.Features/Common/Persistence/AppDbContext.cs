using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SecretSpots.Domain;

namespace SecretSpots.Features.Common.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Spot> Spots => Set<Spot>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Reward> Rewards => Set<Reward>();
    public DbSet<RewardRedemption> RewardRedemptions => Set<RewardRedemption>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<SavedSpot> SavedSpots => Set<SavedSpot>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<ExternalAuthTransaction> ExternalAuthTransactions => Set<ExternalAuthTransaction>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Report> Reports => Set<Report>();

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => await Database.BeginTransactionAsync(cancellationToken);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IHasCreatedAt>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Guards CrystalBalance against lost updates from concurrent check-ins/redemptions —
        // xmin is Postgres's built-in row-version system column, so this needs no migration.
        modelBuilder.Entity<User>().UseXminAsConcurrencyToken();

        modelBuilder.Entity<RefreshToken>()
            .HasIndex(r => r.Token)
            .IsUnique();

        modelBuilder.Entity<Spot>()
            .Property(s => s.Location)
            .HasColumnType("geography (Point, 4326)");

        modelBuilder.Entity<Spot>()
            .HasIndex(s => s.Location)
            .HasMethod("GIST");

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.CreatedAt });

        modelBuilder.Entity<Comment>()
            .HasIndex(c => new { c.SpotId, c.CreatedAt });

        modelBuilder.Entity<Rating>()
            .HasIndex(r => new { r.SpotId, r.UserId })
            .IsUnique();

        modelBuilder.Entity<SavedSpot>()
            .HasIndex(s => new { s.SpotId, s.UserId })
            .IsUnique();

        modelBuilder.Entity<SavedSpot>()
            .HasIndex(s => new { s.UserId, s.CreatedAt });

        modelBuilder.Entity<Spot>()
            .HasIndex(s => s.Name)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        modelBuilder.Entity<Spot>()
            .HasIndex(s => s.Description)
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        modelBuilder.Entity<ExternalLogin>()
            .HasIndex(e => new {e.Provider, e.ProviderUserId})
            .IsUnique();

        modelBuilder.Entity<PasswordResetToken>()
            .HasIndex(p => p.Token)
            .IsUnique();

        // One report per user per piece of content — resubmitting doesn't add signal, and this
        // also lets the insert race (two concurrent reports from the same user) resolve as a
        // uniqueness violation instead of a duplicate row.
        modelBuilder.Entity<Report>()
            .HasIndex(r => new { r.ReporterUserId, r.ContentType, r.ContentId })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
