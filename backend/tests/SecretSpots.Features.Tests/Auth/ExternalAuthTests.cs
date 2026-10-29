using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.ExternalAuth;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Auth;

// Begin → provider callback → one-time code exchange. The callback decides which account an
// outside login lands in, so most cases here are about not attaching it to the wrong one.
public class ExternalAuthTests
{
    private const string FrontendCallbackUrl = "https://test.example/auth/callback";

    private static ExternalAuthCallback.Handler CreateCallbackHandler(IAppDbContext db, FakeExternalAuthProvider provider) =>
        new(db, [provider],
            Options.Create(new ExternalAuthOptions { ApiBaseUrl = "https://api.test.example", FrontendCallbackUrl = FrontendCallbackUrl }),
            TestOptionsFactory.Crystals(startingBalance: 5), NullLogger<ExternalAuthCallback.Handler>.Instance);

    private static ExchangeExternalAuthCode.Handler CreateExchangeHandler(IAppDbContext db) =>
        new(db, new JwtService(TestOptionsFactory.Jwt()), TestOptionsFactory.Jwt(), TestLocalizerFactory.Create());

    private static async Task<string> BeginAsync(IAppDbContext db, FakeExternalAuthProvider provider)
    {
        var result = await new BeginExternalAuth.Handler(db, [provider])
            .Handle(new BeginExternalAuth.Command(provider.Provider), CancellationToken.None);
        return result.Value[(result.Value.IndexOf("state=", StringComparison.Ordinal) + "state=".Length)..];
    }

    private static string CodeFrom(string redirect) =>
        redirect[(redirect.IndexOf("code=", StringComparison.Ordinal) + "code=".Length)..];

    private static FakeExternalAuthProvider Google(string? email = null, bool emailVerified = true, string? providerUserId = null) =>
        new(ExternalAuthProvider.Google,
            new ExternalAuthUserInfo(providerUserId ?? Guid.NewGuid().ToString("N"), email ?? $"g-{Guid.NewGuid():N}@example.com", emailVerified, "Google User"));

