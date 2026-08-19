namespace SecretSpots.Features.Common.Email;

// Registered instead of ResendEmailSender in Development (see Program.cs) — records into
// ITestEmailInbox rather than calling the real Resend API, which needs a key that's configured
// nowhere in Development (neither local dev machines nor the CI e2e job).
public class InMemoryEmailSender(ITestEmailInbox inbox) : IEmailSender
{
    public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        inbox.Record(new SentEmail(toEmail, subject, htmlBody, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}
