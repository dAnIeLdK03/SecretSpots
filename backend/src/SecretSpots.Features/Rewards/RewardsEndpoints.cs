using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Rewards;

public static class RewardsEndpoints
{
    public static IEndpointRouteBuilder MapRewardsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/businesses/{businessId:guid}/rewards", async (
                Guid businessId, CreateReward.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new CreateReward.Command(businessId, body.Title, body.Description, body.CrystalCost);
                var result = await sender.Send(command, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("Rewards")
            .RequireAuthorization()
            .Produces<RewardResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<CreateReward.RequestBody>("application/json");

        app.MapGet("/businesses/{businessId:guid}/rewards", async (
                Guid businessId, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new GetBusinessRewards.Query(businessId), cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("Rewards")
            .Produces<IReadOnlyList<RewardResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapPut("/rewards/{id:guid}", async (
                Guid id, UpdateReward.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new UpdateReward.Command(id, body.Title, body.Description, body.CrystalCost);
                var result = await sender.Send(command, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("Rewards")
            .RequireAuthorization()
            .Produces<RewardResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<UpdateReward.RequestBody>("application/json");

        app.MapDelete("/rewards/{id:guid}", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new DeleteReward.Command(id), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .WithTags("Rewards")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        app.MapPost("/rewards/{id:guid}/redeem", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new RedeemReward.Command(id), cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("Rewards")
            .RequireAuthorization()
            .Produces<RewardRedemptionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
