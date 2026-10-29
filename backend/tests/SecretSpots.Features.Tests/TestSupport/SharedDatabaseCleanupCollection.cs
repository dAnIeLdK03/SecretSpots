namespace SecretSpots.Features.Tests.TestSupport;

// The cleanup services delete/soft-delete rows across the whole table, not just rows a test made —
// run alone so they can't remove tokens or spots another test class is still working with.
[CollectionDefinition(Name, DisableParallelization = true)]
public class SharedDatabaseCleanupCollection
{
    public const string Name = "Whole-table cleanup";
}
