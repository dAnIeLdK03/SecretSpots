using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Common.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace SecretSpots.Features.Photos;

public static class UploadPhoto
{
    public record Command(IFormFile File) : IRequest<Result<UploadPhotoResponse>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(IStringLocalizer<SharedResources> localizer, IOptions<PhotoOptions> photoOptions)
        {
            // Cascade(Stop) is required here — unlike built-in validators (MaximumLength etc.),
            // which no-op on null, these are custom Must() predicates that would NullReferenceException
            // on a null File if they still ran after NotNull() already failed.
            RuleFor(c => c.File)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage(localizer[PhotoMessageKeys.FileRequired].Value)
                .Must(f => f.Length > 0).WithMessage(localizer[PhotoMessageKeys.FileRequired].Value)
                .Must(f => f.Length <= photoOptions.Value.MaxFileSizeBytes)
                    .WithMessage(localizer[PhotoMessageKeys.FileTooLarge].Value);
        }
    }

    public class Handler(
        IPhotoStorage photoStorage,
        IOptions<PhotoOptions> photoOptions,
        IUserContext userContext,
        IStringLocalizer<SharedResources> localizer,
        ILogger<Handler> logger)
        : IRequestHandler<Command, Result<UploadPhotoResponse>>
    {
        public async Task<Result<UploadPhotoResponse>> Handle(Command command, CancellationToken cancellationToken)
        {
            Image image;
            try
            {
                await using var uploadStream = command.File.OpenReadStream();
                image = await Image.LoadAsync(uploadStream, cancellationToken);
            }
            catch (ImageFormatException)
            {
                // Covers both "not an image at all" (UnknownImageFormatException) and "corrupt/
                // truncated image data" (InvalidImageContentException) — the authoritative check
                // is a real decode attempt, not trusting the client-supplied Content-Type/extension.
                return Result<UploadPhotoResponse>.Failure(new Error(
                    PhotoMessageKeys.InvalidImage,
                    localizer[PhotoMessageKeys.InvalidImage].Value,
                    StatusCodes.Status400BadRequest));
            }

            using (image)
            {
                var maxDimension = photoOptions.Value.MaxDimensionPixels;
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    // Only downscales if larger than maxDimension — never upscales a smaller image.
                    Mode = ResizeMode.Max,
                    Size = new Size(maxDimension, maxDimension),
                }));

                var encoder = new WebpEncoder { Quality = photoOptions.Value.WebpQuality };

                using var outputStream = new MemoryStream();
                await image.SaveAsync(outputStream, encoder, cancellationToken);
                outputStream.Position = 0;

                var key = $"{Guid.NewGuid()}.webp";
                var url = await photoStorage.UploadAsync(outputStream, "image/webp", key, cancellationToken);

                logger.LogInformation(PhotoLogMessages.PhotoUploaded, key, userContext.UserId);

                return Result<UploadPhotoResponse>.Success(new UploadPhotoResponse(url));
            }
        }
    }
}
