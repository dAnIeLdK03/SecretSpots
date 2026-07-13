using SecretSpots.Features.Common.Storage;

namespace SecretSpots.Features.Tests.TestSupport;

internal class FakePhotoStorage : IPhotoStorage
{
    public int UploadCallCount { get; private set; }

    public Task<string> UploadAsync(Stream content, string contentType, string key, CancellationToken cancellationToken)
    {
        UploadCallCount++;
        return Task.FromResult($"https://fake-storage.test/{key}");
    }
}
