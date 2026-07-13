using FluentValidation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Businesses;

public static class CreateBusiness
{
    public record Command(
        string Name,
        string Description,
        double Latitude,
        double Longitude) : IRequest<BusinessResponse>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer)
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage(localizer[BusinessesMessageKeys.NameRequired].Value)
                .MaximumLength(100).WithMessage(localizer[BusinessesMessageKeys.NameTooLong].Value);

            RuleFor(c => c.Description)
                .NotEmpty().WithMessage(localizer[BusinessesMessageKeys.DescriptionRequired].Value)
                .MaximumLength(2000).WithMessage(localizer[BusinessesMessageKeys.DescriptionTooLong].Value);

            RuleFor(c => c.Latitude)
                .InclusiveBetween(-90, 90).WithMessage(localizer[GeoMessageKeys.LatitudeOutOfRange].Value);

            RuleFor(c => c.Longitude)
                .InclusiveBetween(-180, 180).WithMessage(localizer[GeoMessageKeys.LongitudeOutOfRange].Value);
        }
    }

    public class Handler(IAppDbContext db, IUserContext userContext, ILogger<Handler> logger)
        : IRequestHandler<Command, BusinessResponse>
    {
        public async Task<BusinessResponse> Handle(Command command, CancellationToken cancellationToken)
        {
            var location = new Point(command.Longitude, command.Latitude) { SRID = 4326 };

            var business = new Business
            {
                Id = Guid.NewGuid(),
                Name = command.Name.Trim(),
                Description = command.Description.Trim(),
                Location = location,
                OwnerUserId = userContext.UserId,
            };

            db.Businesses.Add(business);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(BusinessesLogMessages.BusinessCreated, business.Id, business.OwnerUserId);

            return new BusinessResponse(
                business.Id,
                business.Name,
                business.Description,
                location.Y,
                location.X,
                business.OwnerUserId,
                business.IsPromoted,
                business.CreatedAt);
        }
    }
}
