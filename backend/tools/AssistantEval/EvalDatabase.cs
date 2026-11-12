using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using SecretSpots.Domain;
using SecretSpots.Features.Common.Persistence;

namespace AssistantEval;

// Puts the evaluation spots in a database and takes them out again. The database is assumed to be
// migrated already (the API or the test run does that); this only adds and removes its own rows,
// recognisable by the "[eval] " name prefix and by the throwaway user id.
public static class EvalDatabase
{
    public static AppDbContext Connect(string connectionString) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite())
            .Options);

    // Removes every evaluation spot (also leftovers of an interrupted run) and this run's usage rows.
    public static async Task CleanAsync(AppDbContext db, Guid userId)
    {
        await db.Spots.IgnoreQueryFilters().Where(s => s.Name.StartsWith(EvalSpots.NamePrefix)).ExecuteDeleteAsync();
        await db.AssistantUsages.Where(u => u.UserId == userId).ExecuteDeleteAsync();
    }

    // Returns each spot's id by its key.
    public static async Task<Dictionary<string, Guid>> SeedAsync(AppDbContext db, Guid userId)
    {
        var ids = new Dictionary<string, Guid>();
        foreach (var spot in EvalSpots.All)
        {
            var entity = new Spot
            {
                Id = Guid.NewGuid(),
                Name = spot.Name,
                Description = spot.Description,
                Category = spot.Category,
                Location = new Point(spot.Longitude, spot.Latitude) { SRID = 4326 },
                PhotoUrls = ["https://example.com/photo.jpg"],
                CreatedByUserId = userId,
                AverageRating = spot.AverageRating,
                RatingsCount = spot.RatingsCount,
                ModerationStatus = spot.Status,
            };
            db.Spots.Add(entity);
            ids[spot.Key] = entity.Id;
        }

        await db.SaveChangesAsync();
        return ids;
    }
}
