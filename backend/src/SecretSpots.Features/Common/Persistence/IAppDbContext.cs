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
    DbSet<Notification> Notifications { get; }
    DbSet<Comment> Comments { get; }
    DbSet<Rating> Ratings { get; }
    DbSet<SavedSpot> SavedSpots { get; }
    DbSet<ExternalLogin> ExternalLogins { get; }
    DbSet<ExternalAuthTransaction> ExternalAuthTransactions { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
