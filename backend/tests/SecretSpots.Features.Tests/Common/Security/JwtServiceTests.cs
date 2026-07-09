using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Common.Security;

public class JwtServiceTests
{
    [Fact]
    public void GenerateAccessToken_produces_a_token_with_the_expected_claims()
    {
        var jwtOptions = TestOptionsFactory.Jwt();
        var jwtService = new JwtService(jwtOptions);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "jwt-test@example.com",
            PasswordHash = "irrelevant",
            DisplayName = "Jwt Test",
        };

        var (token, expiresAt) = jwtService.GenerateAccessToken(user);

        // Must match the MapInboundClaims = false set on JwtBearerOptions in Program.cs —
        // otherwise "sub" gets silently remapped to the legacy ClaimTypes.NameIdentifier URI.
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Value.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Value.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Value.Secret)),
            ClockSkew = TimeSpan.Zero,
        }, out _);

        Assert.Equal(user.Id.ToString(), principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);
        Assert.Equal(user.Email, principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value);
        Assert.True(expiresAt > DateTimeOffset.UtcNow);
        Assert.True(expiresAt <= DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.AccessTokenMinutes).AddSeconds(5));
    }
}
