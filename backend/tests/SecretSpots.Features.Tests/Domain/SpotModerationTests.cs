using NetTopologySuite.Geometries;
using SecretSpots.Domain;

namespace SecretSpots.Features.Tests.Domain;

// The moderation rules on Spot and SpotChangeRequest — plain objects, no database.
public class SpotModerationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(24);

    private static Spot NewSpot(SpotModerationStatus status = SpotModerationStatus.Pending) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Old name",
        Description = "Old description",
        Category = SpotCategory.Nature,
        Location = new Point(23.32, 42.69) { SRID = 4326 },
        PhotoUrls = ["https://example.com/kept.jpg", "https://example.com/dropped.jpg"],
        ModerationStatus = status,
    };

    private static SpotChangeRequest EditFor(Spot spot) => new()
    {
        Id = Guid.NewGuid(),
        SpotId = spot.Id,
        Type = SpotChangeRequestType.Edit,
        ProposedName = "New name",
        ProposedDescription = "New description",
        ProposedCategory = SpotCategory.Viewpoint,
        ProposedPhotoUrls = ["https://example.com/new.jpg", "https://example.com/kept.jpg"],
    };

    [Theory]
    [InlineData(SpotModerationStatus.Pending, NotificationType.SpotApproved)]
    [InlineData(SpotModerationStatus.Rejected, NotificationType.SpotRestored)]
    public void Approving_tells_the_creator_approved_or_restored(SpotModerationStatus from, NotificationType expected)
    {
        var spot = NewSpot(from);

        Assert.Equal(expected, spot.Approve());
        Assert.Equal(SpotModerationStatus.Approved, spot.ModerationStatus);
    }

    [Fact]
    public void Approving_clears_the_rejection_and_cancels_the_deletion()
    {
        var spot = NewSpot();
        spot.Reject("Blurry", Now, GracePeriod);

        spot.Approve();

        Assert.Null(spot.RejectionReason);
        Assert.Null(spot.ScheduledDeletionAt);
    }

    [Fact]
    public void Approving_an_approved_spot_has_nothing_to_tell()
    {
        Assert.Null(NewSpot(SpotModerationStatus.Approved).Approve());
    }

    [Theory]
    [InlineData(SpotModerationStatus.Pending, NotificationType.SpotRejected)]
    [InlineData(SpotModerationStatus.Approved, NotificationType.SpotRemoved)]
    public void Rejecting_tells_the_creator_rejected_or_removed(SpotModerationStatus from, NotificationType expected)
    {
        var spot = NewSpot(from);

        Assert.Equal(expected, spot.Reject("  Blurry photo  ", Now, GracePeriod));
        Assert.Equal(SpotModerationStatus.Rejected, spot.ModerationStatus);
        Assert.Equal("Blurry photo", spot.RejectionReason);
        Assert.Equal(Now + GracePeriod, spot.ScheduledDeletionAt);
    }

    [Fact]
    public void Re_rejecting_rewords_the_reason_without_extending_the_deadline_or_notifying()
    {
        var spot = NewSpot();
        spot.Reject("First wording", Now, GracePeriod);

        var notification = spot.Reject("Better wording", Now.AddHours(10), GracePeriod);

        Assert.Null(notification);
        Assert.Equal("Better wording", spot.RejectionReason);
        Assert.Equal(Now + GracePeriod, spot.ScheduledDeletionAt);
    }

    [Fact]
    public void Applying_an_edit_replaces_the_spot_and_returns_the_dropped_photos()
    {
        var spot = NewSpot(SpotModerationStatus.Approved);
        var request = EditFor(spot);
        var reviewerId = Guid.NewGuid();

        var dropped = request.ApplyTo(spot, reviewerId, Now);

        Assert.Equal(["https://example.com/dropped.jpg"], dropped);
        Assert.Equal(("New name", "New description", SpotCategory.Viewpoint), (spot.Name, spot.Description, spot.Category));
        Assert.Equal(["https://example.com/new.jpg", "https://example.com/kept.jpg"], spot.PhotoUrls);
        Assert.NotSame(request.ProposedPhotoUrls, spot.PhotoUrls);
        Assert.Equal((SpotChangeRequestStatus.Approved, reviewerId, Now), (request.Status, request.ReviewedByUserId, request.ReviewedAt));
    }

    [Fact]
    public void Only_a_pending_edit_for_this_spot_can_be_applied()
    {
        var spot = NewSpot();
        var deletion = new SpotChangeRequest { Id = Guid.NewGuid(), SpotId = spot.Id, Type = SpotChangeRequestType.Deletion };
        var otherSpotsEdit = EditFor(NewSpot());
        var reviewed = EditFor(spot);
        reviewed.Reject("No", Guid.NewGuid(), Now);

        Assert.Throws<InvalidOperationException>(() => deletion.ApplyTo(spot, Guid.NewGuid(), Now));
        Assert.Throws<InvalidOperationException>(() => otherSpotsEdit.ApplyTo(spot, Guid.NewGuid(), Now));
        Assert.Throws<InvalidOperationException>(() => reviewed.ApplyTo(spot, Guid.NewGuid(), Now));
        Assert.Equal("Old name", spot.Name);
    }

    [Theory]
    [InlineData(SpotChangeRequestType.Edit, NotificationType.SpotEditRejected)]
    [InlineData(SpotChangeRequestType.Deletion, NotificationType.SpotDeletionRejected)]
    public void Rejecting_a_request_records_the_review_and_picks_the_notification(SpotChangeRequestType type, NotificationType expected)
    {
        var request = new SpotChangeRequest { Id = Guid.NewGuid(), SpotId = Guid.NewGuid(), Type = type };
        var reviewerId = Guid.NewGuid();

        Assert.Equal(expected, request.Reject("  Not an improvement  ", reviewerId, Now));
        Assert.Equal("Not an improvement", request.RejectionReason);
        Assert.Equal((SpotChangeRequestStatus.Rejected, reviewerId, Now), (request.Status, request.ReviewedByUserId, request.ReviewedAt));
        Assert.Throws<InvalidOperationException>(() => request.Reject("Again", reviewerId, Now));
    }
}
