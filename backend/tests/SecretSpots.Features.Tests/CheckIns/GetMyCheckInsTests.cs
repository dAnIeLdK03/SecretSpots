using FluentValidation.TestHelper;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.CheckIns;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.CheckIns;

public class GetMyCheckInsValidatorTests
{
    private readonly GetMyCheckIns.Validator _validator =
        new(TestLocalizerFactory.Create(), TestOptionsFactory.CheckIn());

    [Fact]
    public void Page_below_one_is_invalid()
    {
        var result = _validator.TestValidate(new GetMyCheckIns.Query(0, 20));
        result.ShouldHaveValidationErrorFor(q => q.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_out_of_range_is_invalid(int pageSize)
    {
        var result = _validator.TestValidate(new GetMyCheckIns.Query(1, pageSize));
        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [Fact]
    public void Valid_query_has_no_errors()
    {
        var result = _validator.TestValidate(new GetMyCheckIns.Query(1, 20));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class GetMyCheckInsHandlerTests
{
    private static async Task<Spot> SeedSpotAsync(IAppDbContext db, string name)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "test",
            Category = SpotCategory.Nature,
            PhotoUrls = ["https://example.com/photo.jpg"],
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            CreatedByUserId = Guid.NewGuid(),
        };

        db.Spots.Add(spot);
        await db.SaveChangesAsync();

        return spot;
    }

    private static async Task<CheckIn> SeedCheckInAsync(IAppDbContext db, Guid userId, Guid spotId, int crystalsAwarded = 10)
    {
        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            SpotId = spotId,
            UserId = userId,
            PhotoUrl = "https://example.com/proof.jpg",
            CrystalsAwarded = crystalsAwarded,
        };

        db.CheckIns.Add(checkIn);
        await db.SaveChangesAsync();

        return checkIn;
    }

    private static GetMyCheckIns.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId));

    [Fact]
    public async Task Returns_only_the_current_users_checkins_newest_first_with_spot_name()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var spot = await SeedSpotAsync(db, "Sofia Viewpoint");

        var older = await SeedCheckInAsync(db, userId, spot.Id);
        await Task.Delay(10);
        var newer = await SeedCheckInAsync(db, userId, spot.Id);
        await SeedCheckInAsync(db, otherUserId, spot.Id);

        var handler = CreateHandler(db, userId);
        var result = await handler.Handle(new GetMyCheckIns.Query(1, 20), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(newer.Id, result.Items[0].Id);
        Assert.Equal(older.Id, result.Items[1].Id);
        Assert.Equal("Sofia Viewpoint", result.Items[0].SpotName);
        Assert.Equal(spot.Id, result.Items[0].SpotId);
    }

    [Fact]
    public async Task Pagination_slices_correctly_and_reports_total_count()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var spot = await SeedSpotAsync(db, "Rila Lakes");

        for (var i = 0; i < 5; i++)
        {
            await SeedCheckInAsync(db, userId, spot.Id);
            await Task.Delay(5);
        }

        var handler = CreateHandler(db, userId);
        var page1 = await handler.Handle(new GetMyCheckIns.Query(1, 2), CancellationToken.None);
        var page2 = await handler.Handle(new GetMyCheckIns.Query(2, 2), CancellationToken.None);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.DoesNotContain(page1.Items[0].Id, page2.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task Includes_photo_url_and_crystals_awarded()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var spot = await SeedSpotAsync(db, "Vitosha Peak");
        await SeedCheckInAsync(db, userId, spot.Id, crystalsAwarded: 15);

        var handler = CreateHandler(db, userId);
        var result = await handler.Handle(new GetMyCheckIns.Query(1, 20), CancellationToken.None);

        Assert.Equal("https://example.com/proof.jpg", result.Items[0].PhotoUrl);
        Assert.Equal(15, result.Items[0].CrystalsAwarded);
    }
}
