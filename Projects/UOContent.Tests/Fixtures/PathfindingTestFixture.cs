using Xunit;

namespace Server.Tests.Pathfinding;

[CollectionDefinition("Sequential Pathfinding Tests", DisableParallelization = true)]
public class PathfindingTestFixture : ICollectionFixture<PathfindingTestFixture>
{
    // Shares the single process-wide bootstrap (TestServerBootstrap, in the parent
    // Server.Tests namespace). The superset bootstrap already loads the UO client tile data
    // and configures movement/pathfinding, so this collection needs no extra setup.
    public PathfindingTestFixture() => TestServerBootstrap.EnsureInitialized();
}
