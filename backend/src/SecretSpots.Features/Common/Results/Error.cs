using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using SecretSpots.Features.Common.Localization;

namespace SecretSpots.Features.Common.Results;

public record Error(string Code, string Message, int StatusCode)
{
    public static Error NotFound(string code, IStringLocalizer<SharedResources> localizer) =>
        new(code, localizer[code].Value, StatusCodes.Status404NotFound);

    public static Error Forbidden(string code, IStringLocalizer<SharedResources> localizer) =>
        new(code, localizer[code].Value, StatusCodes.Status403Forbidden);

    public static Error BadRequest(string code, IStringLocalizer<SharedResources> localizer) =>
        new(code, localizer[code].Value, StatusCodes.Status400BadRequest);

    public static Error Conflict(string code, IStringLocalizer<SharedResources> localizer) =>
        new(code, localizer[code].Value, StatusCodes.Status409Conflict);

    public static Error Unauthorized(string code, IStringLocalizer<SharedResources> localizer) =>
        new(code, localizer[code].Value, StatusCodes.Status401Unauthorized);
}
