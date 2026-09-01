using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/register", async (Register.Command command, ISender sender, HttpContext http, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                if (result.IsSuccess)
                {
                    RefreshTokenCookie.Append(
                        http.Response,
                        result.Value.RefreshToken,
                        result.Value.RememberMe ? result.Value.RefreshTokenExpiresAt : null);
                }
                return result.ToOkOrProblem();
            })
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<AuthResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<Register.Command>("application/json");

        group.MapPost("/login", async (Login.Command command, ISender sender, HttpContext http, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                if (result.IsSuccess)
                {
                    RefreshTokenCookie.Append(
                        http.Response,
                        result.Value.RefreshToken,
                        result.Value.RememberMe ? result.Value.RefreshTokenExpiresAt : null);
                }
                return result.ToOkOrProblem();
            })
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<AuthResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<Login.Command>("application/json");

        group.MapPost("/refresh", async (ISender sender, HttpContext http, CancellationToken cancellationToken) =>
            {
                var command = new RefreshAccessToken.Command(http.Request.Cookies[RefreshTokenCookie.Name] ?? "");
                var result = await sender.Send(command, cancellationToken);
                if (result.IsSuccess)
                {
                    RefreshTokenCookie.Append(
                        http.Response,
                        result.Value.RefreshToken,
                        result.Value.RememberMe ? result.Value.RefreshTokenExpiresAt : null);
                }
                return result.ToOkOrProblem();
            })
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .RequireCsrfHeader()
            .Produces<AuthResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/logout", async (ISender sender, HttpContext http, CancellationToken cancellationToken) =>
            {
                var command = new Logout.Command(http.Request.Cookies[RefreshTokenCookie.Name] ?? "");
                var result = await sender.Send(command, cancellationToken);
                RefreshTokenCookie.Delete(http.Response);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireCsrfHeader()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/google", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new BeginExternalAuth.Command(ExternalAuthProvider.Google), cancellationToken);
            return result.IsSuccess ? Results.Redirect(result.Value) : result.ToProblem();
        });

        group.MapGet("/facebook", async (ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new BeginExternalAuth.Command(ExternalAuthProvider.Facebook), cancellationToken);
            return result.IsSuccess ? Results.Redirect(result.Value) : result.ToProblem();
        });

        group.MapGet("/google/callback", async (string? code, string? state, string? error, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ExternalAuthCallback.Command(ExternalAuthProvider.Google, code, state, error), cancellationToken);
            return result.IsSuccess ? Results.Redirect(result.Value) : result.ToProblem();
        });

        group.MapGet("/facebook/callback", async (string? code, string? state, string? error, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ExternalAuthCallback.Command(ExternalAuthProvider.Facebook, code, state, error), cancellationToken);
            return result.IsSuccess ? Results.Redirect(result.Value) : result.ToProblem();
        });

        group.MapPost("/external/exchange", async (ExchangeExternalAuthCode.Command command, ISender sender, HttpContext http, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                if (result.IsSuccess)
                {
                    RefreshTokenCookie.Append(
                        http.Response,
                        result.Value.RefreshToken,
                        result.Value.RememberMe ? result.Value.RefreshTokenExpiresAt : null);
                }
                return result.ToOkOrProblem();
            })
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces<AuthResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .Accepts<ExchangeExternalAuthCode.Command>("application/json");

        group.MapPost("/password-reset/request", async (RequestPasswordReset.Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Accepts<RequestPasswordReset.Command>("application/json");

        group.MapPost("/password-reset/confirm", async (ResetPassword.Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Accepts<ResetPassword.Command>("application/json");

        group.MapPost("/email-verification/resend", async (ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new ResendEmailVerification.Command(), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/email-verification/confirm", async (VerifyEmail.Command command, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Accepts<VerifyEmail.Command>("application/json");

        group.MapGet("/me", async (ISender sender, CancellationToken cancellationToken) =>
                (await sender.Send(new GetCurrentUser.Query(), cancellationToken)).ToOkOrProblem())
            .RequireAuthorization()
            .Produces<GetCurrentUser.Response>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapDelete("/me", async ([FromBody] DeleteAccount.Command command, ISender sender, HttpContext http, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(command, cancellationToken);
                if (result.IsSuccess)
                {
                    RefreshTokenCookie.Delete(http.Response);
                }
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<DeleteAccount.Command>("application/json");

        return app;
    }
}
