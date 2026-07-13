using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Features.Businesses;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Businesses;

public class CreateBusinessValidatorTests
{
    private readonly CreateBusiness.Validator _validator = new(TestLocalizerFactory.Create());

    [Fact]
    public void Name_is_required()
    {
        var result = _validator.TestValidate(new CreateBusiness.Command("", "Desc", 42.6977, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Description_is_required()
    {
        var result = _validator.TestValidate(new CreateBusiness.Command("Name", "", 42.6977, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Latitude_out_of_range(double latitude)
    {
        var result = _validator.TestValidate(new CreateBusiness.Command("Name", "Desc", latitude, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.Latitude);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Longitude_out_of_range(double longitude)
    {
        var result = _validator.TestValidate(new CreateBusiness.Command("Name", "Desc", 42.6977, longitude));
        result.ShouldHaveValidationErrorFor(c => c.Longitude);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(new CreateBusiness.Command("Name", "Desc", 42.6977, 23.3219));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateBusinessHandlerTests
{
    [Fact]
    public async Task Creates_a_business_owned_by_the_current_user()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var handler = new CreateBusiness.Handler(db, new FakeUserContext(userId), NullLogger<CreateBusiness.Handler>.Instance);

        var command = new CreateBusiness.Command("Hizha Vitosha", "A mountain hut", 42.6461, 23.2445);
        var response = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(userId, response.OwnerUserId);
        Assert.Equal(command.Latitude, response.Latitude, precision: 6);
        Assert.Equal(command.Longitude, response.Longitude, precision: 6);

        var saved = await db.Businesses.SingleAsync(b => b.Id == response.Id);
        Assert.Equal(userId, saved.OwnerUserId);
    }
}
