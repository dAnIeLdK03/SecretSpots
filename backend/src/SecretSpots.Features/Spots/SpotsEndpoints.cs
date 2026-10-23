using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
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
                var result = await sender.Send(command, cancellationToken);
                return result.IsSuccess ? Results.Created($"/spots/{result.Value.Id}", result.Value) : result.ToProblem();
            })
            .RequireAuthorization()
            .Produces<SpotResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
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
                double lat, double lng, double radiusKm, SpotCategory? category,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var results = await sender.Send(new SearchNearbySpots.Query(lat, lng, radiusKm, category), cancellationToken);
                return Results.Ok(results);
            })
            .Produces<NearbySpotsResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/search", async (
            string? q, SpotCategory? category, int? page, int? pageSize, IOptions<SpotSearchOptions> spotSearchOptions,
            ISender sender, CancellationToken cancellationToken
        ) =>
        {
            var (resolvedPage, resolvedPageSize) = spotSearchOptions.Value.ResolvePaging(page, pageSize);
            var query = new SearchSpots.Query(q, category, resolvedPage, resolvedPageSize);
            var result = await sender.Send(query, cancellationToken);
            return Results.Ok(result);
        })
            .Produces<PagedResponse<SpotSearchResultResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/mine", async (
                int? page, int? pageSize, IOptions<SpotSearchOptions> spotSearchOptions,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var (resolvedPage, resolvedPageSize) = spotSearchOptions.Value.ResolvePaging(page, pageSize);
                var query = new GetMySpots.Query(resolvedPage, resolvedPageSize);
                var result = await sender.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .Produces<PagedResponse<SpotSearchResultResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        // PUT/DELETE don't change the spot — they submit a change request an admin has to
        // approve (see SubmitSpotEdit/SubmitSpotDeletion).
        group.MapPut("/{id:guid}", async (
                Guid id, SubmitSpotEdit.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new SubmitSpotEdit.Command(id, body.Name, body.Description, body.Category, body.PhotoUrls);
                var result = await sender.Send(command, cancellationToken);
                return result.ToOkOrProblem();
            })
            .RequireAuthorization()
            .Produces<SpotChangeRequestResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<SubmitSpotEdit.RequestBody>("application/json");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new SubmitSpotDeletion.Command(id), cancellationToken);
                return result.ToOkOrProblem();
            })
            .RequireAuthorization()
            .Produces<SpotChangeRequestResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/{id:guid}/change-request", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetMySpotChangeRequest.Query(id), cancellationToken);
                return result.ToOkOrProblem();
            })
            .RequireAuthorization()
            .Produces<SpotChangeRequestResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/change-request", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new CancelSpotChangeRequest.Command(id), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
