namespace SecretSpots.Features.Common.Storage;

public interface IPhotoStorage
{
    Task<string> UploadAsync(Stream content, string contentType, string key, CancellationToken cancellationToken);
}
