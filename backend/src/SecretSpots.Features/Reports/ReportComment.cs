using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using WebPush;

namespace SecretSpots.Features.Reports;

public static class ReportComment
{
    private const int MaxDetailsLength = 500;

    public record RequestBody(ReportReason Reason, string? Details);

    public record Command(Guid CommentId, ReportReason Reason, string? Details) : IRequest<Result<ReportResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Reason)
                .IsInEnum().WithMessage(localizer[ReportsMessageKeys.ReasonRequired].Value);

            RuleFor(c => c.Details)
                .MaximumLength(MaxDetailsLength).WithMessage(localizer[ReportsMessageKeys.DetailsTooLong].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        WebPushClient webPushClient,
        IOptions<WebPushOptions> webPushOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<ReportResponse>>
    {
        public async Task<Result<ReportResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var comment = await db.Comments.SingleOrDefaultAsync(c => c.Id == command.CommentId && !c.IsDeleted, cancellationToken);
            if (comment is null)
            {
                return Result<ReportResponse>.Failure(new Error(
                    ReportsMessageKeys.CommentNotFound,
                    localizer[ReportsMessageKeys.CommentNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var alreadyReported = await db.Reports.AnyAsync(
                r => r.ContentType == ReportedContentType.Comment
                     && r.ContentId == command.CommentId
                     && r.ReporterUserId == userContext.UserId,
                cancellationToken);
            if (alreadyReported)
            {
                return Result<ReportResponse>.Failure(new Error(
                    ReportsMessageKeys.AlreadyReported,
                    localizer[ReportsMessageKeys.AlreadyReported].Value,
                    StatusCodes.Status409Conflict));
            }

            var report = new Report
            {
                Id = Guid.NewGuid(),
                ContentType = ReportedContentType.Comment,
                ContentId = command.CommentId,
                ReporterUserId = userContext.UserId,
                Reason = command.Reason,
                Details = command.Details?.Trim(),
            };

            db.Reports.Add(report);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Concurrent duplicate report from the same user caught by the unique index.
                return Result<ReportResponse>.Failure(new Error(
                    ReportsMessageKeys.AlreadyReported,
                    localizer[ReportsMessageKeys.AlreadyReported].Value,
                    StatusCodes.Status409Conflict));
            }

            logger.LogInformation(ReportsLogMessages.ContentReported, report.ContentType, report.ContentId, report.Reason, userContext.UserId);

            await ReportAdminNotifier.NotifyAsync(db, webPushClient, webPushOptions, localizer, logger, comment.SpotId, cancellationToken);

            return Result<ReportResponse>.Success(new ReportResponse(report.Id));
        }
    }
}
