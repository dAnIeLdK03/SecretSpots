using Microsoft.AspNetCore.Http;

namespace SecretSpots.Features.Common.Security;

public static class RefreshTokenCookie
{
    public const string Name = "secretspots_refresh_token";

    // Not HttpOnly and carries no secret — just a same-domain marker so the frontend can tell
    // whether a session exists without calling /auth/refresh and getting a guaranteed 401 for
    // every anonymous visit. Cookies ignore port, so this is readable from the Next.js dev server
    // on a different port too, as long as both share the same host.
    public const string SessionMarkerName = "secretspots_has_session";

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
        response.Cookies.Append(SessionMarkerName, "1", new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
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
        response.Cookies.Append(SessionMarkerName, string.Empty, new CookieOptions
        {
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/",
            Expires = DateTimeOffset.UnixEpoch,
        });
    }
}
