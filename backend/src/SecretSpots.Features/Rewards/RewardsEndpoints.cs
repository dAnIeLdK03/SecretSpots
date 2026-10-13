using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
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
                Guid businessId, int? page, int? pageSize, IOptions<RewardsOptions> rewardsOptions,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var (resolvedPage, resolvedPageSize) = rewardsOptions.Value.ResolvePaging(page, pageSize);
                var query = new GetBusinessRewards.Query(businessId, resolvedPage, resolvedPageSize);
                var result = await sender.Send(query, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("Rewards")
            .Produces<PagedResponse<RewardResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
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

        app.MapPost("/rewards/{id:guid}/active", async (
                Guid id, SetRewardActive.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new SetRewardActive.Command(id, body.IsActive), cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("Rewards")
            .RequireAuthorization()
            .Produces<RewardResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<SetRewardActive.RequestBody>("application/json");

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

        app.MapGet("/redemptions/me", async (
                int? page, int? pageSize, IOptions<RewardsOptions> rewardsOptions,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var (resolvedPage, resolvedPageSize) = rewardsOptions.Value.ResolvePaging(page, pageSize);
                var query = new GetMyRedemptions.Query(resolvedPage, resolvedPageSize);
                var result = await sender.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .WithTags("Rewards")
            .RequireAuthorization()
            .Produces<PagedResponse<MyRedemptionResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }
}
