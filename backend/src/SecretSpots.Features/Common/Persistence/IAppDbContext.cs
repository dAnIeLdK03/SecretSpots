using Microsoft.EntityFrameworkCore;
using SecretSpots.Domain;

namespace SecretSpots.Features.Common.Persistence;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Spot> Spots { get; }
    DbSet<CheckIn> CheckIns { get; }
    DbSet<Business> Businesses { get; }
    DbSet<Reward> Rewards { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
