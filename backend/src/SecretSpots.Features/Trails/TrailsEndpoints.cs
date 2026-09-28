using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Trails;

public static class TrailsEndpoints
{
    public static IEndpointRouteBuilder MapTrailsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/trails").WithTags("Trails");

        group.MapPost("/", async (CreateTrail.Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var response = await sender.Send(command, cancellationToken);
                return Results.Created($"/trails/{response.Id}", response);
            })
            .RequireAuthorization()
            .Produces<TrailResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<CreateTrail.Command>("application/json");

        group.MapGet("/nearby", async (
                double lat, double lng, double radiusKm, ISender sender, CancellationToken cancellationToken) =>
            {
                var results = await sender.Send(new SearchNearbyTrails.Query(lat, lng, radiusKm), cancellationToken);
                return Results.Ok(results);
            })
            .Produces<NearbyTrailsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new DeleteTrail.Command(id), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
