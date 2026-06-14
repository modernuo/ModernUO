using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Server.Items;
using Server.Misc;
using Server.Movement;
using Server.PathAlgorithms;
using Server.Tests.Maps;

namespace Server.Tests;

/// <summary>
/// Single, process-wide ModernUO bootstrap for the UOContent test host. Mirrors Server.Tests'
/// TestServerInitializer in name and shape; kept as a separate (non-shared) copy because this
/// one loads the UOContent assembly and configures the UOContent-specific systems. Both types
/// are <c>internal</c> so the shared name stays scoped to each assembly.
///
/// ModernUO bootstraps its global singletons (Core, ServerConfiguration, AssemblyHandler,
/// NetState/io-ring, World, Timer, the serialization workers, and TileData) exactly once per
/// process. <see cref="World.Load"/> is guarded to run once, and
/// <see cref="World.ExitSerializationThreads"/> must run once against the live workers. Each
/// xUnit collection gets its own fixture instance, so this guard makes the bootstrap run a
/// single time regardless of how many collection fixtures are constructed. The two stateful
/// collections use <c>[CollectionDefinition(DisableParallelization = true)]</c> so they never
/// overlap; pure tests still run in parallel.
/// </summary>
internal static class TestServerInitializer
{
    private static bool _initialized;
    private static readonly Lock _lock = new();

    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            Core.ApplicationAssembly = Assembly.GetExecutingAssembly();
            Core.LoopContext = new EventLoopContext();
            Core.Expansion = Expansion.EJ;

            ServerConfiguration.Load(true);
            ServerConfiguration.AssemblyDirectories.Add(Core.BaseDirectory);

            // Required for the pathfinding tests (real .mul tile data). Harmless for the rest.
            var clientFiles = Environment.GetEnvironmentVariable("MODERNUO_TEST_DATA_DIR")
                              ?? @"C:\Ultima Online Classic";
            ServerConfiguration.DataDirectories.Add(clientFiles);

            AssemblyHandler.LoadAssemblies(["Server.dll", "UOContent.dll"]);

            SkillsInfo.Configure();
            Server.Network.NetState.Configure();
            TestMapDefinitions.ConfigureTestMapDefinitions();

            // TileData's static cctor short-circuits when running under xUnit
            // (see Server/TileData.cs:295). Force-load via reflection so LandTable/ItemTable
            // flags are populated before anything that reads TileData (MultiData, MovementImpl,
            // CheckMovement). Without this, TileData.MaxItemValue is 0 at MultiData.Configure()
            // time, causing every MCL tile ID to be masked to 0 and stored as ID=0 in Tiles[x][y].
            ForceLoadTileData();

            // Production runs every static Configure() via AssemblyHandler.Invoke("Configure");
            // the fixture calls a curated subset, so configure the pathfinding singleton here so
            // BitmapAStarAlgorithm.Instance carries its configured MaxSearchNodes before any test
            // calls Find. ServerConfiguration is already loaded above, so the setting resolves.
            BitmapAStarAlgorithm.Configure();

            // Multi component lists (multi.mul / MultiCollection.uop). Production invokes this via
            // AssemblyHandler.Invoke("Configure"); the curated fixture subset must call it so that
            // BaseMulti.Components (MultiData.GetComponents) returns real footprints instead of
            // MultiComponentList.Empty. Required by the Multi pathfinding tests.
            MultiData.Configure();

            World.Configure();
            Timer.Init(0);
            RaceDefinitions.Configure();
            MovementImpl.Configure();
            PathFollower.Configure();
            World.Load();
            World.ExitSerializationThreads();
            DecayScheduler.Configure();

            VerifyTrammelTileDataLoaded();

            _initialized = true;
        }
    }

    private static void ForceLoadTileData()
    {
        var loadMethod = typeof(TileData).GetMethod(
            "Load",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        if (loadMethod == null)
        {
            throw new InvalidOperationException(
                "TileData.Load not found via reflection — engine may have refactored."
            );
        }
        loadMethod.Invoke(null, null);
    }

    private static void VerifyTrammelTileDataLoaded()
    {
        var trammel = Map.Maps[1];
        if (trammel == null)
        {
            throw new InvalidOperationException(
                "Trammel (mapId=1) was not registered. Check TestMapDefinitions."
            );
        }

        var tile = trammel.Tiles.GetLandTile(1500, 1600);
        if (tile.ID == 0)
        {
            throw new InvalidOperationException(
                $"Trammel tile data did not load — GetLandTile(1500,1600) returned ID 0. " +
                $"Verify Distribution/Data/map1*.mul (or map1LegacyMUL.uop) is present at " +
                $"{Path.Combine(Core.BaseDirectory, "Data")}."
            );
        }
    }
}
