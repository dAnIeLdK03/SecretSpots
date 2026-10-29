using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Notifications;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Notifications;

public class MarkAllNotificationsAsReadTests
{
    [Fact]
    public async Task Marks_every_unread_notification_of_the_caller_and_nobody_elses()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var alreadyReadAt = DateTimeOffset.UtcNow.AddDays(-1);
        db.Notifications.AddRange(
            new Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.CrystalsEarned },
            new Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.NewSpotNearby },
            new Notification { Id = Guid.NewGuid(), UserId = userId, Type = NotificationType.SpotApproved, IsRead = true, ReadAt = alreadyReadAt },
            new Notification { Id = Guid.NewGuid(), UserId = otherUserId, Type = NotificationType.CrystalsEarned });
        await db.SaveChangesAsync();

        var result = await new MarkAllNotificationsAsRead.Handler(
                db, new FakeUserContext(userId), NullLogger<MarkAllNotificationsAsRead.Handler>.Instance)
            .Handle(new MarkAllNotificationsAsRead.Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        var mine = await db.Notifications.Where(n => n.UserId == userId).ToListAsync();
        Assert.All(mine, n => Assert.True(n.IsRead));
        // Already-read ones keep their original ReadAt, which drives the read-retention cleanup.
        Assert.Equal(alreadyReadAt, mine.Single(n => n.Type == NotificationType.SpotApproved).ReadAt!.Value, TimeSpan.FromMilliseconds(1));
        Assert.False((await db.Notifications.SingleAsync(n => n.UserId == otherUserId)).IsRead);
    }
}
