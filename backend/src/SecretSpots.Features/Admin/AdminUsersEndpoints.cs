using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Admin;

public static class AdminUsersEndpoints
{
    public static IEndpointRouteBuilder MapAdminUsersEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/admin/users").WithTags("Admin").RequireAuthorization("Admin");

        group.MapGet("/", async (
                string? q, int? page, int? pageSize, IOptions<AdminUsersOptions> adminUsersOptions,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var (resolvedPage, resolvedPageSize) = adminUsersOptions.Value.ResolvePaging(page, pageSize);
                var query = new GetAdminUsers.Query(q, resolvedPage, resolvedPageSize);
                var result = await sender.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .Produces<PagedResponse<AdminUserResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}/role", async (
                Guid id, SetUserRole.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new SetUserRole.Command(id, body.Role);
                var result = await sender.Send(command, cancellationToken);
                return result.ToOkOrProblem();
            })
            .Produces<AdminUserResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
