using System.Security.Cryptography;
using SecretSpots.Domain;
using SecretSpots.Features.Common.ExternalAuth;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Auth;

public static class BeginExternalAuth
{
    public record Command(ExternalAuthProvider Provider) : IRequest<Result<string>>;

    public class Handler(IAppDbContext db, IEnumerable<IExternalAuthProvider> providers)
        : IRequestHandler<Command, Result<string>>
    {
        public async Task<Result<string>> Handle(Command command, CancellationToken cancellationToken)
        {
            var provider = providers.Single(p => p.Provider == command.Provider);

            var transaction = new ExternalAuthTransaction
            {
                Id = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                    .Replace('+', '-').Replace('/', '_').TrimEnd('='),
                Provider = command.Provider,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
            };

            db.ExternalAuthTransactions.Add(transaction);
            await db.SaveChangesAsync(cancellationToken);

            return Result<string>.Success(provider.GetAuthorizeUrl(transaction.Id));
        }
    }
}
