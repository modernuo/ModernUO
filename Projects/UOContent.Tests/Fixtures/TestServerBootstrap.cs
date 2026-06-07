using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Server.Items;
using Server.Misc;
using Server.Movement;
using Server.Tests.Maps;

namespace Server.Tests;

/// <summary>
/// Single, process-wide ModernUO bootstrap for the test host.
///
/// ModernUO is designed to bootstrap its global singletons (Core, ServerConfiguration,
/// AssemblyHandler, NetState/io-ring, World, Timer, and the serialization worker threads)
/// exactly once per process. <see cref="World.Load"/> is guarded to run once, and
/// <see cref="World.ExitSerializationThreads"/> must run once against the live workers — a
/// second pass would (prior to the idempotent <c>SerializationThreadWorker.Exit()</c> guard)
/// deadlock on a worker whose thread has already exited.
///
/// Each xUnit collection has its own fixture instance, so without this shared guard every
/// collection would re-run the full bootstrap. This runs the superset bootstrap (the union of
/// what every collection needs, including the UO client data directory and TileData load)
/// exactly once, no matter how many collection fixtures are constructed. Combined with
/// <c>[assembly: CollectionBehavior(DisableTestParallelization = true)]</c>, collections run
/// strictly sequentially and share one initialized world.
/// </summary>
internal static class TestServerBootstrap
{
    private static readonly Lock _sync = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_sync)
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

            World.Configure();
            Timer.Init(0);
            RaceDefinitions.Configure();
            MovementImpl.Configure();
            PathFollower.Configure();
            World.Load();
            World.ExitSerializationThreads();
            DecayScheduler.Configure();

            // TileData's static cctor short-circuits when running under xUnit
            // (see Server/TileData.cs:295). Force-load via reflection so LandTable/ItemTable
            // flags are populated; without this, every tile reads as flag=None and
            // MovementImpl.CheckMovement treats everything as walkable.
            ForceLoadTileData();

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
