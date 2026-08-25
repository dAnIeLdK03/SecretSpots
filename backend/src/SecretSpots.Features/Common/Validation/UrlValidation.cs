namespace SecretSpots.Features.Common.Validation;

public static class UrlValidation
{
    public static bool IsHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    // Photo fields (Spot.PhotoUrls, CheckIn.PhotoUrl) must only ever point at something this app
    // itself produced via UploadPhoto — that's the only place EXIF/GPS gets stripped, size/
    // dimension gets capped, and the content gets a real decode-attempt validation. Accepting any
    // http(s) URL would let a user point at an arbitrary third-party URL (e.g. a tracking pixel
    // they control) that every other viewer of that spot/check-in then silently requests.
    public static bool IsOwnPhotoUrl(string value, string publicBaseUrl)
    {
        return IsHttpUrl(value) && value.StartsWith($"{publicBaseUrl.TrimEnd('/')}/", StringComparison.Ordinal);
    }
}
