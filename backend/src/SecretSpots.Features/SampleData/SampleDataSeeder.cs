using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Storage;

namespace SecretSpots.Features.SampleData;

// Development only: on startup, fills an empty local database with sample users, spots and
// activity so every screen has something on it. Runs once — if any sample user already exists it
// does nothing. Writes straight to the DbContext (not through the handlers) so it can backdate
// rows and skip side effects like "new spot nearby" notifications.
public class SampleDataSeeder(
    IServiceScopeFactory scopeFactory,
    IOptions<SampleDataOptions> options,
    IOptions<CrystalsOptions> crystalsOptions,
    IOptions<SpotModerationOptions> spotModerationOptions,
    ILogger<SampleDataSeeder> logger) : IHostedService
{
    private const int SpotPhotoWidth = 1200;
    private const int SpotPhotoHeight = 800;
    private const int CheckInPhotoWidth = 800;
    private const int CheckInPhotoHeight = 600;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        try
        {
            await SeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // A broken seed (e.g. MinIO not up yet) must not take the whole dev API down with it.
            logger.LogError(ex, SampleDataLogMessages.SeedFailed);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var photoStorage = scope.ServiceProvider.GetRequiredService<IPhotoStorage>();

        var domainSuffix = "@" + SampleDataCatalog.EmailDomain;
        if (await db.Users.AnyAsync(u => u.Email.EndsWith(domainSuffix), cancellationToken))
        {
            logger.LogInformation(SampleDataLogMessages.AlreadySeeded, SampleDataCatalog.EmailDomain);
            return;
        }

        // Fixed seed — the same sample data every time the local database is recreated.
        var random = new Random(20261023);
        var now = DateTimeOffset.UtcNow;

        // One hash for everyone — BCrypt is deliberately slow, and the accounts share a password.
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(options.Value.Password, workFactor: 12);

        var users = SampleDataCatalog.Users
            .Select((sample, i) => new User
            {
                Id = Guid.NewGuid(),
                Email = sample.Email,
                DisplayName = sample.DisplayName,
                PasswordHash = passwordHash,
                IsEmailVerified = true,
                Role = sample.Role,
                CreatedAt = now.AddDays(-90 + i),
            })
            .ToList();
        db.Users.AddRange(users);

        var wallets = users.ToDictionary(
            u => u.Id,
            u => new CrystalWallet { UserId = u.Id, Balance = crystalsOptions.Value.StartingBalance });
        db.CrystalWallets.AddRange(wallets.Values);

        // Admins can't create spots or take part (see CreateSpot/CreateCheckIn/...), so only
        // regular users own and interact with sample content.
        var members = users.Where(u => u.Role == UserRole.User).ToList();

        var spots = new List<Spot>();
        for (var i = 0; i < SampleDataCatalog.Spots.Length; i++)
        {
            var sample = SampleDataCatalog.Spots[i];
            var owner = members[i % members.Count];

            var photoUrls = new List<string>();
            var photoCount = sample.Status == SpotModerationStatus.Rejected ? 1 : random.Next(1, 3);
            for (var p = 0; p < photoCount; p++)
            {
                photoUrls.Add(await UploadPhotoAsync(
                    photoStorage, owner.Id, sample.Category, seed: i * 10 + p, SpotPhotoWidth, SpotPhotoHeight, cancellationToken));
            }

            var spot = new Spot
            {
                Id = Guid.NewGuid(),
                Name = sample.Name,
                Description = sample.Description,
                Category = sample.Category,
                PhotoUrls = photoUrls,
                Location = new Point(sample.Longitude, sample.Latitude) { SRID = 4326 },
                CreatedByUserId = owner.Id,
                ModerationStatus = sample.Status,
                RejectionReason = sample.RejectionReason,
                // Rejected samples get the normal grace period, so they disappear after it like
                // any other rejected spot (RejectedSpotCleanupService).
                ScheduledDeletionAt = sample.Status == SpotModerationStatus.Rejected
                    ? now.AddHours(spotModerationOptions.Value.RejectionGracePeriodHours)
                    : null,
                // Pending/rejected ones are the newest, like a real moderation queue.
                CreatedAt = sample.Status == SpotModerationStatus.Approved
                    ? now.AddDays(-60 + i * 2).AddHours(random.Next(0, 24))
                    : now.AddHours(-random.Next(1, 48)),
            };
            spots.Add(spot);
        }
        db.Spots.AddRange(spots);

        var approvedSpots = spots.Where(s => s.ModerationStatus == SpotModerationStatus.Approved).ToList();

        var ratingCount = 0;
        var commentCount = 0;
        foreach (var spot in approvedSpots)
        {
            var visitors = members.Where(m => m.Id != spot.CreatedByUserId).OrderBy(_ => random.Next()).ToList();

            var raters = visitors.Take(random.Next(2, 6)).ToList();
            var values = raters.Select(_ => random.Next(0, 10) < 7 ? random.Next(4, 6) : 3).ToList();
            for (var r = 0; r < raters.Count; r++)
            {
                db.Ratings.Add(new Rating
                {
                    Id = Guid.NewGuid(),
                    SpotId = spot.Id,
                    UserId = raters[r].Id,
                    Value = values[r],
                    CreatedAt = RandomTimeAfter(spot.CreatedAt, now, random),
                });
            }
            spot.RatingsCount = raters.Count;
            spot.AverageRating = values.Count == 0 ? 0 : values.Average();
            ratingCount += raters.Count;

            foreach (var commenter in visitors.Take(random.Next(0, 4)))
            {
                db.Comments.Add(new Comment
                {
                    Id = Guid.NewGuid(),
                    SpotId = spot.Id,
                    UserId = commenter.Id,
                    Text = SampleDataCatalog.Comments[random.Next(SampleDataCatalog.Comments.Length)],
                    CreatedAt = RandomTimeAfter(spot.CreatedAt, now, random),
                });
                commentCount++;
            }
        }

        // Check-ins mirror CreateCheckIn's crystal rules: the visitor earns CheckInReward, and the
        // spot's creator earns OwnerRewardMin..Max for each first-time visitor.
        var checkInCount = 0;
        var savedCount = 0;
        foreach (var member in members)
        {
            var otherSpots = approvedSpots.Where(s => s.CreatedByUserId != member.Id).OrderBy(_ => random.Next()).ToList();

            foreach (var spot in otherSpots.Take(random.Next(2, 5)))
            {
                var checkIn = new CheckIn
                {
                    Id = Guid.NewGuid(),
                    SpotId = spot.Id,
                    UserId = member.Id,
                    PhotoUrl = await UploadPhotoAsync(
                        photoStorage, member.Id, spot.Category, seed: random.Next(), CheckInPhotoWidth, CheckInPhotoHeight, cancellationToken),
                    CrystalsAwarded = crystalsOptions.Value.CheckInReward,
                    CreatedAt = RandomTimeAfter(spot.CreatedAt, now, random),
                };
                db.CheckIns.Add(checkIn);
                db.CrystalTransactions.Add(new CrystalTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = member.Id,
                    Amount = checkIn.CrystalsAwarded,
                    Reason = CrystalTransactionReason.CheckInReward,
                    RelatedCheckInId = checkIn.Id,
                    CreatedAt = checkIn.CreatedAt,
                });
                wallets[member.Id].Balance += checkIn.CrystalsAwarded;
                wallets[spot.CreatedByUserId].Balance += random.Next(
                    crystalsOptions.Value.OwnerRewardMin, crystalsOptions.Value.OwnerRewardMax + 1);
                checkInCount++;
            }

            foreach (var spot in otherSpots.Skip(4).Take(random.Next(2, 5)))
            {
                db.SavedSpots.Add(new SavedSpot
                {
                    Id = Guid.NewGuid(),
                    SpotId = spot.Id,
                    UserId = member.Id,
                    CreatedAt = RandomTimeAfter(spot.CreatedAt, now, random),
                });
                savedCount++;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            SampleDataLogMessages.Seeded,
            users.Count, spots.Count, ratingCount, commentCount, checkInCount, savedCount, options.Value.Password);
    }

    // Same key layout as UploadPhoto ("{userId}/{guid}.webp"), so the URLs pass the same
    // ownership checks real uploads do and edits/deletions of sample spots work normally.
    private static async Task<string> UploadPhotoAsync(
        IPhotoStorage photoStorage, Guid ownerId, SpotCategory category, int seed, int width, int height, CancellationToken cancellationToken)
    {
        await using var photo = await SamplePhotoGenerator.GenerateAsync(category, seed, width, height, cancellationToken);
        return await photoStorage.UploadAsync(photo, "image/webp", $"{ownerId}/{Guid.NewGuid()}.webp", cancellationToken);
    }

    private static DateTimeOffset RandomTimeAfter(DateTimeOffset start, DateTimeOffset end, Random random) =>
        start + TimeSpan.FromSeconds(random.NextDouble() * (end - start).TotalSeconds);
}
