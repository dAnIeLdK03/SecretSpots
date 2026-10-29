using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Admin;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Email;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Admin;

public class AdminUsersTests
{
    private const string SecurityContactEmail = "security@test.example";

    private static SetUserRole.Handler CreateSetRoleHandler(IAppDbContext db, Guid adminId, IEmailSender emailSender) =>
        new(db, new FakeUserContext(adminId, UserRole.Admin), emailSender,
            Options.Create(new AdminSecurityOptions { SecurityContactEmail = SecurityContactEmail }),
            TestLocalizerFactory.Create(), NullLogger<SetUserRole.Handler>.Instance);

    private static async Task<RefreshToken> SeedRefreshTokenAsync(IAppDbContext db, Guid userId)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
        return token;
    }

    [Fact]
    public async Task Promoting_to_admin_audits_revokes_sessions_and_alerts_the_security_contact()
    {
        await using var db = TestDbContextFactory.Create();
        var adminId = Guid.NewGuid();
        var user = await TestUserFactory.SeedAsync(db);
        var token = await SeedRefreshTokenAsync(db, user.Id);
        var emailSender = new FakeEmailSender();

        var result = await CreateSetRoleHandler(db, adminId, emailSender).Handle(
            new SetUserRole.Command(user.Id, UserRole.Admin), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserRole.Admin, result.Value.Role);
        db.ChangeTracker.Clear();
        Assert.Equal(UserRole.Admin, (await db.Users.SingleAsync(u => u.Id == user.Id)).Role);
        Assert.NotNull((await db.RefreshTokens.SingleAsync(t => t.Id == token.Id)).RevokedAt);
        var audit = await db.RoleChangeAudits.SingleAsync(a => a.UserId == user.Id);
        Assert.Equal((UserRole.User, UserRole.Admin, adminId), (audit.OldRole, audit.NewRole, audit.ChangedByUserId));
        var email = Assert.Single(emailSender.SentEmails);
        Assert.Equal(SecurityContactEmail, email.ToEmail);
        Assert.Contains(user.Email, email.HtmlBody);
    }

    [Fact]
    public async Task Demoting_does_not_send_the_promotion_alert()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, UserRole.Admin);
        var emailSender = new FakeEmailSender();

        var result = await CreateSetRoleHandler(db, Guid.NewGuid(), emailSender).Handle(
            new SetUserRole.Command(user.Id, UserRole.User), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task A_failing_email_provider_does_not_undo_the_role_change()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db);

        var result = await CreateSetRoleHandler(db, Guid.NewGuid(), new ThrowingEmailSender()).Handle(
            new SetUserRole.Command(user.Id, UserRole.Admin), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(UserRole.Admin, (await db.Users.SingleAsync(u => u.Id == user.Id)).Role);
    }

    [Fact]
    public async Task Admins_cannot_change_their_own_role()
    {
        await using var db = TestDbContextFactory.Create();
        var admin = await TestUserFactory.SeedAsync(db, UserRole.Admin);

        var result = await CreateSetRoleHandler(db, admin.Id, new FakeEmailSender()).Handle(
            new SetUserRole.Command(admin.Id, UserRole.User), CancellationToken.None);

        Assert.Equal(AdminMessageKeys.CannotChangeOwnRole, result.Error.Code);
        db.ChangeTracker.Clear();
        Assert.Equal(UserRole.Admin, (await db.Users.SingleAsync(u => u.Id == admin.Id)).Role);
    }

    [Fact]
    public async Task Changing_the_role_of_a_missing_user_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await CreateSetRoleHandler(db, Guid.NewGuid(), new FakeEmailSender()).Handle(
            new SetUserRole.Command(Guid.NewGuid(), UserRole.Admin), CancellationToken.None);

        Assert.Equal(AdminMessageKeys.UserNotFound, result.Error.Code);
    }

    [Fact]
    public void Set_role_validator_rejects_an_unknown_role()
    {
        var result = new SetUserRole.Validator(TestLocalizerFactory.Create()).Validate(new SetUserRole.Command(Guid.NewGuid(), (UserRole)42));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task User_list_searches_email_and_display_name_case_insensitively()
    {
        await using var db = TestDbContextFactory.Create();
        var marker = Guid.NewGuid().ToString("N");
        var byName = await TestUserFactory.SeedAsync(db, displayName: $"Maria {marker}");
        var other = await TestUserFactory.SeedAsync(db);
        var handler = new GetAdminUsers.Handler(db);

        var result = await handler.Handle(new GetAdminUsers.Query($"MARIA {marker}", 1, 20), CancellationToken.None);
        var byEmail = await handler.Handle(new GetAdminUsers.Query(other.Email.ToUpperInvariant(), 1, 20), CancellationToken.None);

        Assert.Equal([byName.Id], result.Items.Select(u => u.Id));
        Assert.Equal([other.Id], byEmail.Items.Select(u => u.Id));
    }

    [Fact]
    public async Task User_details_count_activity_and_ignore_soft_deleted_spots()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, displayName: "Active Ana");
        var otherSpot = await TestSpotFactory.SeedAsync(db);
        await TestSpotFactory.SeedAsync(db, user.Id, SpotModerationStatus.Pending);
        await TestSpotFactory.SeedAsync(db, user.Id, SpotModerationStatus.Approved);
        await TestSpotFactory.SeedAsync(db, user.Id, SpotModerationStatus.Approved);
        var softDeleted = await TestSpotFactory.SeedAsync(db, user.Id, SpotModerationStatus.Rejected);
        softDeleted.DeletedAt = DateTimeOffset.UtcNow;
        await TestCommentFactory.SeedAsync(db, otherSpot.Id, user.Id);
        db.CheckIns.Add(new CheckIn { Id = Guid.NewGuid(), SpotId = otherSpot.Id, UserId = user.Id, PhotoUrl = "https://example.com/c.jpg" });
        db.Ratings.Add(new Rating { Id = Guid.NewGuid(), SpotId = otherSpot.Id, UserId = user.Id, Value = 5 });
        db.ExternalLogins.Add(new ExternalLogin
        {
            UserId = user.Id, Provider = ExternalAuthProvider.Google, ProviderUserId = Guid.NewGuid().ToString(), Email = user.Email,
        });
        (await db.CrystalWallets.SingleAsync(w => w.UserId == user.Id)).Balance = 42;
        await db.SaveChangesAsync();

        var result = await new GetAdminUser.Handler(db, TestLocalizerFactory.Create())
            .Handle(new GetAdminUser.Query(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var details = result.Value;
        Assert.Equal("Active Ana", details.DisplayName);
        Assert.False(details.HasPassword);
        Assert.Equal([ExternalAuthProvider.Google], details.LinkedProviders);
        Assert.Equal(new AdminUserActivityResponse(1, 2, 0, 1, 1, 1, 0, 42), details.Activity);
    }

    [Fact]
    public async Task User_details_for_a_missing_user_return_not_found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new GetAdminUser.Handler(db, TestLocalizerFactory.Create())
            .Handle(new GetAdminUser.Query(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(AdminMessageKeys.UserNotFound, result.Error.Code);
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("SMTP is down");
    }
}
