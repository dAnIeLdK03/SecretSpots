using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Notifications;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Notifications;

public class MarkNotificationAsReadTests
{
    private static MarkNotificationAsRead.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<MarkNotificationAsRead.Handler>.Instance);

    private static async Task<Notification> SeedAsync(IAppDbContext db, Guid userId)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = NotificationType.CrystalsEarned,
            CrystalsAwarded = 10,
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return notification;
    }

    [Fact]
    public async Task Marks_the_notification_as_read()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var notification = await SeedAsync(db, userId);

        var handler = CreateHandler(db, userId);
        var result = await handler.Handle(new MarkNotificationAsRead.Command(notification.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsRead);

        var saved = await db.Notifications.SingleAsync(n => n.Id == notification.Id);
        Assert.True(saved.IsRead);
        Assert.NotNull(saved.ReadAt);
    }

    [Fact]
    public async Task Nonexistent_notification_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(new MarkNotificationAsRead.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotificationsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Another_users_notification_returns_the_same_not_found_error()
    {
        await using var db = TestDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var notification = await SeedAsync(db, ownerId);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(new MarkNotificationAsRead.Command(notification.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(NotificationsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }
}
