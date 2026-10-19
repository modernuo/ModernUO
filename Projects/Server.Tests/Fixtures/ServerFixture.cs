using Server.Tests.Terrain;
using Xunit;

namespace Server.Tests;

/// <summary>
/// Fixture for all server tests. The host runs on generated tile data and terrain (see <see cref="SyntheticWorld"/>).
/// </summary>
[CollectionDefinition("Sequential Server Tests", DisableParallelization = true)]
public class ServerFixture : ICollectionFixture<ServerFixture>
{
    /// <summary>A tile with the Surface flag.</summary>
    public static ushort SurfaceTileId => TestTiles.Floor;

    /// <summary>A tile with the Impassable flag.</summary>
    public static ushort ImpassableTileId => TestTiles.Rock;

    /// <summary>A tile with the Wet flag (water).</summary>
    public static ushort WetTileId => TestTiles.Water;

    /// <summary>A tile with both Surface and Impassable flags (tables, furniture).</summary>
    public static ushort SurfaceImpassableTileId => TestTiles.Counter;

    // Global init runs exactly once via the shared, guarded TestServerInitializer. The single
    // bootstrap owns global state for the lifetime of the test host, so there is no
    // per-collection teardown — matching UOContent.Tests' TestServerInitializer pattern.
    public ServerFixture() => TestServerInitializer.Initialize();
}
