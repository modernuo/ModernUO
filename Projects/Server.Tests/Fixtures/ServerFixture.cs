using System;
using Xunit;

namespace Server.Tests;

/// <summary>
/// Fixture for all server tests. Attempts to load TileData from client files if available.
/// Configure client path via MODERNUO_CLIENT_PATH environment variable or place files at C:\Ultima Online Classic.
/// </summary>
[CollectionDefinition("Sequential Server Tests", DisableParallelization = true)]
public class ServerFixture : ICollectionFixture<ServerFixture>, IDisposable
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

    public ServerFixture()
    {
        TestServerInitializer.Initialize(loadTileData: true);
    }

    public void Dispose()
    {
        Timer.Init(0);
    }
}
