namespace SecretSpots.Features.Common.Storage;

public interface IPhotoStorage
{
    Task<string> UploadAsync(Stream content, string contentType, string key, CancellationToken cancellationToken);

    // Takes the full public URL previously returned by UploadAsync, not a bare key — callers
    // (e.g. DeleteSpot) only ever have the URL they stored, and only the storage implementation
    // knows how to map that back to its own key format.
    Task DeleteAsync(string url, CancellationToken cancellationToken);
}
