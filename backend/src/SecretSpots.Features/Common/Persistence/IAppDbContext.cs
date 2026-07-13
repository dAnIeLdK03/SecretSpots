using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SecretSpots.Domain;

namespace SecretSpots.Features.Common.Persistence;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Spot> Spots { get; }
    DbSet<CheckIn> CheckIns { get; }
    DbSet<Business> Businesses { get; }
    DbSet<Reward> Rewards { get; }
    DbSet<RewardRedemption> RewardRedemptions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
