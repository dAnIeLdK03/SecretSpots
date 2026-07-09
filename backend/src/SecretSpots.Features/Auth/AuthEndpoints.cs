using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", async (Register.Command command, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(command, cancellationToken)).ToOkOrProblem())
            .Produces<AuthResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<Register.Command>("application/json");

        group.MapPost("/login", async (Login.Command command, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(command, cancellationToken)).ToOkOrProblem())
            .Produces<AuthResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<Login.Command>("application/json");

        group.MapPost("/refresh", async (RefreshAccessToken.Command command, ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(command, cancellationToken)).ToOkOrProblem())
            .Produces<AuthResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<RefreshAccessToken.Command>("application/json");

        group.MapGet("/me", async (ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new GetCurrentUser.Query(), cancellationToken)).ToOkOrProblem())
            .RequireAuthorization()
            .Produces<GetCurrentUser.Response>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
