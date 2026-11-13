using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.CheckIns;
using SecretSpots.Features.Common.ExceptionHandling;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.CheckIns;

public class CreateCheckInValidatorTests
{
    private static readonly Guid ValidatorUserId = Guid.NewGuid();

    private readonly CreateCheckIn.Validator _validator =
        new(TestLocalizerFactory.Create(), TestOptionsFactory.R2(), new FakeUserContext(ValidatorUserId));

    private static readonly string ValidPhotoUrl = $"{TestOptionsFactory.PhotoPublicBaseUrl}/{ValidatorUserId}/a.jpg";

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/a.jpg")]
    [InlineData("https://example.com/a.jpg")] // valid http(s) URL, but not this app's own photo storage
    public void PhotoUrl_is_invalid(string photoUrl)
    {
        var result = _validator.TestValidate(
            new CreateCheckIn.Command(Guid.NewGuid(), photoUrl, 42.6977, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.PhotoUrl);
    }

    [Fact]
    public void PhotoUrl_belonging_to_a_different_user_is_invalid()
    {
        var someoneElsesPhotoUrl = $"{TestOptionsFactory.PhotoPublicBaseUrl}/{Guid.NewGuid()}/a.jpg";

        var result = _validator.TestValidate(
            new CreateCheckIn.Command(Guid.NewGuid(), someoneElsesPhotoUrl, 42.6977, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.PhotoUrl);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Latitude_out_of_range(double latitude)
    {
        var result = _validator.TestValidate(
            new CreateCheckIn.Command(Guid.NewGuid(), ValidPhotoUrl, latitude, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.Latitude);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Longitude_out_of_range(double longitude)
    {
        var result = _validator.TestValidate(
            new CreateCheckIn.Command(Guid.NewGuid(), ValidPhotoUrl, 42.6977, longitude));
        result.ShouldHaveValidationErrorFor(c => c.Longitude);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(
            new CreateCheckIn.Command(Guid.NewGuid(), ValidPhotoUrl, 42.6977, 23.3219));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateCheckInHandlerTests
{
    private const double SofiaLatitude = 42.6977;
    private const double SofiaLongitude = 23.3219;

    // ~130km from Sofia (same "far" point used in SearchNearbySpotsTests) — unambiguously
    // outside any realistic MaxDistanceMeters threshold.
    private const double PlovdivLatitude = 42.1354;
    private const double PlovdivLongitude = 24.7453;

    private static async Task<Spot> SeedSpotAsync(IAppDbContext db)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = $"Spot-{Guid.NewGuid():N}",
            Description = "test",
            Category = SpotCategory.Nature,
            PhotoUrls = ["https://example.com/photo.jpg"],
            Location = new Point(SofiaLongitude, SofiaLatitude) { SRID = 4326 },
            CreatedByUserId = Guid.NewGuid(),
        };

        db.Spots.Add(spot);
        await db.SaveChangesAsync();

        return spot;
    }

    private static CreateCheckIn.Handler CreateHandler(
        IAppDbContext db, Guid userId, TimeProvider? time = null) =>
        new(
            db,
            new FakeUserContext(userId),
            TestOptionsFactory.CheckIn(),
            time ?? TimeProvider.System,
            TestLocalizerFactory.Create(),
            NullLogger<CreateCheckIn.Handler>.Instance);

    [Fact]
    public async Task Successful_checkin_persists_the_checkin()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await SeedSpotAsync(db);
        var user = await TestUserFactory.SeedAsync(db, $"checkin-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");

        var handler = CreateHandler(db, user.Id);
        var command = new CreateCheckIn.Command(
            spot.Id, "https://example.com/proof.jpg", SofiaLatitude, SofiaLongitude);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);

        var savedCheckIn = await db.CheckIns.SingleAsync(c => c.Id == result.Value.Id);
        Assert.Equal(spot.Id, savedCheckIn.SpotId);
        Assert.Equal(user.Id, savedCheckIn.UserId);

        var ownerNotification = await db.Notifications.SingleAsync(n => n.UserId == spot.CreatedByUserId);
        Assert.Equal(NotificationType.NewVisitorOnYourSpot, ownerNotification.Type);
        Assert.Equal(spot.Id, ownerNotification.RelatedSpotId);
    }

    [Fact]
    public async Task Checkin_at_nonexistent_spot_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"checkin-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");

        var handler = CreateHandler(db, user.Id);
        var command = new CreateCheckIn.Command(
            Guid.NewGuid(), "https://example.com/proof.jpg", SofiaLatitude, SofiaLongitude);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckInsMessageKeys.SpotNotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Checkin_too_far_from_spot_is_rejected_and_nothing_is_persisted()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await SeedSpotAsync(db);
        var user = await TestUserFactory.SeedAsync(db, $"checkin-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");

        var handler = CreateHandler(db, user.Id);
        var command = new CreateCheckIn.Command(
            spot.Id, "https://example.com/proof.jpg", PlovdivLatitude, PlovdivLongitude);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckInsMessageKeys.TooFarFromSpot, result.Error.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Error.StatusCode);

        Assert.False(await db.CheckIns.AnyAsync(c => c.SpotId == spot.Id));
    }

    [Fact]
    public async Task Checkin_for_a_user_missing_from_the_database_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await SeedSpotAsync(db);

        var handler = CreateHandler(db, Guid.NewGuid());
        var command = new CreateCheckIn.Command(
            spot.Id, "https://example.com/proof.jpg", SofiaLatitude, SofiaLongitude);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CommonMessageKeys.CurrentUserNotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task A_second_checkin_at_the_same_spot_waits_for_the_cooldown()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await SeedSpotAsync(db);
        var user = await TestUserFactory.SeedAsync(db, $"cooldown-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");
        // Starts at the real clock: the check-in's CreatedAt is still stamped by AppDbContext.
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var handler = CreateHandler(db, user.Id, time: time);
        var command = new CreateCheckIn.Command(spot.Id, "https://example.com/proof.jpg", SofiaLatitude, SofiaLongitude);
        Assert.True((await handler.Handle(command, CancellationToken.None)).IsSuccess);

        time.Advance(TimeSpan.FromHours(23));
        var tooSoon = await handler.Handle(command, CancellationToken.None);
        time.Advance(TimeSpan.FromHours(2));
        var afterCooldown = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(CheckInsMessageKeys.TooSoonSinceLastCheckIn, tooSoon.Error.Code);
        Assert.True(afterCooldown.IsSuccess);
    }
}
