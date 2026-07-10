using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Spots;

public static class CreateSpot
{
    public record Command(
        string Name,
        string Description,
        SpotCategory Category,
        string PhotoUrl,
        double Latitude,
        double Longitude) : IRequest<SpotResponse>;

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
                .Must(BeAValidUrl).WithMessage(localizer[SpotsMessageKeys.PhotoUrlInvalid].Value);

            RuleFor(c => c.Category)
                .IsInEnum().WithMessage(localizer[SpotsMessageKeys.InvalidCategory].Value);

            RuleFor(c => c.Latitude)
                .InclusiveBetween(-90, 90).WithMessage(localizer[SpotsMessageKeys.LatitudeOutOfRange].Value);

            RuleFor(c => c.Longitude)
                .InclusiveBetween(-180, 180).WithMessage(localizer[SpotsMessageKeys.LongitudeOutOfRange].Value);
        }

        private static bool BeAValidUrl(string photoUrl)
        {
            return Uri.TryCreate(photoUrl, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext, ILogger<Handler> logger)
        : IRequestHandler<Command, SpotResponse>
    {
        public async Task<SpotResponse> Handle(Command command, CancellationToken cancellationToken)
        {
            var location = new Point(command.Longitude, command.Latitude) { SRID = 4326 };

            var spot = new Spot
            {
                Id = Guid.NewGuid(),
                Name = command.Name.Trim(),
                Description = command.Description.Trim(),
                Category = command.Category,
                PhotoUrl = command.PhotoUrl,
                Location = location,
                CreatedByUserId = userContext.UserId,
            };

            db.Spots.Add(spot);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(SpotsLogMessages.SpotCreated, spot.Id, spot.Category, spot.CreatedByUserId);

            return new SpotResponse(
                spot.Id,
                spot.Name,
                spot.Description,
                spot.Category,
                spot.PhotoUrl,
                location.Y,
                location.X,
                spot.CreatedByUserId,
                spot.CreatedAt);
        }
    }
}
