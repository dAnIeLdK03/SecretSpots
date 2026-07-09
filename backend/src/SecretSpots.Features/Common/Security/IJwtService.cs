using SecretSpots.Domain;

namespace SecretSpots.Features.Common.Security;

public interface IJwtService
{
    (string Token, DateTimeOffset ExpiresAt) GenerateAccessToken(User user);
}
