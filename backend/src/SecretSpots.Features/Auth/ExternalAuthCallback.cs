using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.ExternalAuth;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Auth;

public static class ExternalAuthCallback
{
    public record Command(ExternalAuthProvider Provider, string? Code, string? State, string? Error)
        : IRequest<Result<string>>;

    public class Handler(
        IAppDbContext db,
        IEnumerable<IExternalAuthProvider> providers,
        IOptions<ExternalAuthOptions> externalAuthOptions,
        IOptions<CrystalsOptions> crystalsOptions,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<string>>
    {
        public async Task<Result<string>> Handle(Command command, CancellationToken cancellationToken)
        {
            if (command.Error is not null || command.Code is null || command.State is null)
            {
                logger.LogWarning(AuthLogMessages.ExternalAuthDenied, command.Provider);
                return Result<string>.Success(ErrorRedirect("cancelled"));
            }

            var transaction = await db.ExternalAuthTransactions
                .SingleOrDefaultAsync(t => t.Id == command.State, cancellationToken);

            var stateValid = transaction is { Status: ExternalAuthTransactionStatus.Pending }
                && transaction.Provider == command.Provider
                && transaction.ExpiresAt > DateTimeOffset.UtcNow;

            if (!stateValid)
            {
                logger.LogWarning(AuthLogMessages.ExternalAuthInvalidState, command.Provider);
                return Result<string>.Success(ErrorRedirect("invalid_state"));
            }

            var provider = providers.Single(p => p.Provider == command.Provider);

            ExternalAuthUserInfo userInfo;
            try
            {
                userInfo = await provider.ExchangeCodeAsync(command.Code, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                logger.LogWarning(ex, AuthLogMessages.ExternalAuthProviderExchangeFailed, command.Provider);
                return Result<string>.Success(ErrorRedirect("provider_error"));
            }

            // Without an email we can't match this login to an existing account, and creating one
            // would produce a duplicate (or an empty-email user that collides on the unique index).
            // Returning users already linked to this provider are unaffected.
            var isKnownLogin = await db.ExternalLogins.AnyAsync(
                l => l.Provider == command.Provider && l.ProviderUserId == userInfo.ProviderUserId, cancellationToken);
            if (!isKnownLogin && string.IsNullOrWhiteSpace(userInfo.Email))
            {
                logger.LogWarning(AuthLogMessages.ExternalAuthEmailMissing, command.Provider);
                return Result<string>.Success(ErrorRedirect("email_required"));
            }

            var user = await FindOrCreateUserAsync(userInfo, command.Provider, cancellationToken);

            transaction!.Status = ExternalAuthTransactionStatus.Completed;
            transaction.UserId = user.Id;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(AuthLogMessages.ExternalAuthCompleted, user.Id, command.Provider);
            return Result<string>.Success($"{externalAuthOptions.Value.FrontendCallbackUrl}?code={transaction.Id}");
        }

        private async Task<User> FindOrCreateUserAsync(
            ExternalAuthUserInfo userInfo, ExternalAuthProvider provider, CancellationToken cancellationToken)
        {
            var existingLogin = await db.ExternalLogins
                .SingleOrDefaultAsync(
                    l => l.Provider == provider && l.ProviderUserId == userInfo.ProviderUserId,
                    cancellationToken);

            if (existingLogin is not null)
            {
                return await db.Users.SingleAsync(u => u.Id == existingLogin.UserId, cancellationToken);
            }

            var normalizedEmail = userInfo.Email.Trim().ToLowerInvariant();

            User? user = null;
            if (userInfo.EmailVerified)
            {
                user = await db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
            }

            if (user is null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = normalizedEmail,
                    DisplayName = userInfo.DisplayName,
                    IsEmailVerified = userInfo.EmailVerified,
                };
                db.Users.Add(user);
                db.CrystalWallets.Add(new CrystalWallet { UserId = user.Id, Balance = crystalsOptions.Value.StartingBalance });
            }

            db.ExternalLogins.Add(new ExternalLogin
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = provider,
                ProviderUserId = userInfo.ProviderUserId,
                Email = normalizedEmail,
            });

            await db.SaveChangesAsync(cancellationToken);
            return user;
        }

        private string ErrorRedirect(string code) =>
            $"{externalAuthOptions.Value.FrontendCallbackUrl}?error={code}";
    }
}
