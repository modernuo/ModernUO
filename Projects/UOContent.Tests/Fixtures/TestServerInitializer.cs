using System;
using System.Reflection;
using System.Threading;
using Server.Items;
using Server.Misc;
using Server.Movement;
using Server.PathAlgorithms;
using Server.Tests.Maps;
using Server.Tests.Terrain;

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
///
/// The host runs on generated tile data, terrain and multi footprints (see <see cref="SyntheticWorld"/>),
/// never on client files, so every machine and CI run the same world.
/// </summary>
internal static class TestServerInitializer
{
    private static bool _initialized;
    private static readonly Lock _lock = new();

    /// <summary>
    /// Always true: the host loads generated tile data. Kept until every guard that reads it is gone.
    /// </summary>
    public static bool TileDataLoaded => true;

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

            // TileData's static cctor short-circuits under xUnit, so the generated tiledata.mul must load
            // before anything reads LandTable/ItemTable (the multi footprints, MovementImpl, CheckMovement).
            SyntheticWorld.LoadData();

            AssemblyHandler.LoadAssemblies(["Server.dll", "UOContent.dll"]);

            SkillsInfo.Configure();

            // Seed the loop clock as Main.cs does before the Configure sweep; otherwise Core.Now is
            // DateTime.MinValue for the whole test host.
            Core._now = DateTime.UtcNow;

            // Timer wheel must exist before NetState.Configure(), which schedules a recurring
            // sweep via Timer.DelayCall (matches production ordering in Main.cs: Timer.Init runs
            // before AssemblyHandler.Invoke("Configure")).
            Timer.Init(0);
            Server.Network.NetState.Configure();
            TestMapDefinitions.ConfigureTestMapDefinitions();
            SyntheticWorld.StampTerrain();

            // Production runs every static Configure() via AssemblyHandler.Invoke("Configure");
            // the fixture calls a curated subset, so configure the pathfinding singleton here so
            // BitmapAStarAlgorithm.Instance carries its configured MaxSearchNodes before any test
            // calls Find. ServerConfiguration is already loaded above, so the setting resolves.
            BitmapAStarAlgorithm.Configure();

            World.Configure();
            // Registers the Accounts entity persistence; without it no test can construct an Account.
            Server.Accounting.Accounts.Configure();
            RaceDefinitions.Configure();
            Server.Movement.Movement.Configure();
            MovementImpl.Configure();
            PathFollower.Configure();
            World.Load();
            World.ExitSerializationThreads();
            DecayScheduler.Configure();
            // Without npc-speeds.json every BaseCreature constructor throws.
            Server.Mobiles.NPCSpeeds.Configure();
            Server.Engines.Spawners.SpawnerJsonSerializer.Configure();

            _initialized = true;
        }
    }
}
