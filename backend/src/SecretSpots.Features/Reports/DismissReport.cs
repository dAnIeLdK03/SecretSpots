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

public static class DismissReport
{
    public record Command(Guid ReportId) : IRequest<Result<Unit>>;

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
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

            report.ResolvedAt = DateTimeOffset.UtcNow;
            report.ResolvedByUserId = userContext.UserId;
            report.ResolutionAction = ReportResolutionAction.Dismissed;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(ReportsLogMessages.ReportDismissed, report.Id, userContext.UserId);

            return Result<Unit>.Success(Unit.Value);
        }
    }
}
