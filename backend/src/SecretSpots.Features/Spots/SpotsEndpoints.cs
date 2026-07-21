using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

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

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetSpot.Query(id), cancellationToken);
                return result.ToOkOrProblem();
            })
            .Produces<SpotResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/nearby", async (
                double lat, double lng, double radiusKm, ISender sender, CancellationToken cancellationToken) =>
            {
                var results = await sender.Send(new SearchNearbySpots.Query(lat, lng, radiusKm), cancellationToken);
                return Results.Ok(results);
            })
            .Produces<IReadOnlyList<NearbySpotResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPut("/{id:guid}", async (
                Guid id, UpdateSpot.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new UpdateSpot.Command(id, body.Name, body.Description, body.Category, body.PhotoUrl);
                var result = await sender.Send(command, cancellationToken);
                return result.ToOkOrProblem();
            })
            .RequireAuthorization()
            .Produces<SpotResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<UpdateSpot.RequestBody>("application/json");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new DeleteSpot.Command(id), cancellationToken);
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
