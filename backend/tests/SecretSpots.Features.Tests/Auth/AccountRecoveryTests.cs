using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Auth;

// Email verification, password reset and the small session endpoints (logout, current user).
public partial class AccountRecoveryTests
{
    private const string OldPassword = "OldPassw0rd!";
    private const string NewPassword = "N3wPassword!x";

    [GeneratedRegex("token=([A-Za-z0-9_-]+)")]
    private static partial Regex TokenInLink();

    private static string TokenFrom(FakeEmailSender emailSender) =>
        TokenInLink().Match(Assert.Single(emailSender.SentEmails).HtmlBody).Groups[1].Value;

    private static RequestPasswordReset.Handler CreateRequestResetHandler(IAppDbContext db, FakeEmailSender emailSender) =>
        new(db, emailSender,
            Options.Create(new PasswordResetOptions { FrontendResetUrl = "https://test.example/reset-password" }),
            TestLocalizerFactory.Create(), NullLogger<RequestPasswordReset.Handler>.Instance);

    private static ResetPassword.Handler CreateResetHandler(IAppDbContext db) =>
        new(db, TestLocalizerFactory.Create(), NullLogger<ResetPassword.Handler>.Instance);

    private static async Task<RefreshToken> SeedRefreshTokenAsync(IAppDbContext db, Guid userId, string rawToken)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = OpaqueTokenHasher.Hash(rawToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
        };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
        return token;
    }

    [Fact]
    public async Task Password_reset_link_sets_the_new_password_and_signs_out_every_session()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"reset-{Guid.NewGuid():N}@example.com", OldPassword);
        var session = await SeedRefreshTokenAsync(db, user.Id, Guid.NewGuid().ToString("N"));
        var emailSender = new FakeEmailSender();

        await CreateRequestResetHandler(db, emailSender).Handle(
            new RequestPasswordReset.Command($"  {user.Email.ToUpperInvariant()} "), CancellationToken.None);
        var rawToken = TokenFrom(emailSender);
        var result = await CreateResetHandler(db).Handle(new ResetPassword.Command(rawToken, NewPassword), CancellationToken.None);

        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        var saved = await db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify(NewPassword, saved.PasswordHash));
        Assert.NotNull((await db.RefreshTokens.SingleAsync(t => t.Id == session.Id)).RevokedAt);
        // Only the hash is stored, so a DB leak doesn't hand out working reset links.
        Assert.False(await db.PasswordResetTokens.AnyAsync(t => t.Token == rawToken));
    }

    [Fact]
    public async Task A_reset_link_works_only_once()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"reset-{Guid.NewGuid():N}@example.com", OldPassword);
        var emailSender = new FakeEmailSender();
        await CreateRequestResetHandler(db, emailSender).Handle(new RequestPasswordReset.Command(user.Email), CancellationToken.None);
        var rawToken = TokenFrom(emailSender);
        await CreateResetHandler(db).Handle(new ResetPassword.Command(rawToken, NewPassword), CancellationToken.None);

        var second = await CreateResetHandler(db).Handle(new ResetPassword.Command(rawToken, "An0therPass!"), CancellationToken.None);

        Assert.Equal(AuthMessageKeys.PasswordResetTokenInvalidOrExpired, second.Error.Code);
    }

    [Fact]
    public async Task An_expired_or_unknown_reset_token_is_rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db);
        const string expiredRaw = "expired-reset-token";
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, Token = OpaqueTokenHasher.Hash(expiredRaw), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        });
        await db.SaveChangesAsync();

        var expired = await CreateResetHandler(db).Handle(new ResetPassword.Command(expiredRaw, NewPassword), CancellationToken.None);
        var unknown = await CreateResetHandler(db).Handle(new ResetPassword.Command("never-issued", NewPassword), CancellationToken.None);

        Assert.Equal(AuthMessageKeys.PasswordResetTokenInvalidOrExpired, expired.Error.Code);
        Assert.Equal(AuthMessageKeys.PasswordResetTokenInvalidOrExpired, unknown.Error.Code);
    }

    [Fact]
    public async Task Requesting_a_reset_for_an_unknown_email_succeeds_silently()
    {
        await using var db = TestDbContextFactory.Create();
        var emailSender = new FakeEmailSender();

        // Same answer as for a real account, so the endpoint can't be used to probe which emails exist.
        var result = await CreateRequestResetHandler(db, emailSender).Handle(
            new RequestPasswordReset.Command($"nobody-{Guid.NewGuid():N}@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(emailSender.SentEmails);
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    [InlineData("Password1")]
    public void Reset_validator_applies_the_password_rules(string password)
    {
        var result = new ResetPassword.Validator(TestLocalizerFactory.Create()).Validate(new ResetPassword.Command("token", password));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Verification_link_from_resend_marks_the_email_verified_once()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db);
        var emailSender = new FakeEmailSender();
        await new ResendEmailVerification.Handler(
                db, new FakeUserContext(user.Id), emailSender, TestOptionsFactory.EmailVerification(),
                TestLocalizerFactory.Create(), NullLogger<ResendEmailVerification.Handler>.Instance)
            .Handle(new ResendEmailVerification.Command(), CancellationToken.None);
        var rawToken = TokenFrom(emailSender);
        var verify = new VerifyEmail.Handler(db, TestLocalizerFactory.Create(), NullLogger<VerifyEmail.Handler>.Instance);

        var first = await verify.Handle(new VerifyEmail.Command(rawToken), CancellationToken.None);
        var second = await verify.Handle(new VerifyEmail.Command(rawToken), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal(AuthMessageKeys.EmailVerificationTokenInvalidOrExpired, second.Error.Code);
        db.ChangeTracker.Clear();
        Assert.True((await db.Users.SingleAsync(u => u.Id == user.Id)).IsEmailVerified);
    }

    [Fact]
    public async Task Resend_does_nothing_for_an_already_verified_email()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db);
        user.IsEmailVerified = true;
        await db.SaveChangesAsync();
        var emailSender = new FakeEmailSender();

        var result = await new ResendEmailVerification.Handler(
                db, new FakeUserContext(user.Id), emailSender, TestOptionsFactory.EmailVerification(),
                TestLocalizerFactory.Create(), NullLogger<ResendEmailVerification.Handler>.Instance)
            .Handle(new ResendEmailVerification.Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task An_expired_verification_token_is_rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db);
        const string rawToken = "expired-verification-token";
        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, Token = OpaqueTokenHasher.Hash(rawToken), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        });
        await db.SaveChangesAsync();

        var result = await new VerifyEmail.Handler(db, TestLocalizerFactory.Create(), NullLogger<VerifyEmail.Handler>.Instance)
            .Handle(new VerifyEmail.Command(rawToken), CancellationToken.None);

        Assert.Equal(AuthMessageKeys.EmailVerificationTokenInvalidOrExpired, result.Error.Code);
        db.ChangeTracker.Clear();
        Assert.False((await db.Users.SingleAsync(u => u.Id == user.Id)).IsEmailVerified);
    }

    [Fact]
    public async Task Logout_revokes_only_the_given_session_and_tolerates_unknown_tokens()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db);
        var thisDevice = Guid.NewGuid().ToString("N");
        var thisSession = await SeedRefreshTokenAsync(db, user.Id, thisDevice);
        var otherSession = await SeedRefreshTokenAsync(db, user.Id, Guid.NewGuid().ToString("N"));
        var handler = new Logout.Handler(db);

        var result = await handler.Handle(new Logout.Command(thisDevice), CancellationToken.None);
        var unknown = await handler.Handle(new Logout.Command("not-a-token"), CancellationToken.None);
        var empty = await handler.Handle(new Logout.Command(""), CancellationToken.None);

        Assert.True(result.IsSuccess && unknown.IsSuccess && empty.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.NotNull((await db.RefreshTokens.SingleAsync(t => t.Id == thisSession.Id)).RevokedAt);
        Assert.Null((await db.RefreshTokens.SingleAsync(t => t.Id == otherSession.Id)).RevokedAt);
    }

    [Fact]
    public async Task Current_user_returns_the_profile_with_the_crystal_balance()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, UserRole.User, "Profile Petar");
        (await db.CrystalWallets.SingleAsync(w => w.UserId == user.Id)).Balance = 7;
        await db.SaveChangesAsync();

        var result = await new GetCurrentUser.Handler(
                db, new FakeUserContext(user.Id), TestLocalizerFactory.Create(), NullLogger<GetCurrentUser.Handler>.Instance)
            .Handle(new GetCurrentUser.Query(), CancellationToken.None);

        Assert.Equal(new GetCurrentUser.Response(user.Id, user.Email, "Profile Petar", 7, false, UserRole.User), result.Value);
    }

    [Fact]
    public async Task Current_user_for_a_deleted_account_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();

        var result = await new GetCurrentUser.Handler(
                db, new FakeUserContext(Guid.NewGuid()), TestLocalizerFactory.Create(), NullLogger<GetCurrentUser.Handler>.Instance)
            .Handle(new GetCurrentUser.Query(), CancellationToken.None);

        Assert.Equal(AuthMessageKeys.UserNotFound, result.Error.Code);
    }
}
