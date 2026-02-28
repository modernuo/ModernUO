using System.IO;
using System.Reflection;
using System.Threading;
using Server.Items;
using Server.Tests.Maps;

namespace Server.Tests;

/// <summary>
/// Shared server initialization logic for test fixtures.
/// Ensures the server is only initialized once across all test collections.
/// </summary>
public static class TestServerInitializer
{
    private const string DefaultDataDirectory = @"C:\Ultima Online Classic";
    private static bool _initialized;
    private static readonly Lock _lock = new();

    /// <summary>
    /// True if TileData was successfully loaded from client files.
    /// </summary>
    public static bool TileDataLoaded { get; private set; }

    /// <summary>
    /// A tile ID known to have the Surface flag. 0 if not found.
    /// </summary>
    public static ushort SurfaceTileId { get; private set; }

    /// <summary>
    /// A tile ID known to have the Impassable flag. 0 if not found.
    /// </summary>
    public static ushort ImpassableTileId { get; private set; }

    /// <summary>
    /// A tile ID known to have the Wet flag (water). 0 if not found.
    /// </summary>
    public static ushort WetTileId { get; private set; }

    /// <summary>
    /// A tile ID known to have both Surface and Impassable flags (tables, furniture). 0 if not found.
    /// </summary>
    public static ushort SurfaceImpassableTileId { get; private set; }

    /// <summary>
    /// Initializes the test server. Safe to call multiple times - only initializes once.
    /// </summary>
    /// <param name="loadTileData">If true, attempts to load TileData from client files.</param>
    public static void Initialize(bool loadTileData = false)
    {
        lock (_lock)
        {
            if (_initialized)
            {
                // Already initialized, but check if we need to load TileData now
                if (loadTileData && !TileDataLoaded && TileData.MaxItemValue == 0)
                {
                    TryLoadTileData();
                    DetectTileIds();
                }
                return;
            }

            Core.ApplicationAssembly = Assembly.GetExecutingAssembly();

            // Load Configurations
            ServerConfiguration.Load(true);

            // Try to load TileData if requested
            if (loadTileData)
            {
                TryLoadTileData();
            }

            // Load an empty assembly list into the resolver
            ServerConfiguration.AssemblyDirectories.Add(Core.BaseDirectory);
            AssemblyHandler.LoadAssemblies(["Server.dll"]);

            Core.LoopContext = new EventLoopContext();
            Core.Expansion = Expansion.EJ;

            // Configure networking (initializes RingSocketManager for tests)
            Server.Network.NetState.Configure();

            // Configure / Initialize
            TestMapDefinitions.ConfigureTestMapDefinitions();

            // Configure the world
            World.Configure();

            Timer.Init(0);

            // Load the world
            World.Load();

            World.ExitSerializationThreads();

            // Detect tile IDs if TileData was loaded
            if (loadTileData)
            {
                DetectTileIds();
            }

            DecayScheduler.Configure();

            _initialized = true;
        }
    }

    private static void TryLoadTileData()
    {
        var dataDir = GetDataDirectory();
        if (string.IsNullOrEmpty(dataDir) || !Directory.Exists(dataDir))
        {
            return;
        }

        ServerConfiguration.DataDirectories.Add(dataDir);

        var tileDataPath = Core.FindDataFile("tiledata.mul", false);
        if (File.Exists(tileDataPath))
        {
            TileData.Load();
        }
    }

    private static void DetectTileIds()
    {
        if (TileData.MaxItemValue == 0)
        {
            return;
        }

        SurfaceTileId = FindTileWithFlag(TileFlag.Surface);
        ImpassableTileId = FindTileWithFlag(TileFlag.Impassable);
        WetTileId = FindTileWithFlag(TileFlag.Wet);
        SurfaceImpassableTileId = FindTileWithFlags(TileFlag.Surface | TileFlag.Impassable);
        TileDataLoaded = SurfaceTileId > 0 && ImpassableTileId > 0 && WetTileId > 0;
    }

    private static ushort FindTileWithFlag(TileFlag flag)
    {
        for (ushort i = 1; i <= TileData.MaxItemValue && i < 0xFFFF; i++)
        {
            var data = TileData.ItemTable[i];
            if ((data.Flags & flag) != 0)
            {
                return i;
            }
        }
        return 0;
    }

    private static ushort FindTileWithFlags(TileFlag flags)
    {
        for (ushort i = 1; i <= TileData.MaxItemValue && i < 0xFFFF; i++)
        {
            var data = TileData.ItemTable[i];
            if ((data.Flags & flags) == flags)
            {
                return i;
            }
        }
        return 0;
    }

    private static string GetDataDirectory()
    {
        // Check environment variable first
        var envDir = System.Environment.GetEnvironmentVariable("MODERNUO_CLIENT_PATH");
        if (!string.IsNullOrEmpty(envDir) && Directory.Exists(envDir))
        {
            return envDir;
        }

        // Fall back to default directory
        if (Directory.Exists(DefaultDataDirectory))
        {
            return DefaultDataDirectory;
        }

        return null;
    }
}
