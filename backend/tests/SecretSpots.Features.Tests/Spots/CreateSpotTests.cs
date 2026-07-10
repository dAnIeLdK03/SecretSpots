using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Spots;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Spots;

public class CreateSpotValidatorTests
{
    private readonly CreateSpot.Validator _validator = new(TestLocalizerFactory.Create());

    [Fact]
    public void Name_is_required()
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", 42.6977, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Description_is_required()
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("Name", "", SpotCategory.Nature, "https://example.com/a.jpg", 42.6977, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com/a.jpg")]
    public void PhotoUrl_is_invalid(string photoUrl)
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("Name", "Desc", SpotCategory.Nature, photoUrl, 42.6977, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.PhotoUrl);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Latitude_out_of_range(double latitude)
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("Name", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", latitude, 23.3219));
        result.ShouldHaveValidationErrorFor(c => c.Latitude);
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Longitude_out_of_range(double longitude)
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("Name", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", 42.6977, longitude));
        result.ShouldHaveValidationErrorFor(c => c.Longitude);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(
            new CreateSpot.Command("Name", "Desc", SpotCategory.Nature, "https://example.com/a.jpg", 42.6977, 23.3219));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateSpotHandlerTests
{
    [Fact]
    public async Task Creates_a_spot_owned_by_the_current_user_with_the_given_coordinates()
    {
        await using var db = TestDbContextFactory.Create();
        var userId = Guid.NewGuid();
        var handler = new CreateSpot.Handler(db, new FakeUserContext(userId), NullLogger<CreateSpot.Handler>.Instance);

        var command = new CreateSpot.Command(
            "Боянски водопад",
            "Скрит водопад във Витоша",
            SpotCategory.Nature,
            "https://example.com/photo.jpg",
            42.6461,
            23.2445);

        var response = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(userId, response.CreatedByUserId);
        Assert.Equal(command.Latitude, response.Latitude, precision: 6);
        Assert.Equal(command.Longitude, response.Longitude, precision: 6);

        var saved = await db.Spots.SingleAsync(s => s.Id == response.Id);
        Assert.Equal(userId, saved.CreatedByUserId);
        Assert.Equal(SpotCategory.Nature, saved.Category);
    }
}
