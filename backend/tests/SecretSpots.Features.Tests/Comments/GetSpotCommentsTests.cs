using FluentValidation.TestHelper;
using SecretSpots.Domain;
using SecretSpots.Features.Comments;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Tests.TestSupport;

namespace SecretSpots.Features.Tests.Comments;

public class GetSpotCommentsValidatorTests
{
    private readonly GetSpotComments.Validator _validator =
        new(TestLocalizerFactory.Create(), TestOptionsFactory.Comment());

    [Fact]
    public void Page_below_one_is_invalid()
    {
        var result = _validator.TestValidate(new GetSpotComments.Query(Guid.NewGuid(), 0, 20));
        result.ShouldHaveValidationErrorFor(q => q.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_out_of_range_is_invalid(int pageSize)
    {
        var result = _validator.TestValidate(new GetSpotComments.Query(Guid.NewGuid(), 1, pageSize));
        result.ShouldHaveValidationErrorFor(q => q.PageSize);
    }

    [Fact]
    public void Valid_query_has_no_errors()
    {
        var result = _validator.TestValidate(new GetSpotComments.Query(Guid.NewGuid(), 1, 20));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class GetSpotCommentsHandlerTests
{
    private static async Task<Comment> SeedCommentAsync(
        IAppDbContext db, Guid spotId, Guid userId, string text = "Nice!", bool isDeleted = false)
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            SpotId = spotId,
            UserId = userId,
            Text = text,
            IsDeleted = isDeleted,
        };

        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        return comment;
    }

    private static GetSpotComments.Handler CreateHandler(IAppDbContext db) => new(db);

    [Fact]
    public async Task Returns_only_comments_for_the_given_spot_newest_first_with_author_name()
    {
        await using var db = TestDbContextFactory.Create();
        var spotId = Guid.NewGuid();
        var otherSpotId = Guid.NewGuid();
        var user = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");

        var older = await SeedCommentAsync(db, spotId, user.Id, "First!");
        await Task.Delay(10);
        var newer = await SeedCommentAsync(db, spotId, user.Id, "Second!");
        await SeedCommentAsync(db, otherSpotId, user.Id, "Wrong spot");

        var handler = CreateHandler(db);
        var result = await handler.Handle(new GetSpotComments.Query(spotId, 1, 20), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(newer.Id, result.Items[0].Id);
        Assert.Equal(older.Id, result.Items[1].Id);
        Assert.Equal(user.DisplayName, result.Items[0].AuthorDisplayName);
    }

    [Fact]
    public async Task Deleted_comments_are_excluded()
    {
        await using var db = TestDbContextFactory.Create();
        var spotId = Guid.NewGuid();
        var user = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");

        await SeedCommentAsync(db, spotId, user.Id, "Visible");
        await SeedCommentAsync(db, spotId, user.Id, "Deleted", isDeleted: true);

        var handler = CreateHandler(db);
        var result = await handler.Handle(new GetSpotComments.Query(spotId, 1, 20), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Visible", result.Items[0].Text);
    }

    [Fact]
    public async Task Pagination_slices_correctly_and_reports_total_count()
    {
        await using var db = TestDbContextFactory.Create();
        var spotId = Guid.NewGuid();
        var user = await TestUserFactory.SeedAsync(db, $"commenter-{Guid.NewGuid():N}@example.com", "Str0ng!Passw0rd1");

        for (var i = 0; i < 5; i++)
        {
            await SeedCommentAsync(db, spotId, user.Id, $"Comment {i}");
            await Task.Delay(5);
        }

        var handler = CreateHandler(db);
        var page1 = await handler.Handle(new GetSpotComments.Query(spotId, 1, 2), CancellationToken.None);
        var page2 = await handler.Handle(new GetSpotComments.Query(spotId, 2, 2), CancellationToken.None);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.DoesNotContain(page1.Items[0].Id, page2.Items.Select(i => i.Id));
    }
}
