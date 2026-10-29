using SecretSpots.Features.Common.Validation;

namespace SecretSpots.Features.Tests.Common.Validation;

// IsOwnPhotoUrl is the only thing stopping a spot/check-in from pointing at a third-party URL or at
// another user's photo (see the comment on it) — each case here is one of those escape routes.
public class UrlValidationTests
{
    private const string BaseUrl = "https://photos.example.com/secretspots-photos";
    private const string Owner = "11111111-1111-1111-1111-111111111111";
    private const string OtherUser = "22222222-2222-2222-2222-222222222222";
    private static readonly Guid OwnerId = Guid.Parse(Owner);

    [Theory]
    [InlineData(BaseUrl + "/" + Owner + "/photo.webp")]
    // Trailing slash on the configured base URL makes no difference.
    [InlineData(BaseUrl + "/" + Owner + "/photo.webp", BaseUrl + "/")]
    // Dot-segments that resolve back inside the owner's folder are what the browser fetches too.
    [InlineData(BaseUrl + "/" + OtherUser + "/../" + Owner + "/photo.webp")]
    public void Accepts_the_owners_own_upload(string url, string baseUrl = BaseUrl)
    {
        Assert.True(UrlValidation.IsOwnPhotoUrl(url, baseUrl, OwnerId));
    }

    [Theory]
    // Someone else's photo — copying it would let SpotPhotoCleanup delete their object.
    [InlineData(BaseUrl + "/" + OtherUser + "/photo.webp")]
    [InlineData(BaseUrl + "/" + OtherUser + "/" + Owner + "/photo.webp")]
    [InlineData(BaseUrl + "/" + Owner + "-x/photo.webp")]
    [InlineData(BaseUrl + "/" + Owner)]
    // Dot-segments that escape the bucket prefix (a raw string prefix check would accept these).
    [InlineData(BaseUrl + "/../evil-bucket/" + Owner + "/photo.webp")]
    [InlineData(BaseUrl + "/" + Owner + "/../../evil-bucket/" + Owner + "/photo.webp")]
    // Percent-encoded and backslash variants that browsers also normalize.
    [InlineData(BaseUrl + "/%2e%2e/evil-bucket/" + Owner + "/photo.webp")]
    [InlineData(BaseUrl + "/..%2Fevil-bucket/" + Owner + "/photo.webp")]
    [InlineData(BaseUrl + "\\..\\evil-bucket/" + Owner + "/photo.webp")]
    // A bucket whose name merely starts with ours.
    [InlineData("https://photos.example.com/secretspots-photos-evil/" + Owner + "/photo.webp")]
    // Other host / look-alike host / userinfo trick.
    [InlineData("https://evil.example.com/secretspots-photos/" + Owner + "/photo.webp")]
    [InlineData("https://photos.example.com.evil.com/secretspots-photos/" + Owner + "/photo.webp")]
    [InlineData("https://photos.example.com@evil.com/secretspots-photos/" + Owner + "/photo.webp")]
    // Other scheme or port.
    [InlineData("http://photos.example.com/secretspots-photos/" + Owner + "/photo.webp")]
    [InlineData("https://photos.example.com:8443/secretspots-photos/" + Owner + "/photo.webp")]
    [InlineData("ftp://photos.example.com/secretspots-photos/" + Owner + "/photo.webp")]
    // Not an absolute URL at all.
    [InlineData("/secretspots-photos/" + Owner + "/photo.webp")]
    [InlineData(Owner + "/photo.webp")]
    [InlineData("javascript:alert(1)")]
    [InlineData("")]
    public void Rejects_anything_that_is_not_the_owners_own_upload(string url)
    {
        Assert.False(UrlValidation.IsOwnPhotoUrl(url, BaseUrl, OwnerId));
    }

    [Fact]
    public void Rejects_everything_when_the_configured_base_url_is_invalid()
    {
        Assert.False(UrlValidation.IsOwnPhotoUrl(BaseUrl + "/" + Owner + "/photo.webp", "not a url", OwnerId));
    }
}
