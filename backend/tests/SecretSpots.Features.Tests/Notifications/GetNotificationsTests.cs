using FluentValidation.TestHelper;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Notifications;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Notifications;

public class GetNotificationsValidatorTests
{
    private readonly GetNotifications.Validator _validator =
        new(TestLocalizerFactory.Create(), TestOptionsFactory.Notifications());

    [Fact]
    public void Page_below_one_is_invalid()
    {
        var result = _validator.TestValidate(new GetNotifications.Query(0, 20));
        result.ShouldHaveValidationErrorFor(q => q.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_out_of_range_is_invalid(int pageSize)
    {
        var result = _validator.TestValidate(new GetNotifications.Query(1, pageSize));
        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [Fact]
    public void Valid_query_has_no_errors()
    {
        var result = _validator.TestValidate(new GetNotifications.Query(1, 20));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class GetNotificationsHandlerTests
{
    private static async Task<Notification> SeedAsync(
        IAppDbContext db, Guid userId, int? crystalsAwarded = 10, bool isRead = false, DateTimeOffset? readAt = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = NotificationType.CrystalsEarned,
            CrystalsAwarded = crystalsAwarded,
            IsRead = isRead,
            ReadAt = readAt,
        };

        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        return notification;
    }

    private static GetNotifications.Handler CreateHandler(IAppDbContext db, Guid userId, int readRetentionHours = 24) =>
        new(db, new FakeUserContext(userId), TestOptionsFactory.Notifications(readRetentionHours: readRetentionHours), TestLocalizerFactory.Create());

    [Fact]
    public async Task Returns_only_the_current_users_notifications_newest_first()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var older = await SeedAsync(db, userId);
        await Task.Delay(10);
        var newer = await SeedAsync(db, userId);
        await SeedAsync(db, otherUserId);

        var handler = CreateHandler(db, userId);
        var result = await handler.Handle(new GetNotifications.Query(1, 20), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(newer.Id, result.Items[0].Id);
        Assert.Equal(older.Id, result.Items[1].Id);
    }

    [Fact]
    public async Task Pagination_slices_correctly_and_reports_total_count()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
        {
            await SeedAsync(db, userId);
            await Task.Delay(5);
        }

        var handler = CreateHandler(db, userId);
        var page1 = await handler.Handle(new GetNotifications.Query(1, 2), CancellationToken.None);
        var page2 = await handler.Handle(new GetNotifications.Query(2, 2), CancellationToken.None);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.DoesNotContain(page1.Items[0].Id, page2.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task Message_is_localized_and_includes_crystals_awarded()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        await SeedAsync(db, userId, crystalsAwarded: 15);

        var handler = CreateHandler(db, userId);
        var result = await handler.Handle(new GetNotifications.Query(1, 20), CancellationToken.None);

        Assert.Contains("15", result.Items[0].Message);
    }

    [Fact]
    public async Task Excludes_a_read_notification_older_than_the_retention_window()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var oldRead = await SeedAsync(db, userId, isRead: true, readAt: DateTimeOffset.UtcNow.AddHours(-25));

        var handler = CreateHandler(db, userId, readRetentionHours: 24);
        var result = await handler.Handle(new GetNotifications.Query(1, 20), CancellationToken.None);

        Assert.DoesNotContain(result.Items, i => i.Id == oldRead.Id);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Includes_a_read_notification_still_within_the_retention_window()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var recentRead = await SeedAsync(db, userId, isRead: true, readAt: DateTimeOffset.UtcNow.AddHours(-1));

        var handler = CreateHandler(db, userId, readRetentionHours: 24);
        var result = await handler.Handle(new GetNotifications.Query(1, 20), CancellationToken.None);

        Assert.Contains(result.Items, i => i.Id == recentRead.Id);
    }

    [Fact]
    public async Task Includes_an_unread_notification_regardless_of_age()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var oldUnread = await SeedAsync(db, userId, isRead: false);

        var handler = CreateHandler(db, userId, readRetentionHours: 24);
        var result = await handler.Handle(new GetNotifications.Query(1, 20), CancellationToken.None);

        Assert.Contains(result.Items, i => i.Id == oldUnread.Id);
    }
}
