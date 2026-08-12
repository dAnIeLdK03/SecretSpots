namespace SecretSpots.Domain;

// Append-only — stored as int in the DB (see the InitialCreate migration), so existing members
// must keep their position/value. New categories always go at the end.
public enum SpotCategory
{
    Nature,
    Viewpoint,
    Cafe,
    Abandoned,
    Waterfall,
    Cave,
    Landmark,
    BeachOrLake,
    Spring,
    MonasteryOrChurch,
    CampingSpot,
    RockFormation,
    RailwayTunnel,
    FortressRuins,
}
