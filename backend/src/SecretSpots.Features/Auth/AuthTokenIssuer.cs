using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

// Shared by Register, Login and RefreshAccessToken — all three end with
// "issue a fresh access + refresh token pair for this user".
internal static class AuthTokenIssuer
{
    public static async Task<Result<AuthResult>> IssueAsync(
        IAppDbContext db,
        IJwtService jwtService,
        IOptions<JwtOptions> jwtOptions,
        User user,
        CancellationToken cancellationToken)
    {
        var (accessToken, accessTokenExpiresAt) = jwtService.GenerateAccessToken(user);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtOptions.Value.RefreshTokenDays),
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<AuthResult>.Success(new AuthResult(accessToken, refreshToken.Token, accessTokenExpiresAt));
    }
}
