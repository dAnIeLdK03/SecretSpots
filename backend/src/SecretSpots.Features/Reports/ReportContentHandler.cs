using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Reports;

internal static class ReportContentHandler
{
    public static async Task<Result<ReportResponse>> CreateAsync(
        IAppDbContext db,
        IUserContext userContext,
        IStringLocalizer<SharedResources> localizer,
        ILogger logger,
        ReportedContentType contentType,
        Guid contentId,
        Guid relatedSpotId,
        ReportReason reason,
        string? details,
        CancellationToken cancellationToken)
    {
        var alreadyReported = await db.Reports.AnyAsync(
            r => r.ContentType == contentType
                 && r.ContentId == contentId
                 && r.ReporterUserId == userContext.UserId,
            cancellationToken);
        if (alreadyReported)
        {
            return Result<ReportResponse>.Failure(Error.Conflict(ReportsMessageKeys.AlreadyReported, localizer));
        }

        var report = new Report
        {
            Id = Guid.NewGuid(),
            ContentType = contentType,
            ContentId = contentId,
            ReporterUserId = userContext.UserId,
            Reason = reason,
            Details = details?.Trim(),
        };

        db.Reports.Add(report);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Concurrent duplicate report from the same user caught by the unique index.
            return Result<ReportResponse>.Failure(Error.Conflict(ReportsMessageKeys.AlreadyReported, localizer));
        }

        logger.LogInformation(ReportsLogMessages.ContentReported, report.ContentType, report.ContentId, report.Reason, userContext.UserId);

        await ReportAdminNotifier.NotifyAsync(db, relatedSpotId, cancellationToken);

        return Result<ReportResponse>.Success(new ReportResponse(report.Id));
    }
}
