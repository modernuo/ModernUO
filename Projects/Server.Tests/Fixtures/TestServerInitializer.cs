using System;
using System.Reflection;
using System.Threading;
using Server.Items;
using Server.Tests.Maps;
using Server.Tests.Terrain;

namespace Server.Tests;

/// <summary>
/// Shared server initialization logic for test fixtures.
/// Ensures the server is only initialized once across all test collections.
/// </summary>
internal static class TestServerInitializer
{
    private static bool _initialized;
    private static readonly Lock _lock = new();

    /// <summary>
    /// Always true: the host loads generated tile data. Kept until every guard that reads it is gone.
    /// </summary>
    public static bool TileDataLoaded => true;

    /// <summary>
    /// Initializes the test server. Safe to call multiple times - only initializes once.
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            Core.ApplicationAssembly = Assembly.GetExecutingAssembly();

            // Load Configurations
            ServerConfiguration.Load(true);

            SyntheticWorld.LoadData();

            // Load an empty assembly list into the resolver
            ServerConfiguration.AssemblyDirectories.Add(Core.BaseDirectory);
            AssemblyHandler.LoadAssemblies(["Server.dll"]);

            Core.LoopContext = new EventLoopContext();
            Core.Expansion = Expansion.EJ;

            // Seed the loop clock as Main.cs does before the Configure sweep; otherwise Core.Now is
            // DateTime.MinValue for the whole test host.
            Core._now = DateTime.UtcNow;

            // Timer wheel must exist before NetState.Configure(), which schedules a recurring
            // sweep via Timer.DelayCall (matches production ordering in Main.cs: Timer.Init runs
            // before AssemblyHandler.Invoke("Configure")).
            Timer.Init(0);

            // Configure networking (initializes RingSocketManager for tests)
            Server.Network.NetState.Configure();

            // Configure / Initialize
            TestMapDefinitions.ConfigureTestMapDefinitions();
            SyntheticWorld.StampTerrain();

            // Configure the world
            World.Configure();

            // Load the world
            World.Load();

            World.ExitSerializationThreads();

            DecayScheduler.Configure();

            _initialized = true;
        }
    }
}
