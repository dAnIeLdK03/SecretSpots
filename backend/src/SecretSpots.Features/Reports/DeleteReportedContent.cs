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
using SecretSpots.Features.Common.Storage;
using SecretSpots.Features.Spots;

namespace SecretSpots.Features.Reports;

public static class DeleteReportedContent
{
    public record Command(Guid ReportId) : IRequest<Result<Unit>>;

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IPhotoStorage photoStorage,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<Unit>>
    {
        public async Task<Result<Unit>> Handle(Command command, CancellationToken cancellationToken)
        {
            var report = await db.Reports.SingleOrDefaultAsync(r => r.Id == command.ReportId, cancellationToken);
            if (report is null)
            {
                return Result<Unit>.Failure(new Error(
                    ReportsMessageKeys.NotFound,
                    localizer[ReportsMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            if (report.ContentType == ReportedContentType.Spot)
            {
                // Deliberately bypasses DeleteSpot's ownership check — an admin acting on a
                // report removes content regardless of who created it. Reuses the same cascade
                // cleanup as a self-service delete (comments/ratings/saved/check-ins/photos) so
                // the two paths can't drift apart.
                var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == report.ContentId, cancellationToken);
                if (spot is not null)
                {
                    await SpotDeletionCleanup.DeleteAsync(db, photoStorage, spot, logger, cancellationToken);
                }
            }
            else
            {
                var comment = await db.Comments
                    .SingleOrDefaultAsync(c => c.Id == report.ContentId && !c.IsDeleted, cancellationToken);
                if (comment is not null)
                {
                    comment.IsDeleted = true;
                    comment.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }

            var now = DateTimeOffset.UtcNow;

            // Every other still-open report about this same piece of content is moot once it's
            // gone — resolve them all, not just the one the admin happened to click. They're all
            // attributed to this admin/action even though only one was clicked directly, since
            // the content removal is what actually resolved them.
            await db.Reports
                .Where(r => r.ContentType == report.ContentType && r.ContentId == report.ContentId && r.ResolvedAt == null)
                .ExecuteUpdateAsync(r => r
                    .SetProperty(x => x.ResolvedAt, now)
                    .SetProperty(x => x.ResolvedByUserId, userContext.UserId)
                    .SetProperty(x => x.ResolutionAction, ReportResolutionAction.ContentDeleted), cancellationToken);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                ReportsLogMessages.ReportedContentDeleted, report.ContentType, report.ContentId, userContext.UserId, report.Id);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
