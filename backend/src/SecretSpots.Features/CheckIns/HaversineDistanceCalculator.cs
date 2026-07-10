namespace SecretSpots.Features.CheckIns;

// Plain C# distance calculation for a single point-to-point anti-fraud check — deliberately not
// PostGIS/geography here (unlike SearchNearbySpots): this only needs one comparison, not a
// search/sort, and avoids the geometry/geography ST_Y/ST_X pitfalls hit in the Spots slice.
internal static class HaversineDistanceCalculator
{
    private const double EarthRadiusMeters = 6371000;

    public static double CalculateMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
            + (Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2));

        // a is mathematically bounded to [0, 1], but floating-point rounding near antipodal
        // points can push it slightly outside that range, making 1 - a negative and
        // Math.Sqrt return NaN — which would make the distance check below silently pass.
        a = Math.Clamp(a, 0, 1);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
