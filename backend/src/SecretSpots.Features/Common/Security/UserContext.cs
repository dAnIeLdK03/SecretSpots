using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using SecretSpots.Domain;

namespace SecretSpots.Features.Common.Security;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid UserId
    {
        get
        {
            var subClaim = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)
                ?? throw new InvalidOperationException(SecurityMessages.NoAuthenticatedUser);

            return Guid.Parse(subClaim.Value);
        }
    }

    public Guid? UserIdOrNull
    {
        get
        {
            var subClaim = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub);
            return subClaim is null ? null : Guid.Parse(subClaim.Value);
        }
    }

    public UserRole Role
    {
        get
        {
            var roleClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimNames.Role)
                ?? throw new InvalidOperationException(SecurityMessages.NoAuthenticatedUser);

            return Enum.Parse<UserRole>(roleClaim.Value);
        }
    }
}
