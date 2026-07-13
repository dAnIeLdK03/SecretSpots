namespace SecretSpots.Features.Common.Configuration;

public class PhotoOptions
{
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxDimensionPixels { get; set; } = 1920;
    public int WebpQuality { get; set; } = 80;
}
