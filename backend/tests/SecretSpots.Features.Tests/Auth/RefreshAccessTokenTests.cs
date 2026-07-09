using System.Security.Cryptography;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Auth;

public class RefreshAccessTokenValidatorTests
{
    private readonly RefreshAccessToken.Validator _validator = new(TestLocalizerFactory.Create());

    [Fact]
    public void RefreshToken_is_required()
    {
        var result = _validator.TestValidate(new RefreshAccessToken.Command(""));
        result.ShouldHaveValidationErrorFor(c => c.RefreshToken);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(new RefreshAccessToken.Command("some-token-value"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class RefreshAccessTokenHandlerTests
{
    private static RefreshAccessToken.Handler CreateHandler(IAppDbContext db)
    {
        var jwtOptions = TestOptionsFactory.Jwt();
        return new RefreshAccessToken.Handler(
            db,
            new JwtService(jwtOptions),
            jwtOptions,
            TestLocalizerFactory.Create());
    }

    private static async Task<RefreshToken> SeedRefreshTokenAsync(
        IAppDbContext db, Guid userId, DateTimeOffset expiresAt, DateTimeOffset? revokedAt = null)
    {
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = expiresAt,
            RevokedAt = revokedAt,
        };

        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        return token;
    }

    [Fact]
    public async Task Valid_refresh_token_rotates_to_a_new_token_pair()
    {
        await using var db = TestDbContextFactory.Create();
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        var user = await TestUserFactory.SeedAsync(db, email, "Str0ng!Passw0rd1");
        var oldToken = await SeedRefreshTokenAsync(db, user.Id, DateTimeOffset.UtcNow.AddDays(1));

        var handler = CreateHandler(db);
        var result = await handler.Handle(new RefreshAccessToken.Command(oldToken.Token), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEqual(oldToken.Token, result.Value.RefreshToken);

        var reloaded = await db.RefreshTokens.FindAsync(oldToken.Id);
        Assert.NotNull(reloaded!.RevokedAt);
    }

    [Fact]
    public async Task Expired_refresh_token_is_rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var email = $"refresh-expired-{Guid.NewGuid():N}@example.com";
        var user = await TestUserFactory.SeedAsync(db, email, "Str0ng!Passw0rd1");
        var expiredToken = await SeedRefreshTokenAsync(db, user.Id, DateTimeOffset.UtcNow.AddDays(-1));

        var handler = CreateHandler(db);
        var result = await handler.Handle(new RefreshAccessToken.Command(expiredToken.Token), CancellationToken.None);

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
        var revokedToken = await SeedRefreshTokenAsync(
            db, user.Id, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddMinutes(-1));

        var handler = CreateHandler(db);
        var result = await handler.Handle(new RefreshAccessToken.Command(revokedToken.Token), CancellationToken.None);

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
