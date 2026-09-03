namespace SecretSpots.Features.Common.Security;

// Custom (non-JWT-registered) claim names, shared between JwtService (which issues them) and
// Program.cs (which builds authorization policies off them).
public static class ClaimNames
{
    public const string IsAdmin = "is_admin";
}
