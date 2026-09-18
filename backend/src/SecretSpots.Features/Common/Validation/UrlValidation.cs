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
    //
    // Comparing against the *normalized* URI (scheme/host/port + AbsolutePath) rather than the
    // raw string is deliberate: System.Uri collapses "/.." dot-segments per RFC 3986, and so does
    // every browser resolving the URL to actually fetch it. A raw-string prefix check accepts
    // "{publicBaseUrl}/../evil-bucket/x" (it does start with the prefix as text) while the browser
    // — and Uri.AbsolutePath here — resolve it to "/evil-bucket/x", escaping the intended prefix
    // entirely. Comparing the normalized form is what actually matches what gets fetched.
    //
    // ownerUserId requires the key's first path segment (UploadPhoto writes every object under
    // "{userId}/...") to match the caller — without this, any authenticated user could copy
    // someone else's public photo URL (a fully legitimate own-bucket URL) into their own spot,
    // then edit/delete that spot to have UpdateSpot/SpotDeletionCleanup delete the *original*
    // owner's R2 object out from under their still-live spot/check-in.
    public static bool IsOwnPhotoUrl(string value, string publicBaseUrl, Guid ownerUserId)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !IsHttpUrl(value))
        {
            return false;
        }

        if (!Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri))
        {
            return false;
        }

        var basePathPrefix = baseUri.AbsolutePath.TrimEnd('/') + "/";

        if (uri.Scheme != baseUri.Scheme
            || uri.Host != baseUri.Host
            || uri.Port != baseUri.Port
            || !uri.AbsolutePath.StartsWith(basePathPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var key = uri.AbsolutePath[basePathPrefix.Length..];
        var ownerKeyPrefix = ownerUserId.ToString() + "/";
        return key.StartsWith(ownerKeyPrefix, StringComparison.Ordinal);
    }
}
