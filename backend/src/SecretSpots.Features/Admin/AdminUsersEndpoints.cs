using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Admin;

public static class AdminUsersEndpoints
{
    public static IEndpointRouteBuilder MapAdminUsersEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/admin/users").WithTags("Admin").RequireAuthorization("Admin");

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
