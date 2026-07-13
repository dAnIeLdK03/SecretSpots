using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        return app;
    }
}
