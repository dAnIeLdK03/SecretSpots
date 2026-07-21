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
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Spots;

public static class UpdateSpot
{
    // SpotId comes from the route, not the request body — kept as a separate
    // record so the endpoint can bind RequestBody from JSON and Command from both.
    // Coordinates are intentionally not editable here — changing them would break
    // the distance check against check-ins already recorded against this spot.
    public record RequestBody(string Name, string Description, SpotCategory Category, string PhotoUrl);

    public record Command(
        Guid SpotId,
        string Name,
        string Description,
        SpotCategory Category,
        string PhotoUrl) : IRequest<Result<SpotResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage(localizer[SpotsMessageKeys.NameRequired].Value)
                .MaximumLength(100).WithMessage(localizer[SpotsMessageKeys.NameTooLong].Value);

            RuleFor(c => c.Description)
                .NotEmpty().WithMessage(localizer[SpotsMessageKeys.DescriptionRequired].Value)
                .MaximumLength(2000).WithMessage(localizer[SpotsMessageKeys.DescriptionTooLong].Value);

            RuleFor(c => c.PhotoUrl)
                .NotEmpty().WithMessage(localizer[SpotsMessageKeys.PhotoUrlRequired].Value)
                .Must(UrlValidation.IsHttpUrl).WithMessage(localizer[SpotsMessageKeys.PhotoUrlInvalid].Value);

            RuleFor(c => c.Category)
                .IsInEnum().WithMessage(localizer[SpotsMessageKeys.InvalidCategory].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext, IStringLocalizer<SharedResources> localizer, ILogger<Handler> logger)
        : IRequestHandler<Command, Result<SpotResponse>>
    {
        public async Task<Result<SpotResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<SpotResponse>.Failure(new Error(
                    SpotsMessageKeys.NotFound,
                    localizer[SpotsMessageKeys.NotFound].Value,
                    StatusCodes.Status404NotFound));
            }

            if (spot.CreatedByUserId != userContext.UserId)
            {
                return Result<SpotResponse>.Failure(new Error(
                    SpotsMessageKeys.NotYourSpot,
                    localizer[SpotsMessageKeys.NotYourSpot].Value,
                    StatusCodes.Status403Forbidden));
            }

            spot.Name = command.Name.Trim();
            spot.Description = command.Description.Trim();
            spot.Category = command.Category;
            spot.PhotoUrl = command.PhotoUrl;

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(SpotsLogMessages.SpotUpdated, spot.Id, userContext.UserId);

            return Result<SpotResponse>.Success(new SpotResponse(
                spot.Id,
                spot.Name,
                spot.Description,
                spot.Category,
                spot.PhotoUrl,
                spot.Location.Y,
                spot.Location.X,
                spot.CreatedByUserId,
                spot.CreatedAt));
        }
    }
}
