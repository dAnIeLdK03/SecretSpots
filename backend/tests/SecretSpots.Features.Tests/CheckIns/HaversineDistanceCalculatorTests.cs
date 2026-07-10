using SecretSpots.Features.CheckIns;

namespace SecretSpots.Features.Tests.CheckIns;

public class HaversineDistanceCalculatorTests
{
    [Fact]
    public void Identical_points_are_zero_meters_apart()
    {
        var distance = HaversineDistanceCalculator.CalculateMeters(42.6977, 23.3219, 42.6977, 23.3219);
        Assert.Equal(0, distance, precision: 6);
    }

    [Fact]
    public void One_degree_of_latitude_is_about_111_km()
    {
        // Along a meridian (same longitude), Haversine reduces to the exact great-circle
        // arc length: EarthRadius * angle-in-radians — a precise, ground-truth-free check.
        var distance = HaversineDistanceCalculator.CalculateMeters(0, 0, 1, 0);
        Assert.Equal(111194.93, distance, precision: 2);
    }

    [Fact]
    public void Near_antipodal_points_do_not_produce_NaN()
    {
        // Regression test for the Math.Clamp fix: floating-point rounding can push the
        // intermediate `a` term slightly above 1 for near-antipodal coordinates, which
        // would make Math.Sqrt(1 - a) return NaN without the clamp.
        var sofia = (Latitude: 42.6977, Longitude: 23.3219);
        var antipode = (Latitude: -sofia.Latitude, Longitude: sofia.Longitude - 180);

        var distance = HaversineDistanceCalculator.CalculateMeters(
            sofia.Latitude, sofia.Longitude, antipode.Latitude, antipode.Longitude);

        Assert.False(double.IsNaN(distance));

        // Exact antipodes are always half the Earth's circumference apart, regardless of direction.
        var halfCircumference = Math.PI * 6371000;
        Assert.Equal(halfCircumference, distance, precision: 0);
    }
}
