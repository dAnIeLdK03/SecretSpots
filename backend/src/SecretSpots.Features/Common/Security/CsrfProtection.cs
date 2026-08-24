using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SecretSpots.Features.Common.Security;

// /auth/refresh and /auth/logout authenticate via an ambient httpOnly cookie (SameSite=None,
// required since frontend/backend are different sites in production) rather than an explicit
// Authorization header, so unlike every Bearer-token endpoint they're reachable by a plain
// cross-site POST — the browser attaches the cookie automatically regardless of which site
// triggered the request. SameSite can't defend against that here since it's already forced to
// None. Requiring a header that isn't on the CORS-safelisted list (Accept, Accept-Language,
// Content-Language, Content-Type) forces the browser to preflight first, and the strict CORS
// origin allowlist then rejects that preflight for any origin but our own frontend.
public static class CsrfProtection
{
    public const string HeaderName = "X-Requested-With";
    public const string HeaderValue = "XMLHttpRequest";

    public static RouteHandlerBuilder RequireCsrfHeader(this RouteHandlerBuilder builder) =>
        builder.AddEndpointFilter(async (context, next) =>
        {
            if (context.HttpContext.Request.Headers[HeaderName] != HeaderValue)
            {
                return Microsoft.AspNetCore.Http.Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            return await next(context);
        });
}
