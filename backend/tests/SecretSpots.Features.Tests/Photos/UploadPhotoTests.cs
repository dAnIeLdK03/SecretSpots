using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SecretSpots.Features.Photos;
using SecretSpots.Features.Tests.TestSupport;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SecretSpots.Features.Tests.Photos;

public class UploadPhotoValidatorTests
{
    private readonly UploadPhoto.Validator _validator = new(TestLocalizerFactory.Create(), TestOptionsFactory.Photo());

    private static IFormFile MakeFile(long length)
    {
        var stream = new MemoryStream(new byte[Math.Min(length, 16)]);
        return new FormFile(stream, 0, length, "file", "photo.png");
    }

    [Fact]
    public void Missing_file_is_invalid()
    {
        var result = _validator.TestValidate(new UploadPhoto.Command(null!));
        result.ShouldHaveValidationErrorFor(c => c.File);
    }

    [Fact]
    public void Empty_file_is_invalid()
    {
        var result = _validator.TestValidate(new UploadPhoto.Command(MakeFile(0)));
        result.ShouldHaveValidationErrorFor(c => c.File);
    }

    [Fact]
    public void File_larger_than_the_configured_limit_is_invalid()
    {
        var validator = new UploadPhoto.Validator(TestLocalizerFactory.Create(), TestOptionsFactory.Photo(maxFileSizeBytes: 1024));

        var result = validator.TestValidate(new UploadPhoto.Command(MakeFile(1025)));
        result.ShouldHaveValidationErrorFor(c => c.File);
    }

    [Fact]
    public void File_within_the_limit_has_no_errors()
    {
        var result = _validator.TestValidate(new UploadPhoto.Command(MakeFile(1024)));
        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UploadPhotoHandlerTests
{
    private static IFormFile MakeImageFile()
    {
        using var image = new Image<Rgba32>(4, 4);
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "file", "photo.png");
    }

    private static IFormFile MakeNonImageFile()
    {
        var bytes = "this is definitely not an image"u8.ToArray();
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "not-a-photo.png");
    }

    private static UploadPhoto.Handler CreateHandler(FakePhotoStorage photoStorage, Guid userId) =>
        new(
            photoStorage,
            TestOptionsFactory.Photo(),
            new FakeUserContext(userId),
            TestLocalizerFactory.Create(),
            NullLogger<UploadPhoto.Handler>.Instance);

    [Fact]
    public async Task Valid_image_is_uploaded_and_returns_a_url()
    {
        var photoStorage = new FakePhotoStorage();
        var handler = CreateHandler(photoStorage, Guid.NewGuid());

        var result = await handler.Handle(new UploadPhoto.Command(MakeImageFile()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.StartsWith("https://fake-storage.test/", result.Value.Url);
        Assert.Equal(1, photoStorage.UploadCallCount);
    }

    [Fact]
    public async Task Non_image_content_is_rejected_and_nothing_is_uploaded()
    {
        var photoStorage = new FakePhotoStorage();
        var handler = CreateHandler(photoStorage, Guid.NewGuid());

        var result = await handler.Handle(new UploadPhoto.Command(MakeNonImageFile()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(PhotoMessageKeys.InvalidImage, result.Error.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Error.StatusCode);
        Assert.Equal(0, photoStorage.UploadCallCount);
    }
}
