using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.CheckIns;

public static class CheckInsEndpoints
{
    public static IEndpointRouteBuilder MapCheckInsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/spots/{spotId:guid}/checkins", async (
                Guid spotId, CreateCheckIn.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new CreateCheckIn.Command(spotId, body.PhotoUrl, body.Latitude, body.Longitude);
                var result = await sender.Send(command, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("CheckIns")
            .RequireAuthorization()
            .Produces<CheckInResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<CreateCheckIn.RequestBody>("application/json");

        return app;
    }
}
