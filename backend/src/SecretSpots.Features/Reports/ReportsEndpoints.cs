using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Reports;

public static class ReportsEndpoints
{
    public static IEndpointRouteBuilder MapReportsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/spots/{spotId:guid}/reports", async (
                Guid spotId, ReportSpot.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new ReportSpot.Command(spotId, body.Reason, body.Details);
                var result = await sender.Send(command, cancellationToken);
                return result.IsSuccess ? Results.Created($"/reports/{result.Value.Id}", result.Value) : result.ToProblem();
            })
            .WithTags("Reports")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.ContentWrites)
            .Produces<ReportResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<ReportSpot.RequestBody>("application/json");

        app.MapPost("/comments/{commentId:guid}/reports", async (
                Guid commentId, ReportComment.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new ReportComment.Command(commentId, body.Reason, body.Details);
                var result = await sender.Send(command, cancellationToken);
                return result.IsSuccess ? Results.Created($"/reports/{result.Value.Id}", result.Value) : result.ToProblem();
            })
            .WithTags("Reports")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitPolicies.ContentWrites)
            .Produces<ReportResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Accepts<ReportComment.RequestBody>("application/json");

        return app;
    }
}
