using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Tests.TestSupport;

internal static class TestOptionsFactory
{
    public static IOptions<JwtOptions> Jwt() => Options.Create(new JwtOptions
    {
        Secret = "test-secret-test-secret-test-secret-32-bytes-minimum!",
        Issuer = "SecretSpots.Tests",
        Audience = "SecretSpots.Tests.Clients",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 30,
    });

    public static IOptions<CrystalsOptions> Crystals(int startingBalance = 0) =>
        Options.Create(new CrystalsOptions { StartingBalance = startingBalance });
}
