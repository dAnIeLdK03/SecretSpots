using SecretSpots.Features.Common.Storage;

namespace SecretSpots.Features.Tests.TestSupport;

internal class FakePhotoStorage : IPhotoStorage
{
    public int UploadCallCount { get; private set; }
    public List<string> DeletedUrls { get; } = [];

    public Task<string> UploadAsync(Stream content, string contentType, string key, CancellationToken cancellationToken)
    {
        UploadCallCount++;
        return Task.FromResult($"https://fake-storage.test/{key}");
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken)
    {
        DeletedUrls.Add(url);
        return Task.CompletedTask;
    }
}
