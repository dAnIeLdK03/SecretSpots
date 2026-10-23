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

public class SubmitSpotEditValidatorTests
{
    private static readonly Guid ValidatorUserId = Guid.NewGuid();

    private readonly SubmitSpotEdit.Validator _validator =
        new(TestLocalizerFactory.Create(), TestOptionsFactory.R2(), new FakeUserContext(ValidatorUserId));

    private static readonly string ValidPhotoUrl = $"{TestOptionsFactory.PhotoPublicBaseUrl}/{ValidatorUserId}/a.jpg";

    [Fact]
    public void Name_is_required()
    {
        var result = _validator.TestValidate(
            new SubmitSpotEdit.Command(Guid.NewGuid(), "", "Desc", SpotCategory.Nature, [ValidPhotoUrl]));
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Description_is_required()
    {
        var result = _validator.TestValidate(
            new SubmitSpotEdit.Command(Guid.NewGuid(), "Name", "", SpotCategory.Nature, [ValidPhotoUrl]));
        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/a.jpg")]
    [InlineData("https://example.com/a.jpg")] // valid http(s) URL, but not this app's own photo storage
    public void PhotoUrl_is_invalid(string photoUrl)
    {
        var result = _validator.TestValidate(
            new SubmitSpotEdit.Command(Guid.NewGuid(), "Name", "Desc", SpotCategory.Nature, [photoUrl]));
        result.ShouldHaveValidationErrorFor(c => c.PhotoUrls);
    }

    [Fact]
    public void PhotoUrl_belonging_to_a_different_user_is_invalid()
    {
        var someoneElsesPhotoUrl = $"{TestOptionsFactory.PhotoPublicBaseUrl}/{Guid.NewGuid()}/a.jpg";

        var result = _validator.TestValidate(
            new SubmitSpotEdit.Command(Guid.NewGuid(), "Name", "Desc", SpotCategory.Nature, [someoneElsesPhotoUrl]));
        result.ShouldHaveValidationErrorFor(c => c.PhotoUrls);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(
            new SubmitSpotEdit.Command(Guid.NewGuid(), "Name", "Desc", SpotCategory.Nature, [ValidPhotoUrl]));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class SubmitSpotEditHandlerTests
{
    private static async Task<Spot> SeedAsync(IAppDbContext db, Guid createdByUserId)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = "Original name",
            Description = "Original description",
            Category = SpotCategory.Nature,
            PhotoUrls = ["https://example.com/original.jpg"],
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            CreatedByUserId = createdByUserId,
        };
        db.Spots.Add(spot);

        await db.SaveChangesAsync();

        return spot;
    }

    private static SubmitSpotEdit.Handler CreateHandler(IAppDbContext db, Guid userId, FakePhotoStorage? photoStorage = null) =>
        new(db, new FakeUserContext(userId), photoStorage ?? new FakePhotoStorage(), TestLocalizerFactory.Create(), NullLogger<SubmitSpotEdit.Handler>.Instance);

    [Fact]
    public async Task Creator_submits_a_pending_edit_without_changing_the_spot()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);
        var photoStorage = new FakePhotoStorage();

        var handler = CreateHandler(db, creatorId, photoStorage);
        var result = await handler.Handle(
            new SubmitSpotEdit.Command(spot.Id, "New name", "New description", SpotCategory.Viewpoint, ["https://example.com/new.jpg"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SpotChangeRequestType.Edit, result.Value.Type);
        Assert.Equal(SpotChangeRequestStatus.Pending, result.Value.Status);
        Assert.Equal("New name", result.Value.ProposedName);

        db.ChangeTracker.Clear();
        var saved = await db.Spots.SingleAsync(s => s.Id == spot.Id);
        Assert.Equal("Original name", saved.Name);
        Assert.Equal(["https://example.com/original.jpg"], saved.PhotoUrls);
        Assert.Empty(photoStorage.DeletedUrls);
    }

    [Fact]
    public async Task Resubmitting_replaces_the_pending_request_and_deletes_its_dropped_uploads()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);
        var photoStorage = new FakePhotoStorage();
        var handler = CreateHandler(db, creatorId, photoStorage);

        await handler.Handle(
            new SubmitSpotEdit.Command(spot.Id, "First", "Desc", SpotCategory.Nature, ["https://example.com/first.jpg"]),
            CancellationToken.None);
        var result = await handler.Handle(
            new SubmitSpotEdit.Command(spot.Id, "Second", "Desc", SpotCategory.Nature, ["https://example.com/original.jpg"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, await db.SpotChangeRequests.CountAsync(r => r.SpotId == spot.Id));
        Assert.Equal("Second", (await db.SpotChangeRequests.SingleAsync(r => r.SpotId == spot.Id)).ProposedName);
        Assert.Equal(["https://example.com/first.jpg"], photoStorage.DeletedUrls);
    }

    [Fact]
    public async Task Submitting_without_changes_is_rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);

        var handler = CreateHandler(db, creatorId);
        var result = await handler.Handle(
            new SubmitSpotEdit.Command(spot.Id, spot.Name, spot.Description, spot.Category, spot.PhotoUrls),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SpotsMessageKeys.NoChanges, result.Error.Code);
        Assert.False(await db.SpotChangeRequests.AnyAsync(r => r.SpotId == spot.Id));
    }

    [Fact]
    public async Task Nonexistent_spot_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(
            new SubmitSpotEdit.Command(Guid.NewGuid(), "Name", "Desc", SpotCategory.Nature, ["https://example.com/a.jpg"]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SpotsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Non_creator_cannot_submit_an_edit()
    {
        await using var db = TestDbContextFactory.Create();
        var creatorId = Guid.NewGuid();
        var spot = await SeedAsync(db, creatorId);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(
            new SubmitSpotEdit.Command(spot.Id, "Name", "Desc", SpotCategory.Nature, ["https://example.com/a.jpg"]),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SpotsMessageKeys.NotYourSpot, result.Error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error.StatusCode);
    }
}
