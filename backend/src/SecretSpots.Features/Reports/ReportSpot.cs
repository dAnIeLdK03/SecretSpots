using FluentValidation;
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

namespace SecretSpots.Features.Reports;

public static class ReportSpot
{
    private const int MaxDetailsLength = 500;

    public record RequestBody(ReportReason Reason, string? Details);

    public record Command(Guid SpotId, ReportReason Reason, string? Details) : IRequest<Result<ReportResponse>>;

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

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<ReportResponse>>
    {
        public async Task<Result<ReportResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spotExists = await db.Spots.AnyAsync(s => s.Id == command.SpotId, cancellationToken);
            if (!spotExists)
            {
                return Result<ReportResponse>.Failure(new Error(
                    ReportsMessageKeys.SpotNotFound,
                    localizer[ReportsMessageKeys.SpotNotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            var alreadyReported = await db.Reports.AnyAsync(
                r => r.ContentType == ReportedContentType.Spot
                     && r.ContentId == command.SpotId
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
                ContentType = ReportedContentType.Spot,
                ContentId = command.SpotId,
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

            return Result<ReportResponse>.Success(new ReportResponse(report.Id));
        }
    }
}
