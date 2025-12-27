using Xunit;

namespace Server.Tests;

/// <summary>
/// Fixture that loads TileData from client files for tests that require it.
/// Requires client files at C:\Ultima Online Classic or configured via MODERNUO_CLIENT_PATH environment variable.
/// </summary>
[CollectionDefinition("TileData Server Tests", DisableParallelization = true)]
public class TileDataServerFixture : ICollectionFixture<TileDataServerFixture>
{
    /// <summary>
    /// True if TileData was successfully loaded from client files.
    /// </summary>
    public bool TileDataLoaded => TestServerInitializer.TileDataLoaded;

    /// <summary>
    /// A tile ID known to have the Surface flag. 0 if not found.
    /// </summary>
    public ushort SurfaceTileId => TestServerInitializer.SurfaceTileId;

    /// <summary>
    /// A tile ID known to have the Impassable flag. 0 if not found.
    /// </summary>
    public ushort ImpassableTileId => TestServerInitializer.ImpassableTileId;

    /// <summary>
    /// A tile ID known to have the Wet flag (water). 0 if not found.
    /// </summary>
    public ushort WetTileId => TestServerInitializer.WetTileId;

    public TileDataServerFixture()
    {
        TestServerInitializer.Initialize(loadTileData: true);
    }
}
