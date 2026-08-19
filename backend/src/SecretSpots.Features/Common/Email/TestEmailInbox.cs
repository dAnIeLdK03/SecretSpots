using System.Collections.Concurrent;

namespace SecretSpots.Features.Common.Email;

public record SentEmail(string ToEmail, string Subject, string HtmlBody, DateTimeOffset SentAt);

public interface ITestEmailInbox
{
    void Record(SentEmail email);
    SentEmail? GetLatest(string toEmail);
}

// Development-only stand-in for the real email provider — see InMemoryEmailSender. Lets e2e
// tests read back what would have been sent (e.g. a password-reset link) without ever calling
// Resend, which needs a real API key neither the local dev environment nor CI has configured.
public class TestEmailInbox : ITestEmailInbox
{
    private readonly ConcurrentBag<SentEmail> _emails = [];

    public void Record(SentEmail email) => _emails.Add(email);

    public SentEmail? GetLatest(string toEmail) =>
        _emails
            .Where(e => e.ToEmail.Equals(toEmail, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.SentAt)
            .FirstOrDefault();
}
