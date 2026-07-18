using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;

namespace SecretSpots.Features.Common.Storage;

public class R2PhotoStorage : IPhotoStorage, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly R2Options _options;

    public R2PhotoStorage(IOptions<R2Options> options)
    {
        _options = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.ServiceUrlOverride ?? $"https://{_options.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
        };

        _client = new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey), config);
    }

    public async Task<string> UploadAsync(Stream content, string contentType, string key, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
        };

        await _client.PutObjectAsync(request, cancellationToken);

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
