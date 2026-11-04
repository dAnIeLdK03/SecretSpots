namespace SecretSpots.Features.Tests.TestSupport;

// Tests that touch or measure whole tables rather than just their own rows: the cleanup services
// delete/soft-delete across the table, and the admin spot counts read it. Run alone so they can't
// remove rows another test class is using, or see counts move under them.
[CollectionDefinition(Name, DisableParallelization = true)]
public class SharedDatabaseCleanupCollection
{
    public const string Name = "Whole-table cleanup";
}
