using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Spots;

public class UpdateSpotValidatorTests
{
    private readonly UpdateSpot.Validator _validator = new(TestLocalizerFactory.Create());

    [Fact]
    public void Name_is_required()
    {
        var result = _validator.TestValidate(
            new UpdateSpot.Command(Guid.NewGuid(), "", "Desc", SpotCategory.Nature, "https://example.com/a.jpg"));
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Description_is_required()
    {
        var result = _validator.TestValidate(
            new UpdateSpot.Command(Guid.NewGuid(), "Name", "", SpotCategory.Nature, "https://example.com/a.jpg"));
        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/a.jpg")]
    public void PhotoUrl_is_invalid(string photoUrl)
    {
        var result = _validator.TestValidate(
            new UpdateSpot.Command(Guid.NewGuid(), "Name", "Desc", SpotCategory.Nature, photoUrl));
        result.ShouldHaveValidationErrorFor(c => c.PhotoUrl);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(
            new UpdateSpot.Command(Guid.NewGuid(), "Name", "Desc", SpotCategory.Nature, "https://example.com/a.jpg"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateSpotHandlerTests
{
    private static async Task<Spot> SeedAsync(IAppDbContext db, Guid createdByUserId)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = "Original name",
            Description = "Original description",
            Category = SpotCategory.Nature,
            PhotoUrl = "https://example.com/original.jpg",
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            CreatedByUserId = createdByUserId,
        };
        db.Spots.Add(spot);

        await db.SaveChangesAsync();

        return spot;
    }

    private static UpdateSpot.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<UpdateSpot.Handler>.Instance);

    [Fact]
    public async Task Creator_can_update_their_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);

        var handler = CreateHandler(db, creatorId);
        var result = await handler.Handle(
            new UpdateSpot.Command(spot.Id, "New name", "New description", SpotCategory.Viewpoint, "https://example.com/new.jpg"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New name", result.Value.Name);
        Assert.Equal(SpotCategory.Viewpoint, result.Value.Category);

        var saved = await db.Spots.SingleAsync(s => s.Id == spot.Id);
        Assert.Equal("New name", saved.Name);
        Assert.Equal("New description", saved.Description);
        Assert.Equal(SpotCategory.Viewpoint, saved.Category);
        Assert.Equal("https://example.com/new.jpg", saved.PhotoUrl);
    }

    [Fact]
    public async Task Update_does_not_change_the_spot_coordinates()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);
        var originalLatitude = spot.Location.Y;
        var originalLongitude = spot.Location.X;

        var handler = CreateHandler(db, creatorId);
        var result = await handler.Handle(
            new UpdateSpot.Command(spot.Id, "New name", "New description", SpotCategory.Viewpoint, "https://example.com/new.jpg"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(originalLatitude, result.Value.Latitude, precision: 6);
        Assert.Equal(originalLongitude, result.Value.Longitude, precision: 6);
    }

    [Fact]
    public async Task Nonexistent_spot_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(
            new UpdateSpot.Command(Guid.NewGuid(), "Name", "Desc", SpotCategory.Nature, "https://example.com/a.jpg"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SpotsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Non_creator_cannot_update_the_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(
            new UpdateSpot.Command(spot.Id, "Name", "Desc", SpotCategory.Nature, "https://example.com/a.jpg"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SpotsMessageKeys.NotYourSpot, result.Error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error.StatusCode);
    }
}
