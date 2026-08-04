namespace SecretSpots.Features.Common.ExternalAuth;

public record  ExternalAuthUserInfo(string ProviderUserId, string Email, bool EmailVerified, string DisplayName);