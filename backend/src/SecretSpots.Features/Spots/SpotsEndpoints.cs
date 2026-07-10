using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Features.Common.Mediator;

namespace SecretSpots.Features.Spots;

public static class SpotsEndpoints
{
    public static IEndpointRouteBuilder MapSpotsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/spots").WithTags("Spots");

        group.MapPost("/", async (CreateSpot.Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var response = await sender.Send(command, cancellationToken);
                return Results.Created($"/spots/{response.Id}", response);
            })
            .RequireAuthorization()
            .Produces<SpotResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<CreateSpot.Command>("application/json");

        group.MapGet("/nearby", async (
                double lat, double lng, double radiusKm, ISender sender, CancellationToken cancellationToken) =>
            {
                var results = await sender.Send(new SearchNearbySpots.Query(lat, lng, radiusKm), cancellationToken);
                return Results.Ok(results);
            })
            .Produces<IReadOnlyList<NearbySpotResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
