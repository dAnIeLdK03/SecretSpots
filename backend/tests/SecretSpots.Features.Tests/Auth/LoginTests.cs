using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Auth;

public class LoginValidatorTests
{
    private readonly Login.Validator _validator = new(TestLocalizerFactory.Create());

    [Fact]
    public void Email_is_required()
    {
        var result = _validator.TestValidate(new Login.Command("", "Str0ng!Passw0rd1"));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Password_is_required()
    {
        var result = _validator.TestValidate(new Login.Command("user@example.com", ""));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(new Login.Command("user@example.com", "Str0ng!Passw0rd1"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class LoginHandlerTests
{
    private static Login.Handler CreateHandler(SecretSpots.Features.Common.Persistence.IAppDbContext db)
    {
        var jwtOptions = TestOptionsFactory.Jwt();
        return new Login.Handler(
            db,
            new JwtService(jwtOptions),
            jwtOptions,
            TestLocalizerFactory.Create(),
            NullLogger<Login.Handler>.Instance);
    }

    [Fact]
    public async Task Successful_login_issues_tokens()
    {
        await using var db = TestDbContextFactory.Create();
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await TestUserFactory.SeedAsync(db, email, "Str0ng!Passw0rd1");

        var handler = CreateHandler(db);
        var result = await handler.Handle(new Login.Command(email, "Str0ng!Passw0rd1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        await using var db = TestDbContextFactory.Create();
        var email = $"login-wrong-{Guid.NewGuid():N}@example.com";
        await TestUserFactory.SeedAsync(db, email, "Str0ng!Passw0rd1");

        var handler = CreateHandler(db);
        var result = await handler.Handle(new Login.Command(email, "TotallyWrong1!"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthMessageKeys.InvalidCredentials, result.Error.Code);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.Error.StatusCode);
    }

    [Fact]
    public async Task Unknown_email_returns_the_same_error_as_wrong_password()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db);

        var result = await handler.Handle(
            new Login.Command($"no-such-user-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthMessageKeys.InvalidCredentials, result.Error.Code);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.Error.StatusCode);
    }
}
