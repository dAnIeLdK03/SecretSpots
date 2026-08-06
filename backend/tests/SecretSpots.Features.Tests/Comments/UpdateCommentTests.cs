using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Domain;
using SecretSpots.Features.Comments;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Comments;

public class UpdateCommentValidatorTests
{
    private readonly UpdateComment.Validator _validator = new(TestLocalizerFactory.Create(), TestOptionsFactory.Comment());

    [Fact]
    public void Empty_text_is_invalid()
    {
        var result = _validator.TestValidate(new UpdateComment.Command(Guid.NewGuid(), ""));
        result.ShouldHaveValidationErrorFor(c => c.Text);
    }

    [Fact]
    public void Valid_command_has_no_errors()
    {
        var result = _validator.TestValidate(new UpdateComment.Command(Guid.NewGuid(), "Edited text"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateCommentHandlerTests
{
    private static async Task<Comment> SeedCommentAsync(IAppDbContext db, Guid spotId, Guid userId, string text = "Original") =>
        await TestCommentFactory.SeedAsync(db, spotId, userId, text);

    private static UpdateComment.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<UpdateComment.Handler>.Instance);

    [Fact]
    public async Task Author_can_edit_their_comment()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");
        var comment = await SeedCommentAsync(db, Guid.NewGuid(), user.Id);

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new UpdateComment.Command(comment.Id, "  Updated text  "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated text", result.Value.Text);
        Assert.NotNull(result.Value.UpdatedAt);

        var saved = await db.Comments.SingleAsync(c => c.Id == comment.Id);
        Assert.Equal("Updated text", saved.Text);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task Nonexistent_comment_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(new UpdateComment.Command(Guid.NewGuid(), "text"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CommentsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Non_author_cannot_edit_the_comment()
    {
        await using var db = TestDbContextFactory.Create();
        var author = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");
        var comment = await SeedCommentAsync(db, Guid.NewGuid(), author.Id);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(new UpdateComment.Command(comment.Id, "text"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CommentsMessageKeys.NotYourComment, result.Error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error.StatusCode);

        var saved = await db.Comments.SingleAsync(c => c.Id == comment.Id);
        Assert.Equal("Original", saved.Text);
    }

    [Fact]
    public async Task Deleted_comment_cannot_be_edited()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");
        var comment = await TestCommentFactory.SeedAsync(db, Guid.NewGuid(), user.Id, isDeleted: true);

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new UpdateComment.Command(comment.Id, "text"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CommentsMessageKeys.NotFound, result.Error.Code);
    }
}
