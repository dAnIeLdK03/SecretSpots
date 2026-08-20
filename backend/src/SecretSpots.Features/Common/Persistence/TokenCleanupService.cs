using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SecretSpots.Features.Common.Persistence;

// RefreshTokens and PasswordResetTokens are never deleted anywhere else, so both tables grow
// forever. Runs on a timer for the lifetime of the process rather than as a one-shot scheduled
// job, since the app has no external scheduler infrastructure.
public class TokenCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<TokenCleanupOptions> options,
    ILogger<TokenCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(options.Value.IntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var now = DateTimeOffset.UtcNow;

        // A revoked/used token can never become usable again (see the isUsable checks in
        // RefreshAccessToken/ResetPassword) — deleting it doesn't need to wait for its original
        // expiry too. Without this, every refresh-token rotation left the superseded row sitting
        // around for up to RefreshTokenDays (30 days by default) before this service touched it.
        var deletedRefreshTokens = await db.RefreshTokens
            .Where(t => t.ExpiresAt < now || t.RevokedAt != null)
            .ExecuteDeleteAsync(cancellationToken);

        var deletedResetTokens = await db.PasswordResetTokens
            .Where(t => t.ExpiresAt < now || t.UsedAt != null)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedRefreshTokens > 0 || deletedResetTokens > 0)
        {
            logger.LogInformation(
                TokenCleanupLogMessages.TokensCleanedUp, deletedRefreshTokens, deletedResetTokens);
        }
    }
}
