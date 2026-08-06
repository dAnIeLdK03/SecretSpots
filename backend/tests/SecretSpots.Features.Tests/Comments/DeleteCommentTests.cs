using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Features.Comments;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Comments;

public class DeleteCommentTests
{
    private static DeleteComment.Handler CreateHandler(IAppDbContext db, Guid userId) =>
        new(db, new FakeUserContext(userId), TestLocalizerFactory.Create(), NullLogger<DeleteComment.Handler>.Instance);

    [Fact]
    public async Task Author_can_delete_their_comment_and_it_is_soft_deleted_not_removed()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");
        var comment = await TestCommentFactory.SeedAsync(db, Guid.NewGuid(), user.Id);

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new DeleteComment.Command(comment.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var saved = await db.Comments.SingleAsync(c => c.Id == comment.Id);
        Assert.True(saved.IsDeleted);
        Assert.NotNull(saved.UpdatedAt);
    }

    [Fact]
    public async Task Nonexistent_comment_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var handler = CreateHandler(db, Guid.NewGuid());

        var result = await handler.Handle(new DeleteComment.Command(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CommentsMessageKeys.NotFound, result.Error.Code);
        Assert.Equal(StatusCodes.Status404NotFound, result.Error.StatusCode);
    }

    [Fact]
    public async Task Non_author_cannot_delete_the_comment()
    {
        await using var db = TestDbContextFactory.Create();
        var author = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");
        var comment = await TestCommentFactory.SeedAsync(db, Guid.NewGuid(), author.Id);

        var handler = CreateHandler(db, Guid.NewGuid());
        var result = await handler.Handle(new DeleteComment.Command(comment.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CommentsMessageKeys.NotYourComment, result.Error.Code);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Error.StatusCode);

        var saved = await db.Comments.SingleAsync(c => c.Id == comment.Id);
        Assert.False(saved.IsDeleted);
    }

    [Fact]
    public async Task Already_deleted_comment_returns_not_found()
    {
        await using var db = TestDbContextFactory.Create();
        var user = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");
        var comment = await TestCommentFactory.SeedAsync(db, Guid.NewGuid(), user.Id, isDeleted: true);

        var handler = CreateHandler(db, user.Id);
        var result = await handler.Handle(new DeleteComment.Command(comment.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CommentsMessageKeys.NotFound, result.Error.Code);
    }
}
