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
using SecretSpots.Features.Common.Storage;
using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Spots;

// The creator proposes an edit; nothing on the spot changes until an admin approves it
// (ApproveSpotChangeRequest). A new submission replaces any request still pending for the spot.
public static class SubmitSpotEdit
{
    // SpotId comes from the route, not the request body — kept as a separate
    // record so the endpoint can bind RequestBody from JSON and Command from both.
    // Coordinates are intentionally not editable here — changing them would break
    // the distance check against check-ins already recorded against this spot.
    public record RequestBody(string Name, string Description, SpotCategory Category, IReadOnlyList<string> PhotoUrls);

    public record Command(
        Guid SpotId,
        string Name,
        string Description,
        SpotCategory Category,
        IReadOnlyList<string> PhotoUrls) : IRequest<Result<SpotChangeRequestResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<R2Options> r2Options, IUserContext userContext)
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage(localizer[SpotsMessageKeys.NameRequired].Value)
                .MaximumLength(100).WithMessage(localizer[SpotsMessageKeys.NameTooLong].Value);

            RuleFor(c => c.Description)
                .NotEmpty().WithMessage(localizer[SpotsMessageKeys.DescriptionRequired].Value)
                .MaximumLength(2000).WithMessage(localizer[SpotsMessageKeys.DescriptionTooLong].Value);

            RuleFor(c => c.PhotoUrls)
                .NotEmpty().WithMessage(localizer[SpotsMessageKeys.PhotoUrlRequired].Value)
                .Must(urls => urls.Count <= CreateSpot.MaxPhotoCount).WithMessage(localizer[SpotsMessageKeys.PhotoUrlsTooMany].Value);

            RuleForEach(c => c.PhotoUrls)
                .Must(url => UrlValidation.IsOwnPhotoUrl(url, r2Options.Value.PublicBaseUrl, userContext.UserId))
                .WithMessage(localizer[SpotsMessageKeys.PhotoUrlInvalid].Value);

            RuleFor(c => c.Category)
                .IsInEnum().WithMessage(localizer[SpotsMessageKeys.InvalidCategory].Value);
        }
    }

    public class Handler(
        IAppDbContext db,
        IUserContext userContext,
        IPhotoStorage photoStorage,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<SpotChangeRequestResponse>>
    {
        public async Task<Result<SpotChangeRequestResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            var spot = await db.Spots.SingleOrDefaultAsync(s => s.Id == command.SpotId, cancellationToken);
            if (spot is null)
            {
                return Result<SpotChangeRequestResponse>.Failure(Error.NotFound(SpotsMessageKeys.NotFound, localizer));
            }

            if (spot.CreatedByUserId != userContext.UserId)
            {
                return Result<SpotChangeRequestResponse>.Failure(Error.Forbidden(SpotsMessageKeys.NotYourSpot, localizer));
            }

            var name = command.Name.Trim();
            var description = command.Description.Trim();
            var photoUrls = command.PhotoUrls.ToList();

            if (name == spot.Name
                && description == spot.Description
                && command.Category == spot.Category
                && photoUrls.SequenceEqual(spot.PhotoUrls))
            {
                return Result<SpotChangeRequestResponse>.Failure(Error.BadRequest(SpotsMessageKeys.NoChanges, localizer));
            }

            var request = await db.SpotChangeRequests.SingleOrDefaultAsync(
                r => r.SpotId == spot.Id && r.Status == SpotChangeRequestStatus.Pending, cancellationToken);

            var replacedPhotoUrls = request?.ProposedPhotoUrls;

            if (request is null)
            {
                request = new SpotChangeRequest
                {
                    Id = Guid.NewGuid(),
                    SpotId = spot.Id,
                    RequestedByUserId = userContext.UserId,
                };
                db.SpotChangeRequests.Add(request);
            }
            else
            {
                // Replacing the pending request — it goes back to the end of the review queue.
                request.CreatedAt = DateTimeOffset.UtcNow;
            }

            request.Type = SpotChangeRequestType.Edit;
            request.ProposedName = name;
            request.ProposedDescription = description;
            request.ProposedCategory = command.Category;
            request.ProposedPhotoUrls = photoUrls;

            await db.SaveChangesAsync(cancellationToken);

            await SpotChangeRequestPhotos.DeleteUnusedAsync(
                photoStorage, replacedPhotoUrls, spot.PhotoUrls.Concat(photoUrls), spot.Id, logger, cancellationToken);

            logger.LogInformation(SpotsLogMessages.SpotEditRequested, spot.Id, userContext.UserId);

            return Result<SpotChangeRequestResponse>.Success(SpotChangeRequestResponse.From(request));
        }
    }
}
