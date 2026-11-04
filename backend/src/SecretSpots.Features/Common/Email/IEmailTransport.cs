namespace SecretSpots.Features.Common.Email;

// Actually delivers one email (Resend in production, the in-memory inbox in Development). Only
// EmailDispatchService calls it — handlers go through IEmailSender, which just queues.
public interface IEmailTransport
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
