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

    public static IOptions<CrystalsOptions> Crystals(int startingBalance = 0, int checkInReward = 10) =>
        Options.Create(new CrystalsOptions { StartingBalance = startingBalance, CheckInReward = checkInReward });

    public static IOptions<CheckInOptions> CheckIn(double maxDistanceMeters = 75) =>
        Options.Create(new CheckInOptions { MaxDistanceMeters = maxDistanceMeters });
}
