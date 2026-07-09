namespace SecretSpots.Features.Auth;

public record AuthResult(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
