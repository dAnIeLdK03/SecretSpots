using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Auth;

public class DeleteAccountTests
{
    private const string Password = "Str0ng!Passw0rd1";

    private static DeleteAccount.Handler CreateHandler(IAppDbContext db, Guid userId, FakePhotoStorage? photoStorage = null) =>
        new(db, new FakeUserContext(userId), photoStorage ?? new FakePhotoStorage(), TestLocalizerFactory.Create(),
            NullLogger<DeleteAccount.Handler>.Instance);

    [Fact]
    public async Task Deleting_the_account_removes_every_row_keyed_by_the_user_including_push_subscriptions_and_email_verification_tokens()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"delete-{Guid.NewGuid():N}@example.com", Password);

        db.PushSubscriptions.Add(new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Endpoint = "https://push.example.com/endpoint",
            P256dh = "p256dh-key",
            Auth = "auth-secret",
        });
        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = "verify-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new DeleteAccount.Command(Password), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(await db.Users.AnyAsync(u => u.Id == user.Id));
        Assert.False(await db.PushSubscriptions.AnyAsync(p => p.UserId == user.Id));
        Assert.False(await db.EmailVerificationTokens.AnyAsync(t => t.UserId == user.Id));
    }

    [Fact]
    public async Task Wrong_password_is_rejected_and_nothing_is_deleted()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"delete-{Guid.NewGuid():N}@example.com", Password);

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new DeleteAccount.Command("wrong-password"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.True(await db.Users.AnyAsync(u => u.Id == user.Id));
    }
}
