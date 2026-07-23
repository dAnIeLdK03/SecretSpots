using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Spots;

public class CreateSpotValidatorTests
{
    private readonly CreateSpot.Validator _validator = new(TestLocalizerFactory.Create());

    [Fact]
    public void Name_is_required()
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", 42.6977, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Description_is_required()
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("Name", "", SpotCategory.Nature, "https://example.com/a.jpg", 42.6977, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/a.jpg")]
    public void PhotoUrl_is_invalid(string photoUrl)
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("Name", "Desc", SpotCategory.Nature, photoUrl, 42.6977, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.PhotoUrl);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Latitude_out_of_range(double latitude)
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("Name", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", latitude, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.Latitude);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Longitude_out_of_range(double longitude)
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("Name", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", 42.6977, longitude));
        result.ShouldHaveValidationErrorFor(c => c.Longitude);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("Name", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", 42.6977, 23.3219));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateSpotHandlerTests
{
    private const double SofiaLatitude = 42.6977;
    private const double SofiaLongitude = 23.3219;

    // ~130km from Sofia (same "far" point used in SearchNearbySpotsTests) — unambiguously
    // outside any realistic NewSpotRadiusKm.
    private const double PlovdivLatitude = 42.1354;
    private const double PlovdivLongitude = 24.7453;

    private static CreateSpot.Handler CreateHandler(IAppDbContext db, Guid userId, double newSpotRadiusKm = 5) =>
        new(
            db,
            new FakeUserContext(userId),
            TestOptionsFactory.Notifications(newSpotRadiusKm: newSpotRadiusKm),
            NullLogger<CreateSpot.Handler>.Instance);

    private static async Task<Spot> SeedSpotAsync(IAppDbContext db, Guid createdByUserId, double latitude, double longitude)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = $"Spot-{Guid.NewGuid():N}",
            Description = "test",
            Category = SpotCategory.Nature,
            PhotoUrl = "https://example.com/photo.jpg",
            Location = new Point(longitude, latitude) { SRID = 4326 },
            CreatedByUserId = createdByUserId,
        };

        db.Spots.Add(spot);
        await db.SaveChangesAsync();

        return spot;
    }

    [Fact]
    public async Task Creates_a_spot_owned_by_the_current_user_with_the_given_coordinates()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var handler = CreateHandler(db, userId);

        var command = new CreateSpot.Command(
            "Боянски водопад",
            "Скрит водопад във Витоша",
            SpotCategory.Nature,
            "https://example.com/photo.jpg",
            42.6461,
            23.2445);

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(userId, response.CreatedByUserId);
        Assert.Equal(command.Latitude, response.Latitude, precision: 6);
        Assert.Equal(command.Longitude, response.Longitude, precision: 6);

        var saved = await db.Spots.SingleAsync(s => s.Id == response.Id);
        Assert.Equal(userId, saved.CreatedByUserId);
        Assert.Equal(SpotCategory.Nature, saved.Category);
    }

    [Fact]
    public async Task Notifies_a_user_who_previously_created_a_nearby_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var authorId = Guid.NewGuid();
        var nearbyCreatorId = Guid.NewGuid();
        await SeedSpotAsync(db, nearbyCreatorId, SofiaLatitude, SofiaLongitude);

        var handler = CreateHandler(db, authorId);
        var command = new CreateSpot.Command(
            "New spot", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", SofiaLatitude, SofiaLongitude);
        var response = await handler.Handle(command, CancellationToken.None);

        var notification = await db.Notifications.SingleAsync(n => n.UserId == nearbyCreatorId);
        Assert.Equal(NotificationType.NewSpotNearby, notification.Type);
        Assert.Equal(response.Id, notification.RelatedSpotId);
    }

    [Fact]
    public async Task Notifies_a_user_who_previously_checked_in_near_the_new_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var authorId = Guid.NewGuid();
        var checkedInUserId = Guid.NewGuid();
        var existingSpot = await SeedSpotAsync(db, Guid.NewGuid(), SofiaLatitude, SofiaLongitude);
        db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            SpotId = existingSpot.Id,
            UserId = checkedInUserId,
            PhotoUrl = "https://example.com/checkin.jpg",
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, authorId);
        var command = new CreateSpot.Command(
            "New spot", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", SofiaLatitude, SofiaLongitude);
        var response = await handler.Handle(command, CancellationToken.None);

        var notification = await db.Notifications.SingleAsync(n => n.UserId == checkedInUserId);
        Assert.Equal(NotificationType.NewSpotNearby, notification.Type);
        Assert.Equal(response.Id, notification.RelatedSpotId);
    }

    [Fact]
    public async Task Does_not_notify_the_author_of_the_new_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var authorId = Guid.NewGuid();
        await SeedSpotAsync(db, authorId, SofiaLatitude, SofiaLongitude);

        var handler = CreateHandler(db, authorId);
        var command = new CreateSpot.Command(
            "New spot", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", SofiaLatitude, SofiaLongitude);
        await handler.Handle(command, CancellationToken.None);

        Assert.False(await db.Notifications.AnyAsync(n => n.UserId == authorId));
    }

    [Fact]
    public async Task Does_not_notify_a_user_whose_nearby_spot_is_outside_the_radius()
    {
        await using var db = TestDbContextFactory.Create();
        var authorId = Guid.NewGuid();
        var farUserId = Guid.NewGuid();
        await SeedSpotAsync(db, farUserId, PlovdivLatitude, PlovdivLongitude);

        var handler = CreateHandler(db, authorId);
        var command = new CreateSpot.Command(
            "New spot", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", SofiaLatitude, SofiaLongitude);
        await handler.Handle(command, CancellationToken.None);

        Assert.False(await db.Notifications.AnyAsync(n => n.UserId == farUserId));
    }

    [Fact]
    public async Task Notifies_a_nearby_user_only_once_even_if_both_a_creator_and_a_checked_in_visitor()
    {
        await using var db = TestDbContextFactory.Create();
        var authorId = Guid.NewGuid();
        var bothId = Guid.NewGuid();
        var existingSpot = await SeedSpotAsync(db, bothId, SofiaLatitude, SofiaLongitude);
        db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            SpotId = existingSpot.Id,
            UserId = bothId,
            PhotoUrl = "https://example.com/checkin.jpg",
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, authorId);
        var command = new CreateSpot.Command(
            "New spot", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", SofiaLatitude, SofiaLongitude);
        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, await db.Notifications.CountAsync(n => n.UserId == bothId));
    }
}
