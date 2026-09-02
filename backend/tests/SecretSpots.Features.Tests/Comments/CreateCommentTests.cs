using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Auth;
using SecretSpots.Features.Comments;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;
using WebPush;

namespace SecretSpots.Features.Tests.Comments;

public class CreateCommentValidatorTests
{
    private readonly CreateComment.Validator _validator = new(TestLocalizerFactory.Create(), TestOptionsFactory.Comment());

    [Fact]
    public void Empty_text_is_invalid()
    {
        var result = _validator.TestValidate(new CreateComment.Command(Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(c => c.Text);
    }

    [Fact]
    public void Text_over_max_length_is_invalid()
    {
        var result = _validator.TestValidate(
            new CreateComment.Command(Guid.NewGuid(), new string('a', 1001)));
        result.ShouldHaveValidationErrorFor(c => c.Text);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(new CreateComment.Command(Guid.NewGuid(), "Great spot!"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class CreateCommentHandlerTests
{
    private static async Task<Spot> SeedSpotAsync(IAppDbContext db)
    {
        var spot = new Spot
        {
            Id = Guid.NewGuid(),
            Name = $"Spot-{Guid.NewGuid():N}",
            Description = "test",
            Category = SpotCategory.Nature,
            PhotoUrls = ["https://example.com/photo.jpg"],
            Location = new Point(23.3219, 42.6977) { SRID = 4326 },
            CreatedByUserId = Guid.NewGuid(),
        };

        db.Spots.Add(spot);
        await db.SaveChangesAsync();

        return spot;
    }

    private static CreateComment.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), new WebPushClient(), TestOptionsFactory.WebPush(),
            TestLocalizerFactory.Create(), NullLogger<CreateComment.Handler>.Instance);

    [Fact]
    public async Task Successful_comment_is_persisted_with_author_display_name()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await SeedSpotAsync(db);
        var user = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new CreateComment.Command(spot.Id, "  Nice waterfall!  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Nice waterfall!", result.Value.Text);
        Assert.Equal(user.DisplayName, result.Value.AuthorDisplayName);
        Assert.Equal(spot.Id, result.Value.SpotId);

        var saved = await db.Comments.SingleAsync(c => c.Id == result.Value.Id);
        Assert.Equal(user.Id, saved.UserId);
        Assert.False(saved.IsDeleted);
        Assert.Null(saved.UpdatedAt);
    }

    [Fact]
    public async Task Comment_on_nonexistent_spot_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new CreateComment.Command(Guid.NewGuid(), "Nice!"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CommentsMessageKeys.SpotNotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Comment_from_a_user_missing_from_the_database_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var spot = await SeedSpotAsync(db);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(new CreateComment.Command(spot.Id, "Nice!"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(AuthMessageKeys.UserNotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }
}
