using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;

namespace SecretSpots.Features.Tests.TestSupport;

internal static class TestCommentFactory
{
    public static async Task<Comment> SeedAsync(
        IAppDbContext db, Guid spotId, Guid userId, string text = "Original", bool isDeleted = false)
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
}
