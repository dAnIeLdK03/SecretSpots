using SecretSpots.Domain;
using SecretSpots.Features.Admin;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Admin;

public class SpotChangeRequestQueueTests
{
    [Fact]
    public async Task Queue_shows_pending_requests_oldest_first_with_current_and_proposed_values()
    {
        await using var db = TestDbContextFactory.Create();
        var owner = await TestUserFactory.SeedAsync(db, displayName: "Owner Olga");
        var spotA = await TestSpotFactory.SeedAsync(db, owner.Id, name: "Current A");
        var spotB = await TestSpotFactory.SeedAsync(db, owner.Id);
        var spotC = await TestSpotFactory.SeedAsync(db, owner.Id);
        var older = new SpotChangeRequest
        {
            Id = Guid.NewGuid(), SpotId = spotA.Id, RequestedByUserId = owner.Id, Type = SpotChangeRequestType.Edit,
            ProposedName = "Proposed A", ProposedDescription = "d", ProposedCategory = SpotCategory.Viewpoint, ProposedPhotoUrls = ["https://example.com/n.jpg"],
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
        };
        var newer = new SpotChangeRequest
        {
            Id = Guid.NewGuid(), SpotId = spotB.Id, RequestedByUserId = owner.Id, Type = SpotChangeRequestType.Deletion,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
        };
        var reviewed = new SpotChangeRequest
        {
            Id = Guid.NewGuid(), SpotId = spotC.Id, RequestedByUserId = owner.Id, Type = SpotChangeRequestType.Deletion,
            Status = SpotChangeRequestStatus.Rejected,
        };
        db.SpotChangeRequests.AddRange(older, newer, reviewed);
        await db.SaveChangesAsync();

        var page = await new GetAdminSpotChangeRequests.Handler(db)
            .Handle(new GetAdminSpotChangeRequests.Query(1, 100), CancellationToken.None);

        var ours = page.Items.Where(i => i.RequestedByUserId == owner.Id).ToList();
        Assert.Equal([older.Id, newer.Id], ours.Select(i => i.Id));
        var edit = ours[0];
        Assert.Equal("Owner Olga", edit.RequestedByDisplayName);
        Assert.Equal("Current A", edit.CurrentName);
        Assert.Equal("Proposed A", edit.ProposedName);
        Assert.Equal(SpotCategory.Viewpoint, edit.ProposedCategory);
        Assert.Null(ours[1].ProposedName);
    }
}
