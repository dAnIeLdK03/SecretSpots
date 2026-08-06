using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Ratings;

public static class RatingsEndpoints
{
    public static IEndpointRouteBuilder MapRatingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/spots/{spotId:guid}/ratings", async (
                Guid spotId, RateSpot.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new RateSpot.Command(spotId, body.Value);
                var result = await sender.Send(command, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("Ratings")
            .RequireAuthorization()
            .Produces<RatingResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<RateSpot.RequestBody>("application/json");

        app.MapGet("/spots/{spotId:guid}/ratings/me", async (
                Guid spotId, ISender sender, CancellationToken cancellationToken) =>
            {
                var value = await sender.Send(new GetMyRating.Query(spotId), cancellationToken);
                return Results.Ok(new MyRatingResponse(value));
            })
            .WithTags("Ratings")
            .RequireAuthorization()
            .Produces<MyRatingResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
