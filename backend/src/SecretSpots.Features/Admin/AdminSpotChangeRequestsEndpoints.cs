using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Admin;

public static class AdminSpotChangeRequestsEndpoints
{
    public static IEndpointRouteBuilder MapAdminSpotChangeRequestsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/admin/spot-change-requests").WithTags("Admin").RequireAuthorization(AuthorizationPolicies.Admin);

        group.MapGet("/", async (
                int? page, int? pageSize, IOptions<AdminSpotsOptions> adminSpotsOptions,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var (resolvedPage, resolvedPageSize) = adminSpotsOptions.Value.ResolvePaging(page, pageSize);
                var result = await sender.Send(new GetAdminSpotChangeRequests.Query(resolvedPage, resolvedPageSize), cancellationToken);
                return Results.Ok(result);
            })
            .Produces<PagedResponse<AdminSpotChangeRequestResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ApproveSpotChangeRequest.Command(id), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/reject", async (
                Guid id, RejectSpotChangeRequest.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new RejectSpotChangeRequest.Command(id, body.Reason), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
