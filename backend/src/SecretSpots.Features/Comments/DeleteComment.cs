using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Comments;

public static class DeleteComment
{
    public record Command(Guid CommentId) : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var comment = await db.Comments.SingleOrDefaultAsync(c => c.Id == command.CommentId && !c.IsDeleted, cancellationToken);
            if (comment is null)
            {
                return Result<Unit>.Failure(new Error(
                    CommentsMessageKeys.NotFound,
                    localizer[CommentsMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            if (comment.UserId != userContext.UserId)
            {
                return Result<Unit>.Failure(new Error(
                    CommentsMessageKeys.NotYourComment,
                    localizer[CommentsMessageKeys.NotYourComment].Value,
                    StatusCodes.Status403Forbidden));
            }

            // Soft delete — keeps the row (unlike Spots' hard DeleteSpot) for audit purposes,
            // but GetSpotComments filters IsDeleted out, so it still disappears from the UI.
            comment.IsDeleted = true;
            comment.UpdatedAt = DateTimeOffset.UtcNow;

            // Otherwise a reported comment's author could delete it themselves to dodge
            // moderation, leaving the report stuck unresolved forever — same fix as
            // SpotDeletionCleanup applies for a deleted spot's reports.
            await db.Reports
                .Where(r => r.ContentType == ReportedContentType.Comment && r.ContentId == comment.Id && r.ResolvedAt == null)
                .ExecuteUpdateAsync(r => r
                    .SetProperty(x => x.ResolvedAt, DateTimeOffset.UtcNow)
                    .SetProperty(x => x.ResolutionAction, ReportResolutionAction.ContentDeletedByAuthor), cancellationToken);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(CommentsLogMessages.CommentDeleted, comment.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