    [Fact]
    public async Task A_new_google_user_gets_an_account_and_can_exchange_the_code_once()
    {
        await using var db = TestDbContextFactory.Create();
        var provider = Google();
        var state = await BeginAsync(db, provider);

        var redirect = await CreateCallbackHandler(db, provider).Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "auth-code", state, null), CancellationToken.None);
        var code = CodeFrom(redirect.Value);
        var exchange = await CreateExchangeHandler(db).Handle(new ExchangeExternalAuthCode.Command(code), CancellationToken.None);
        var replay = await CreateExchangeHandler(db).Handle(new ExchangeExternalAuthCode.Command(code), CancellationToken.None);

        Assert.StartsWith(FrontendCallbackUrl + "?code=", redirect.Value);
        Assert.True(exchange.IsSuccess);
        Assert.NotEmpty(exchange.Value.AccessToken);
        Assert.Equal(AuthMessageKeys.ExternalAuthCodeInvalidOrExpired, replay.Error.Code);
        var user = await db.Users.SingleAsync(u => u.Email == provider.UserInfo.Email);
        Assert.True(user.IsEmailVerified);
        Assert.Null(user.PasswordHash);
        Assert.Equal(5, (await db.CrystalWallets.SingleAsync(w => w.UserId == user.Id)).Balance);
    }

    [Fact]
    public async Task A_returning_external_user_lands_in_the_same_account()
    {
        await using var db = TestDbContextFactory.Create();
        var providerUserId = Guid.NewGuid().ToString("N");
        var first = Google(providerUserId: providerUserId);
        await CreateCallbackHandler(db, first).Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "c", await BeginAsync(db, first), null), CancellationToken.None);
        // Same Google account, even if it has changed its email since.
        var again = Google(email: $"changed-{Guid.NewGuid():N}@example.com", providerUserId: providerUserId);

        await CreateCallbackHandler(db, again).Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "c", await BeginAsync(db, again), null), CancellationToken.None);

        Assert.Equal(1, await db.ExternalLogins.CountAsync(l => l.ProviderUserId == providerUserId));
        Assert.False(await db.Users.AnyAsync(u => u.Email == again.UserInfo.Email));
    }

    [Fact]
    public async Task A_verified_email_links_to_the_existing_password_account()
    {
        await using var db = TestDbContextFactory.Create();
        var existing = await TestUserFactory.SeedAsync(db, $"link-{Guid.NewGuid():N}@example.com", "Passw0rd!x");
        var provider = Google(email: existing.Email.ToUpperInvariant());

        await CreateCallbackHandler(db, provider).Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "c", await BeginAsync(db, provider), null), CancellationToken.None);

        Assert.True(await db.ExternalLogins.AnyAsync(l => l.UserId == existing.Id && l.ProviderUserId == provider.UserInfo.ProviderUserId));
        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == existing.Email));
    }

    [Fact]
    public async Task A_cancelled_or_tampered_callback_redirects_with_an_error_and_logs_no_one_in()
    {
        await using var db = TestDbContextFactory.Create();
        var provider = Google();
        var handler = CreateCallbackHandler(db, provider);
        var state = await BeginAsync(db, provider);

        var cancelled = await handler.Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, null, state, "access_denied"), CancellationToken.None);
        var forgedState = await handler.Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "c", "forged-state", null), CancellationToken.None);
        // A state issued for Google can't complete a Facebook callback.
        var wrongProvider = await handler.Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Facebook, "c", state, null), CancellationToken.None);

        Assert.Equal(FrontendCallbackUrl + "?error=cancelled", cancelled.Value);
        Assert.Equal(FrontendCallbackUrl + "?error=invalid_state", forgedState.Value);
        Assert.Equal(FrontendCallbackUrl + "?error=invalid_state", wrongProvider.Value);
        Assert.Equal(0, provider.ExchangeCallCount);
    }

    [Fact]
    public async Task A_state_can_only_be_used_once()
    {
        await using var db = TestDbContextFactory.Create();
        var provider = Google();
        var state = await BeginAsync(db, provider);
        await CreateCallbackHandler(db, provider).Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "c", state, null), CancellationToken.None);

        var replay = await CreateCallbackHandler(db, provider).Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "c", state, null), CancellationToken.None);

        Assert.Equal(FrontendCallbackUrl + "?error=invalid_state", replay.Value);
    }

    [Fact]
    public async Task Provider_failures_and_missing_emails_redirect_with_an_error()
    {
        await using var db = TestDbContextFactory.Create();
        var failing = Google();
        failing.ThrowOnExchange = true;
        var noEmail = new FakeExternalAuthProvider(
            ExternalAuthProvider.Google, new ExternalAuthUserInfo(Guid.NewGuid().ToString("N"), "", false, "No Email"));

        var providerError = await CreateCallbackHandler(db, failing).Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "c", await BeginAsync(db, failing), null), CancellationToken.None);
        var emailRequired = await CreateCallbackHandler(db, noEmail).Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "c", await BeginAsync(db, noEmail), null), CancellationToken.None);

        Assert.Equal(FrontendCallbackUrl + "?error=provider_error", providerError.Value);
        Assert.Equal(FrontendCallbackUrl + "?error=email_required", emailRequired.Value);
    }

    [Fact]
    public async Task An_unfinished_or_expired_code_cannot_be_exchanged()
    {
        await using var db = TestDbContextFactory.Create();
        var provider = Google();
        var pendingState = await BeginAsync(db, provider);
        var expired = new ExternalAuthTransaction
        {
            Id = Guid.NewGuid().ToString("N"), Provider = ExternalAuthProvider.Google, Status = ExternalAuthTransactionStatus.Completed,
            UserId = (await TestUserFactory.SeedAsync(db)).Id, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        db.ExternalAuthTransactions.Add(expired);
        await db.SaveChangesAsync();

        var pending = await CreateExchangeHandler(db).Handle(new ExchangeExternalAuthCode.Command(pendingState), CancellationToken.None);
        var late = await CreateExchangeHandler(db).Handle(new ExchangeExternalAuthCode.Command(expired.Id), CancellationToken.None);

        Assert.Equal(AuthMessageKeys.ExternalAuthCodeInvalidOrExpired, pending.Error.Code);
        Assert.Equal(AuthMessageKeys.ExternalAuthCodeInvalidOrExpired, late.Error.Code);
    }

    [Fact]
    public async Task An_unverified_email_that_belongs_to_an_existing_account_is_refused()
    {
        await using var db = TestDbContextFactory.Create();
        var existing = await TestUserFactory.SeedAsync(db, $"taken-{Guid.NewGuid():N}@example.com", "Passw0rd!x");
        var provider = Google(email: existing.Email, emailVerified: false);

        var result = await CreateCallbackHandler(db, provider).Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "c", await BeginAsync(db, provider), null), CancellationToken.None);

        Assert.Equal(FrontendCallbackUrl + "?error=email_in_use", result.Value);
        Assert.False(await db.ExternalLogins.AnyAsync(l => l.UserId == existing.Id));
    }

    [Fact]
    public async Task An_unverified_email_still_creates_a_new_unverified_account()
    {
        await using var db = TestDbContextFactory.Create();
        var provider = Google(emailVerified: false);

        await CreateCallbackHandler(db, provider).Handle(
            new ExternalAuthCallback.Command(ExternalAuthProvider.Google, "c", await BeginAsync(db, provider), null), CancellationToken.None);

        Assert.False((await db.Users.SingleAsync(u => u.Email == provider.UserInfo.Email)).IsEmailVerified);
    }

    private sealed class FakeExternalAuthProvider(ExternalAuthProvider provider, ExternalAuthUserInfo userInfo) : IExternalAuthProvider
    {
        public ExternalAuthUserInfo UserInfo => userInfo;

        public bool ThrowOnExchange { get; set; }

        public int ExchangeCallCount { get; private set; }

        public ExternalAuthProvider Provider => provider;

        public string GetAuthorizeUrl(string state) => $"https://provider.test/authorize?state={state}";

        public Task<ExternalAuthUserInfo> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
        {
            ExchangeCallCount++;
            return ThrowOnExchange ? throw new HttpRequestException("provider down") : Task.FromResult(userInfo);
        }
    }
}
