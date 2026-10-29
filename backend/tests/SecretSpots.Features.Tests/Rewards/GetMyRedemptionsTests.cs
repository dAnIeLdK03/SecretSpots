using SecretSpots.Domain;
using SecretSpots.Features.Rewards;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Rewards;

public class GetMyRedemptionsTests
{
    [Fact]
    public async Task Lists_only_the_callers_redemptions_newest_first()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        RewardRedemption Redemption(Guid owner, string title, DateTimeOffset createdAt) => new()
        {
            Id = Guid.NewGuid(), RewardId = Guid.NewGuid(), BusinessId = Guid.NewGuid(), UserId = owner,
            CrystalsSpent = 10, RewardTitle = title, BusinessName = "Cafe", CreatedAt = createdAt,
        };
        var older = Redemption(userId, "Older", DateTimeOffset.UtcNow.AddDays(-2));
        var newer = Redemption(userId, "Newer", DateTimeOffset.UtcNow.AddDays(-1));
        db.RewardRedemptions.AddRange(older, newer, Redemption(Guid.NewGuid(), "Someone else's", DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var page = await new GetMyRedemptions.Handler(db, new FakeUserContext(userId))
            .Handle(new GetMyRedemptions.Query(1, 20), CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], page.Items.Select(r => r.RedemptionId));
        Assert.Equal("Newer", page.Items[0].RewardTitle);
    }
}
