namespace SecretSpots.Features.Common.Configuration;

public class PhotoOptions
{
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public int MaxDimensionPixels { get; set; } = 1920;
    public int WebpQuality { get; set; } = 80;

    // Guards against "pixel bomb" uploads — a small file (e.g. a crafted PNG) that decodes to
    // an enormous pixel grid and spikes memory. Checked via Image.Identify (reads header only)
    // before the full Image.LoadAsync decode. 40MP comfortably covers real phone camera photos.
    public long MaxDecodedPixels { get; set; } = 40_000_000;
}
