using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace SecretSpots.Features.Common.Email;

// What handlers get as IEmailSender: "sending" only queues the email and returns, so a request
// (registration, password reset, role change) never waits on — or fails because of — the email
// provider. EmailDispatchService delivers from the queue in the background.
//
// In memory on purpose: an email queued right before the process crashes is lost. Every email
// here can be asked for again (resend verification, request another reset link), so that trade
// is cheaper than a durable outbox table. A normal shutdown (deploy) still drains the queue.
public class EmailQueue(ILogger<EmailQueue> logger) : IEmailSender
{
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateUnbounded<EmailMessage>(new UnboundedChannelOptions { SingleReader = true });

    public ChannelReader<EmailMessage> Reader => _channel.Reader;

    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        // An unbounded channel only refuses writes after Complete(), i.e. while shutting down. Not
        // an error for the request that got here a moment too late — its email can be asked for again.
        if (!_channel.Writer.TryWrite(new EmailMessage(toEmail, subject, htmlBody)))
        {
            logger.LogWarning(EmailLogMessages.QueueClosed, toEmail);
        }

        return Task.CompletedTask;
    }

    // No more emails will be queued; the dispatcher finishes what's left and stops.
    public void Complete() => _channel.Writer.TryComplete();
}
