namespace SecretSpots.Features.Common.Security;

// Authorization policy names, shared between Program.cs (which defines them) and every
// *Endpoints.cs file that gates a route group behind one — avoids repeating the bare string
// (and the typo risk that comes with it) at every call site.
public static class AuthorizationPolicies
{
    public const string Admin = "Admin";
}
