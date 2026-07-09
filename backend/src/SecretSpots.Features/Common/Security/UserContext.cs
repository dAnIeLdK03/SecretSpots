using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

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
}
