using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;

namespace SecretSpots.Features.Common.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Spot> Spots => Set<Spot>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Reward> Rewards => Set<Reward>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");
        base.OnModelCreating(modelBuilder);
    }
}
