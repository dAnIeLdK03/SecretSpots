using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Auth;

public class RegisterValidatorTests
{
    private readonly Register.Validator _validator = new(TestLocalizerFactory.Create());

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Email_is_invalid(string email)
    {
        var result = _validator.TestValidate(new Register.Command(email, "Str0ng!Passw0rd1", "Ivana"));
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Theory]
    [InlineData("short1A")] // too short
    [InlineData("alllowercase1")] // no uppercase
    [InlineData("ALLUPPERCASE1")] // no lowercase
    [InlineData("NoDigitsHere")] // no digit
    [InlineData("Password1")] // common password
    public void Password_is_invalid(string password)
    {
        var result = _validator.TestValidate(new Register.Command("user@example.com", password, "Ivana"));
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void DisplayName_is_required()
    {
        var result = _validator.TestValidate(new Register.Command("user@example.com", "Str0ng!Passw0rd1", ""));
        result.ShouldHaveValidationErrorFor(c => c.DisplayName);
    }

    [Fact]
    public void DisplayName_cannot_exceed_50_characters()
    {
        var result = _validator.TestValidate(
            new Register.Command("user@example.com", "Str0ng!Passw0rd1", new string('a', 51)));
        result.ShouldHaveValidationErrorFor(c => c.DisplayName);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(new Register.Command("user@example.com", "Str0ng!Passw0rd1", "Ivana"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class RegisterHandlerTests
{
    private static Register.Handler CreateHandler(SecretSpots.Features.Common.Persistence.IAppDbContext db)
    {
        var jwtOptions = TestOptionsFactory.Jwt();
        return new Register.Handler(
            db,
            new JwtService(jwtOptions),
            jwtOptions,
            TestOptionsFactory.Crystals(),
            TestLocalizerFactory.Create(),
            NullLogger<Register.Handler>.Instance);
    }

    [Fact]
    public async Task Successful_registration_hashes_the_password_and_issues_tokens()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db);

        var email = $"register-{Guid.NewGuid():N}@example.com";
        var command = new Register.Command(email, "Str0ng!Passw0rd1", "Ivana Register");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value.AccessToken);
        Assert.NotEmpty(result.Value.RefreshToken);

        var savedUser = await db.Users.SingleAsync(u => u.Email == email.ToLowerInvariant());
        Assert.NotEqual("Str0ng!Passw0rd1", savedUser.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Str0ng!Passw0rd1", savedUser.PasswordHash));
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_with_a_conflict()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db);

        var email = $"duplicate-{Guid.NewGuid():N}@example.com";
        var command = new Register.Command(email, "Str0ng!Passw0rd1", "Ivana Register");

        var first = await handler.Handle(command, CancellationToken.None);
        Assert.True(first.IsSuccess);

        var second = await handler.Handle(command, CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(AuthMessageKeys.EmailAlreadyRegistered, second.Error.Code);
        Assert.Equal(StatusCodes.Status409Conflict, second.Error.StatusCode);
    }
}
