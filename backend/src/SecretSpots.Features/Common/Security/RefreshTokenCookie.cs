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

    // Must mirror the attributes Append used — browsers only overwrite a cookie whose Secure and
    // SameSite flags match, so a bare Delete can leave the original refresh cookie in place and
    // the "remember me" session comes back on the next page load.
    public static void Delete(HttpResponse response)
    {
        response.Cookies.Append(Name, string.Empty, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/auth",
            Expires = DateTimeOffset.UnixEpoch,
        });
    }
}
