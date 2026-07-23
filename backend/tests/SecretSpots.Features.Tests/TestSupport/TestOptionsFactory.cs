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

    public static IOptions<CheckInOptions> CheckIn(
        double maxDistanceMeters = 75, int defaultPageSize = 20, int maxPageSize = 100) =>
        Options.Create(new CheckInOptions
        {
            MaxDistanceMeters = maxDistanceMeters,
            DefaultPageSize = defaultPageSize,
            MaxPageSize = maxPageSize,
        });

    public static IOptions<PhotoOptions> Photo(
        long maxFileSizeBytes = 10 * 1024 * 1024, int maxDimensionPixels = 1920, int webpQuality = 80) =>
        Options.Create(new PhotoOptions
        {
            MaxFileSizeBytes = maxFileSizeBytes,
            MaxDimensionPixels = maxDimensionPixels,
            WebpQuality = webpQuality,
        });

    public static IOptions<NotificationsOptions> Notifications(
        int defaultPageSize = 20, int maxPageSize = 100, double newSpotRadiusKm = 5, int readRetentionHours = 24) =>
        Options.Create(new NotificationsOptions
        {
            DefaultPageSize = defaultPageSize,
            MaxPageSize = maxPageSize,
            NewSpotRadiusKm = newSpotRadiusKm,
            ReadRetentionHours = readRetentionHours,
        });
}
