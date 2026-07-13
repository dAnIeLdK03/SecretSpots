using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Photos;

public static class PhotosEndpoints
{
    public static IEndpointRouteBuilder MapPhotosEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/photos", async (IFormFile file, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new UploadPhoto.Command(file), cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("Photos")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Produces<UploadPhotoResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<IFormFile>("multipart/form-data");

        return app;
    }
}
