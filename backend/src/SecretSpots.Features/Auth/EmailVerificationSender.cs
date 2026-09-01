using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Email;
using SecretSpots.Features.Common.Localization;
using SecretSpots.Features.Common.Persistence;
using SecretSpots.Features.Common.Security;

namespace SecretSpots.Features.Auth;

// Shared by Register and ResendEmailVerification — both end with "issue a fresh verification
// token for this user and email them the link", the same relationship AuthTokenIssuer has to
// Register/Login/RefreshAccessToken.
internal static class EmailVerificationSender
{
    public static async Task SendAsync(
        IAppDbContext db,
        IEmailSender emailSender,
        IOptions<EmailVerificationOptions> emailVerificationOptions,
        IStringLocalizer<SharedResources> localizer,
        User user,
        CancellationToken cancellationToken)
    {
        // Only the hash is persisted — the raw value is a bearer secret usable on its own, so a
        // database leak must not hand out working verification links.
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var token = new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = OpaqueTokenHasher.Hash(rawToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(emailVerificationOptions.Value.TokenExpiryMinutes),
        };

        db.EmailVerificationTokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);

        var verifyLink = QueryHelpers.AddQueryString(
            emailVerificationOptions.Value.FrontendVerifyUrl, "token", rawToken);

        await emailSender.SendAsync(
            user.Email,
            localizer[AuthMessageKeys.EmailVerificationEmailSubject].Value,
            string.Format(localizer[AuthMessageKeys.EmailVerificationEmailBody].Value, verifyLink),
            cancellationToken);
    }
}
