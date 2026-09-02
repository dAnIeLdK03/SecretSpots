using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Notifications;

public static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/notifications").WithTags("Notifications");

        group.MapGet("/", async (
                int? page, int? pageSize, IOptions<NotificationsOptions> notificationsOptions,
                ISender sender, CancellationToken cancellationToken) =>
            {
                var query = new GetNotifications.Query(
                    page ?? 1, pageSize ?? notificationsOptions.Value.DefaultPageSize);
                var result = await sender.Send(query, cancellationToken);
                return Results.Ok(result);
            })
            .RequireAuthorization()
            .Produces<NotificationsPageResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/{id:guid}/read", async (Guid id, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new MarkNotificationAsRead.Command(id), cancellationToken);
                return result.ToOkOrProblem();
            })
            .RequireAuthorization()
            .Produces<NotificationResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/read-all", async (ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new MarkAllNotificationsAsRead.Command(), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapPost("/push-subscriptions", async (
                SubscribeToPush.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var command = new SubscribeToPush.Command(body.Endpoint, body.P256dh, body.Auth);
                var result = await sender.Send(command, cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .Accepts<SubscribeToPush.RequestBody>("application/json");

        group.MapDelete("/push-subscriptions", async (
                [FromBody] UnsubscribeFromPush.RequestBody body, ISender sender, CancellationToken cancellationToken) =>
            {
                var result = await sender.Send(new UnsubscribeFromPush.Command(body.Endpoint), cancellationToken);
                return result.IsSuccess ? Results.NoContent() : result.ToProblem();
            })
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .Accepts<UnsubscribeFromPush.RequestBody>("application/json");

        group.MapGet("/push-public-key", (IOptions<WebPushOptions> webPushOptions) =>
                Results.Ok(new { publicKey = webPushOptions.Value.VapidPublicKey }))
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK);

        return app;
    }
}
