using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Rewards;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Rewards;

public class CreateRewardValidatorTests
{
    private readonly CreateReward.Validator _validator = new(TestLocalizerFactory.Create());

    [Fact]
    public void Title_is_required()
    {
        var result = _validator.TestValidate(new CreateReward.Command(Guid.NewGuid(), "", "Desc", 10));
        result.ShouldHaveValidationErrorFor(c => c.Title);
    }

    [Fact]
    public void Description_is_required()
    {
        var result = _validator.TestValidate(new CreateReward.Command(Guid.NewGuid(), "Title", "", 10));
        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CrystalCost_must_be_positive(int crystalCost)
    {
        var result = _validator.TestValidate(new CreateReward.Command(Guid.NewGuid(), "Title", "Desc", crystalCost));
        result.ShouldHaveValidationErrorFor(c => c.CrystalCost);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(new CreateReward.Command(Guid.NewGuid(), "Title", "Desc", 10));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateRewardHandlerTests
{
    private static async Task<Business> SeedBusinessAsync(IAppDbContext db, Guid ownerId)
    {
        var business = new Business
        {
            Id = Guid.NewGuid(),
            Name = $"Business-{Guid.NewGuid():N}",
            Description = "test",
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            OwnerUserId = ownerId,
        };

        db.Businesses.Add(business);
        await db.SaveChangesAsync();

        return business;
    }

    private static CreateReward.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<CreateReward.Handler>.Instance);

    [Fact]
    public async Task Owner_can_create_a_reward_for_their_business()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var business = await SeedBusinessAsync(db, ownerId);

        var handler = CreateHandler(db, ownerId);
        var result = await handler.Handle(
            new CreateReward.Command(business.Id, "Free coffee", "One free coffee", 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(business.Id, result.Value.BusinessId);

        var saved = await db.Rewards.SingleAsync(r => r.Id == result.Value.Id);
        Assert.Equal("Free coffee", saved.Title);
        Assert.Equal(20, saved.CrystalCost);
    }

    [Fact]
    public async Task Nonexistent_business_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(
            new CreateReward.Command(Guid.NewGuid(), "Title", "Desc", 10), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(BusinessesMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Non_owner_cannot_create_a_reward()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var business = await SeedBusinessAsync(db, ownerId);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(
            new CreateReward.Command(business.Id, "Title", "Desc", 10), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(RewardsMessageKeys.NotYourBusiness, result.Error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error.StatusCode);
    }
}
