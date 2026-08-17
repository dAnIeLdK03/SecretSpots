using Microsoft.AspNetCore.Http;

namespace SecretSpots.Features.Common.Security;

public static class RefreshTokenCookie
{
    public const string Name = "secretspots_refresh_token";

    public static void Append(HttpResponse response, string token, DateTimeOffset? expires)
    {
        response.Cookies.Append(Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/auth",
            Expires = expires,
        });
    }

    public static void Delete(HttpResponse response)
    {
        response.Cookies.Delete(Name, new CookieOptions
        {
            Path = "/auth",
        });
    }
}
