using NetTopologySuite.Geometries;

namespace SecretSpots.Domain;

public class Business : IHasCreatedAt
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Point Location { get; set; }
    public Guid OwnerUserId { get; set; }
    public bool IsPromoted { get; set; }

    // Lets an owner temporarily pause a business (e.g. closed for the season) without deleting
    // it — a hard delete would cascade-remove its rewards and, via that, orphan nothing but would
    // still be a one-way door the owner can't undo. Only gates discovery (SearchNearbyBusinesses);
    // a direct link to the business still resolves, same as a paused reward still being visible
    // on its own business page.
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
