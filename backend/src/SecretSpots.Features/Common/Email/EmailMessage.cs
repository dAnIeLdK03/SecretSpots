namespace SecretSpots.Features.Common.Email;

public record EmailMessage(string ToEmail, string Subject, string HtmlBody);
