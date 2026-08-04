using SecretSpots.Domain;

namespace SecretSpots.Features.Common.ExternalAuth;


public interface IExternalAuthProvider
{
    ExternalAuthProvider Provider { get; }
    string GetAuthorizeUrl(string state);
    Task<ExternalAuthUserInfo> ExchangeCodeAsync(string code, CancellationToken cancellationToken);
}