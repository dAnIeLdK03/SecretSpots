using SecretSpots.Domain;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace SecretSpots.Features.SampleData;

// Draws a simple stylized landscape (sky gradient, two ridgelines, ground) tinted by category, so
// sample spots have real photos in storage without shipping image files or needing the internet.
// Deterministic per seed, so every reseed produces the same pictures.
internal static class SamplePhotoGenerator
{
    private record Palette(Rgb24 SkyTop, Rgb24 SkyBottom, Rgb24 FarRidge, Rgb24 NearRidge, Rgb24 Ground);

    private static readonly Palette Default = new(
        new Rgb24(120, 170, 220), new Rgb24(230, 225, 200), new Rgb24(110, 130, 150), new Rgb24(70, 95, 80), new Rgb24(60, 80, 50));

    private static readonly Dictionary<SpotCategory, Palette> Palettes = new()
    {
        [SpotCategory.BeachOrLake] = new(new Rgb24(90, 160, 230), new Rgb24(210, 235, 245), new Rgb24(120, 150, 170), new Rgb24(80, 120, 140), new Rgb24(40, 110, 160)),
        [SpotCategory.RockFormation] = new(new Rgb24(240, 170, 110), new Rgb24(250, 225, 180), new Rgb24(170, 110, 80), new Rgb24(140, 80, 55), new Rgb24(110, 85, 60)),
        [SpotCategory.Waterfall] = new(new Rgb24(150, 200, 210), new Rgb24(225, 240, 235), new Rgb24(90, 130, 110), new Rgb24(50, 100, 70), new Rgb24(70, 140, 160)),
        [SpotCategory.Cave] = new(new Rgb24(40, 40, 60), new Rgb24(110, 100, 110), new Rgb24(70, 65, 75), new Rgb24(45, 40, 50), new Rgb24(30, 28, 35)),
        [SpotCategory.Abandoned] = new(new Rgb24(140, 140, 150), new Rgb24(210, 200, 190), new Rgb24(110, 110, 115), new Rgb24(80, 80, 85), new Rgb24(90, 85, 70)),
        [SpotCategory.FortressRuins] = new(new Rgb24(230, 150, 90), new Rgb24(250, 210, 160), new Rgb24(150, 120, 100), new Rgb24(110, 90, 70), new Rgb24(100, 90, 60)),
        [SpotCategory.Viewpoint] = new(new Rgb24(60, 110, 200), new Rgb24(250, 190, 140), new Rgb24(90, 100, 140), new Rgb24(50, 60, 90), new Rgb24(40, 55, 60)),
        [SpotCategory.MonasteryOrChurch] = new(new Rgb24(170, 190, 230), new Rgb24(245, 230, 210), new Rgb24(120, 140, 130), new Rgb24(80, 110, 90), new Rgb24(90, 110, 70)),
        [SpotCategory.Nature] = new(new Rgb24(110, 180, 230), new Rgb24(220, 240, 210), new Rgb24(90, 140, 100), new Rgb24(50, 110, 60), new Rgb24(60, 120, 50)),
        [SpotCategory.Spring] = new(new Rgb24(150, 190, 220), new Rgb24(235, 235, 220), new Rgb24(110, 140, 120), new Rgb24(70, 110, 85), new Rgb24(90, 150, 140)),
        [SpotCategory.CampingSpot] = new(new Rgb24(50, 70, 130), new Rgb24(240, 160, 100), new Rgb24(60, 80, 90), new Rgb24(35, 60, 50), new Rgb24(45, 75, 40)),
        [SpotCategory.Cafe] = new(new Rgb24(200, 160, 120), new Rgb24(245, 225, 200), new Rgb24(150, 110, 80), new Rgb24(110, 80, 60), new Rgb24(90, 65, 45)),
        [SpotCategory.RailwayTunnel] = new(new Rgb24(130, 170, 200), new Rgb24(225, 230, 215), new Rgb24(100, 120, 110), new Rgb24(60, 85, 70), new Rgb24(85, 80, 70)),
    };

    public static async Task<MemoryStream> GenerateAsync(SpotCategory category, int seed, int width, int height, CancellationToken cancellationToken)
    {
        var palette = Palettes.GetValueOrDefault(category, Default);
        var random = new Random(seed);

        // Each ridge is a sum of two sine waves with seed-dependent frequency/phase.
        double farPhase = random.NextDouble() * Math.PI * 2, nearPhase = random.NextDouble() * Math.PI * 2;
        double farFrequency = 1.5 + random.NextDouble() * 2, nearFrequency = 2 + random.NextDouble() * 3;
        var groundLine = (int)(height * (0.78 + random.NextDouble() * 0.08));

        using var image = new Image<Rgb24>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                var sky = Lerp(palette.SkyTop, palette.SkyBottom, (double)y / groundLine);

                for (var x = 0; x < row.Length; x++)
                {
                    var t = (double)x / width;
                    var farRidge = height * (0.42 + 0.10 * Math.Sin(t * Math.PI * farFrequency + farPhase) + 0.04 * Math.Sin(t * Math.PI * farFrequency * 3.1 + farPhase));
                    var nearRidge = height * (0.58 + 0.09 * Math.Sin(t * Math.PI * nearFrequency + nearPhase) + 0.03 * Math.Sin(t * Math.PI * nearFrequency * 2.7 + nearPhase));

                    row[x] = y >= groundLine ? palette.Ground
                        : y >= nearRidge ? palette.NearRidge
                        : y >= farRidge ? palette.FarRidge
                        : sky;
                }
            }
        });

        var stream = new MemoryStream();
        await image.SaveAsync(stream, new WebpEncoder { Quality = 80 }, cancellationToken);
        stream.Position = 0;
        return stream;
    }

    private static Rgb24 Lerp(Rgb24 from, Rgb24 to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return new Rgb24(
            (byte)(from.R + (to.R - from.R) * amount),
            (byte)(from.G + (to.G - from.G) * amount),
            (byte)(from.B + (to.B - from.B) * amount));
    }
}
