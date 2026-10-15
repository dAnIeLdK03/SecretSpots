using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Admin;

// Promotes a config-driven list of emails to Admin on every startup. Idempotent (only touches
// rows that aren't already Admin) and a one-shot task, not a recurring background loop — unlike
// TokenCleanupService (same folder-adjacent pattern), this doesn't need to run on a timer.
public class AdminBootstrapService(
    IServiceScopeFactory scopeFactory,
    IOptions<AdminBootstrapOptions> options,
    ILogger<AdminBootstrapService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var emails = options.Value.Emails;
        if (emails.Count == 0)
        {
            return;
        }

        // Users.Email is stored already Trim().ToLowerInvariant()'d (see Login/Register) — match
        // that exact normalization here so the comparison below works without a SQL-side ToLower.
        var normalizedEmails = emails
            .Select(e => e.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var promoted = await db.Users
            .Where(u => normalizedEmails.Contains(u.Email) && u.Role != UserRole.Admin)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.Role, UserRole.Admin), cancellationToken);

        if (promoted > 0)
        {
            logger.LogInformation(AdminLogMessages.BootstrapAdminsPromoted, promoted);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
