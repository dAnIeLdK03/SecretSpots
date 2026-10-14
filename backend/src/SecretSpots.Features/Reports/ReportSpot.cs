using FluentValidation;
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

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<ReportResponse>>
    {
        public async Task<Result<ReportResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spotExists = await db.Spots.AnyAsync(s => s.Id == command.SpotId, cancellationToken);
            if (!spotExists)
            {
                return Result<ReportResponse>.Failure(Error.NotFound(ReportsMessageKeys.SpotNotFound, localizer));
            }

            return await ReportContentHandler.CreateAsync(
                db, userContext, localizer, logger,
                ReportedContentType.Spot, command.SpotId, command.SpotId,
                command.Reason, command.Details, cancellationToken);
        }
    }
}
