using System;
using System.IO;
using System.Reflection;
using Server.Items;
using Server.Misc;
using Server.Movement;
using Server.Tests.Maps;
using Xunit;

namespace Server.Tests.Pathfinding;

[CollectionDefinition("Sequential Pathfinding Tests", DisableParallelization = true)]
public class PathfindingTestFixture : ICollectionFixture<PathfindingTestFixture>, IDisposable
{
    public PathfindingTestFixture()
    {
        Core.ApplicationAssembly = Assembly.GetExecutingAssembly();
        Core.LoopContext = new EventLoopContext();
        Core.Expansion = Expansion.EJ;

        ServerConfiguration.Load(true);
        ServerConfiguration.AssemblyDirectories.Add(Core.BaseDirectory);

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
        World.Load();
        World.ExitSerializationThreads();
        DecayScheduler.Configure();

        // TileData's static cctor short-circuits when running under xUnit
        // (see Server/TileData.cs:295). Force-load via reflection so LandTable/ItemTable
        // flags are populated; without this, every tile reads as flag=None and
        // MovementImpl.CheckMovement treats everything as walkable.
        ForceLoadTileData();

        VerifyTrammelTileDataLoaded();
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

    public void Dispose()
    {
        Timer.Init(0);
    }
}
