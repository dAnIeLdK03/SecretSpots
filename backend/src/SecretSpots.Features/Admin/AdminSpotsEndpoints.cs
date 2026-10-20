using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;
using SecretSpots.Features.Spots;

namespace SecretSpots.Features.Admin;

public static class AdminSpotsEndpoints
{
    public static IEndpointRouteBuilder MapAdminSpotsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/admin/spots").WithTags("Admin").RequireAuthorization(AuthorizationPolicies.Admin);

        group.MapGet("/", async (
                SpotModerationStatus? status, int? page, int? pageSize, IOptions<AdminSpotsOptions> adminSpotsOptions,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var (resolvedPage, resolvedPageSize) = adminSpotsOptions.Value.ResolvePaging(page, pageSize);
                var query = new GetAdminSpots.Query(status, resolvedPage, resolvedPageSize);
                var result = await sender.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .Produces<PagedResponse<SpotSearchResultResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ApproveSpot.Command(id), cancellationToken);
                return result.ToOkOrProblem();
            })
            .Produces<SpotResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/reject", async (
                Guid id, RejectSpot.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new RejectSpot.Command(id, body.Reason);
                var result = await sender.Send(command, cancellationToken);
                return result.ToOkOrProblem();
            })
            .Produces<SpotResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
