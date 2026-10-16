namespace SecretSpots.Features.Common.Security;

// Reads "who is calling" from the current request's authenticated principal —
// handlers ask this instead of taking a UserId as part of their own request data.
public interface IUserContext
{
    Guid UserId { get; }

    // Non-throwing variant of UserId for endpoints reachable anonymously (e.g. GetSpot), where
    // "no authenticated user" is a normal case, not an error.
    Guid? UserIdOrNull { get; }
}
