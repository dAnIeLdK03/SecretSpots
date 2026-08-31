using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Auth;

public class RefreshAccessTokenHandlerTests
{
    private static RefreshAccessToken.Handler CreateHandler(IAppDbContext db)
    {
        var jwtOptions = TestOptionsFactory.Jwt();
        return new RefreshAccessToken.Handler(
            db,
            new JwtService(jwtOptions),
            jwtOptions,
            TestLocalizerFactory.Create(),
            NullLogger<RefreshAccessToken.Handler>.Instance);
    }

    private static async Task<(RefreshToken Entity, string RawToken)> SeedRefreshTokenAsync(
        IAppDbContext db, Guid userId, DateTimeOffset expiresAt, DateTimeOffset? revokedAt = null)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = OpaqueTokenHasher.Hash(rawToken),
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
        };

        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        return (token, rawToken);
    }

    [Fact]
    public async Task Valid_refresh_token_rotates_to_a_new_token_pair()
    {
        await using var db = TestDbContextFactory.Create();
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        var user = await TestUserFactory.SeedAsync(db, email, "Str0ng!Passw0rd1");
        var (oldToken, rawOldToken) = await SeedRefreshTokenAsync(db, user.Id, DateTimeOffset.UtcNow.AddDays(1));

        var handler = CreateHandler(db);
        var result = await handler.Handle(new RefreshAccessToken.Command(rawOldToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEqual(rawOldToken, result.Value.RefreshToken);

        var reloaded = await db.RefreshTokens.FindAsync(oldToken.Id);
        Assert.NotNull(reloaded!.RevokedAt);
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var email = $"refresh-expired-{Guid.NewGuid():N}@example.com";
        var user = await TestUserFactory.SeedAsync(db, email, "Str0ng!Passw0rd1");
        var (_, rawExpiredToken) = await SeedRefreshTokenAsync(db, user.Id, DateTimeOffset.UtcNow.AddDays(-1));

        var handler = CreateHandler(db);
        var result = await handler.Handle(new RefreshAccessToken.Command(rawExpiredToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthMessageKeys.InvalidOrExpiredRefreshToken, result.Error.Code);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.Error.StatusCode);
    }

    [Fact]
    public async Task Already_revoked_refresh_token_is_rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var email = $"refresh-revoked-{Guid.NewGuid():N}@example.com";
        var user = await TestUserFactory.SeedAsync(db, email, "Str0ng!Passw0rd1");
        var (_, rawRevokedToken) = await SeedRefreshTokenAsync(
            db, user.Id, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddMinutes(-1));

        var handler = CreateHandler(db);
        var result = await handler.Handle(new RefreshAccessToken.Command(rawRevokedToken), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthMessageKeys.InvalidOrExpiredRefreshToken, result.Error.Code);
    }

    [Fact]
    public async Task Unknown_refresh_token_is_rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db);

        var result = await handler.Handle(
            new RefreshAccessToken.Command("this-token-does-not-exist"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthMessageKeys.InvalidOrExpiredRefreshToken, result.Error.Code);
    }
}
