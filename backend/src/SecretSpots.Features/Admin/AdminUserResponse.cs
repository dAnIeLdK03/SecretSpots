using SecretSpots.Domain;

namespace SecretSpots.Features.Admin;

public record AdminUserResponse(Guid Id, string Email, string DisplayName, UserRole Role);
