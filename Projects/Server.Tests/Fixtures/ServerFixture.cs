using Xunit;

namespace Server.Tests;

/// <summary>
/// Fixture for all server tests. Attempts to load TileData from client files if available.
/// Configure client path via MODERNUO_CLIENT_PATH environment variable or place files at C:\Ultima Online Classic.
/// </summary>
[CollectionDefinition("Sequential Server Tests", DisableParallelization = true)]
public class ServerFixture : ICollectionFixture<ServerFixture>
{
    /// <summary>
    /// True if TileData was successfully loaded from client files.
    /// </summary>
    public static bool TileDataLoaded => TestServerInitializer.TileDataLoaded;

    /// <summary>
    /// A tile ID known to have the Surface flag. 0 if not found.
    /// </summary>
    public static ushort SurfaceTileId => TestServerInitializer.SurfaceTileId;

    /// <summary>
    /// A tile ID known to have the Impassable flag. 0 if not found.
    /// </summary>
    public static ushort ImpassableTileId => TestServerInitializer.ImpassableTileId;

    /// <summary>
    /// A tile ID known to have the Wet flag (water). 0 if not found.
    /// </summary>
    public static ushort WetTileId => TestServerInitializer.WetTileId;

    /// <summary>
    /// A tile ID known to have both Surface and Impassable flags (tables, furniture). 0 if not found.
    /// </summary>
    public static ushort SurfaceImpassableTileId => TestServerInitializer.SurfaceImpassableTileId;

    // Global init runs exactly once via the shared, guarded TestServerInitializer. The single
    // bootstrap owns global state for the lifetime of the test host, so there is no
    // per-collection teardown — matching UOContent.Tests' TestServerInitializer pattern.
    public ServerFixture() => TestServerInitializer.Initialize(loadTileData: true);
}
