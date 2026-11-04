namespace SecretSpots.Features.Common.Email;

// Registered instead of ResendEmailTransport in Development (see Program.cs) — records into
// ITestEmailInbox rather than calling the real Resend API, which needs a key that's configured
// nowhere in Development (neither local dev machines nor the CI e2e job).
public class InMemoryEmailTransport(ITestEmailInbox inbox) : IEmailTransport
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        inbox.Record(new SentEmail(message.ToEmail, message.Subject, message.HtmlBody, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}
