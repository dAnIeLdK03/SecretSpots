using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.SavedSpots;

public static class SavedSpotsEndpoints
{
    public static IEndpointRouteBuilder MapSavedSpotsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPut("/spots/{spotId:guid}/saved", async (Guid spotId, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new SaveSpot.Command(spotId), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .WithTags("SavedSpots")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapDelete("/spots/{spotId:guid}/saved", async (Guid spotId, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new UnsaveSpot.Command(spotId), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .WithTags("SavedSpots")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/spots/{spotId:guid}/saved/me", async (Guid spotId, ISender sender, CancellationToken cancellationToken) =>
            {
                var saved = await sender.Send(new GetIsSpotSaved.Query(spotId), cancellationToken);
                return Results.Ok(new IsSpotSavedResponse(saved));
            })
            .WithTags("SavedSpots")
            .RequireAuthorization()
            .Produces<IsSpotSavedResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapGet("/saved-spots/me", async (
                int? page, int? pageSize, IOptions<SavedSpotsOptions> savedSpotsOptions,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var query = new GetMySavedSpots.Query(page ?? 1, pageSize ?? savedSpotsOptions.Value.DefaultPageSize);
                var result = await sender.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .WithTags("SavedSpots")
            .RequireAuthorization()
            .Produces<SavedSpotsPageResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
