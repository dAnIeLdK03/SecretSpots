using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Persistence;
using WebPush;
using WebPushSubscription = WebPush.PushSubscription;

namespace SecretSpots.Features.Notifications;

// Shared by every place that creates a Notification (CreateSpot, CreateComment, RateSpot,
// CreateCheckIn) — mirrors the in-app notification with a browser push. Best-effort: a push
// provider hiccup must never fail the caller's actual operation (spot created, comment posted,
// etc.), so every failure here is caught and swallowed, not rethrown.
internal static class PushNotificationSender
{
    private const string AppName = "SecretSpots";

    // Push text renders at send-time, not at read-time like GetNotifications does — there's no
    // "recipient's browser is making this request" moment to read their locale from, and no
    // persisted per-user locale preference to fall back to. Rendering in the app's default
    // culture is an honest MVP tradeoff; properly localizing this would need a stored user
    // locale setting.
    private static readonly CultureInfo PushCulture = new("bg");

    public static async Task SendAsync(
        IAppDbContext db,
        WebPushClient webPushClient,
        IOptions<WebPushOptions> webPushOptions,
        IStringLocalizer<SharedResources> localizer,
        ILogger logger,
        Notification notification,
        CancellationToken cancellationToken)
    {
        var subscriptions = await db.PushSubscriptions
            .Where(p => p.UserId == notification.UserId)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            return;
        }

        // IStringLocalizer resolves text from the ambient CultureInfo.CurrentUICulture — there's
        // no per-call "render in this culture instead" API anymore (WithCulture was removed after
        // .NET Core 3.0), so the thread's culture is swapped for the duration of this one call.
        var previousUiCulture = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;
        string body;
        try
        {
            CultureInfo.CurrentUICulture = PushCulture;
            CultureInfo.CurrentCulture = PushCulture;
            body = NotificationResponseFactory.Create(notification, localizer).Message;
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUiCulture;
            CultureInfo.CurrentCulture = previousCulture;
        }

        var payload = JsonSerializer.Serialize(new
        {
            title = AppName,
            body,
            relatedSpotId = notification.RelatedSpotId,
        });

        var vapidDetails = new VapidDetails(
            webPushOptions.Value.VapidSubject, webPushOptions.Value.VapidPublicKey, webPushOptions.Value.VapidPrivateKey);

        foreach (var subscription in subscriptions)
        {
            var pushSubscription = new WebPushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth);
            try
            {
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails, cancellationToken);
            }
            catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                // The push service says this subscription no longer exists (browser uninstalled,
                // site data cleared, endpoint rotated) — stop trying to push to it.
                db.PushSubscriptions.Remove(subscription);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, NotificationsLogMessages.PushSendFailed, subscription.Id, notification.UserId);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
