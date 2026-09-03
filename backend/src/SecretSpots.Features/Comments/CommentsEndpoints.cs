using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Comments;

public static class CommentsEndpoints
{
    public static IEndpointRouteBuilder MapCommentsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/spots/{spotId:guid}/comments", async (
                Guid spotId, CreateComment.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new CreateComment.Command(spotId, body.Text);
                var result = await sender.Send(command, cancellationToken);
                return result.IsSuccess ? Results.Created($"/comments/{result.Value.Id}", result.Value) : result.ToProblem();
            })
            .WithTags("Comments")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.ContentWrites)
            .Produces<CommentResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<CreateComment.RequestBody>("application/json");

        app.MapGet("/spots/{spotId:guid}/comments", async (
                Guid spotId, int? page, int? pageSize, IOptions<CommentOptions> commentOptions,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var query = new GetSpotComments.Query(spotId, page ?? 1, pageSize ?? commentOptions.Value.DefaultPageSize);
                var result = await sender.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .WithTags("Comments")
            .RequireAuthorization()
            .Produces<CommentsPageResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapPut("/comments/{id:guid}", async (
                Guid id, UpdateComment.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new UpdateComment.Command(id, body.Text);
                var result = await sender.Send(command, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("Comments")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.ContentWrites)
            .Produces<CommentResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<UpdateComment.RequestBody>("application/json");

        app.MapDelete("/comments/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new DeleteComment.Command(id), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .WithTags("Comments")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.ContentWrites)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
