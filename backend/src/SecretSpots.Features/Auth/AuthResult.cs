using System.Text.Json.Serialization;

namespace SecretSpots.Features.Auth;

public record AuthResult(
    string AccessToken,
    [property: JsonIgnore] string RefreshToken,
    DateTimeOffset ExpiresAt,
    [property: JsonIgnore] DateTimeOffset RefreshTokenExpiresAt,
    [property: JsonIgnore] bool RememberMe);
