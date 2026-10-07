using System;
using System.Collections.Generic;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Tests;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class TreasureMapChestGuardianTests
{
    // A treasure.cfg chest site with room to dig the chest up and spawn its guardians.
    private const int SiteX = 2643;
    private const int SiteY = 851;

    // The guardian list starts null and is created by the first guardian added. A level 0 chest
    // stays shut to players until its guardians are dead.
    [Fact]
    public void AddToGuardians_OnNewChest_KeepsLevelZeroChestShut()
    {
        var map = Map.Felucca;
        var location = new Point3D(5000, 610, 0);
        var player = CreatePlayerMobile(map, location);
        var guardian = new Mobile(World.NewMobile);
        guardian.DefaultMobileInit();
        var chest = new TreasureMapChest(0);

        try
        {
            Assert.Null(chest.Guardians);

            chest.AddToGuardians(guardian);

            Assert.Same(guardian, Assert.Single(chest.Guardians));
            Assert.True(chest.CheckLocked(player));

            guardian.Delete();

            Assert.False(chest.CheckLocked(player));
        }
        finally
        {
            chest.Delete();
            guardian.Delete();
            player.Delete();
        }
    }

    // Drives the real dig, one DigTimer tick per call, until the chest surfaces and its guardians
    // spawn. Levels 0 and 2+ spawn guardians; level 1 spawns none.
    [SkippableTheory]
    [InlineData(0)]
    [InlineData(2)]
    public void Dig_AddsSpawnedGuardiansToTheChest(int level)
    {
        TileDataRequirement.SkipIfMissing();

        var map = Map.Felucca;
        var site = new Point3D(SiteX, SiteY, map.GetAverageZ(SiteX, SiteY));
        var player = CreatePlayerMobile(map, new Point3D(site.X + 1, site.Y, site.Z));
        var treasureMap = new TreasureMap(level, map);

        try
        {
            Assert.True(map.CanFit(site.X, site.Y, site.Z, 16, true));
            Assert.True(player.BeginAction<TreasureMap>());

            var timerType = typeof(TreasureMap).GetNestedType("DigTimer", BindingFlags.NonPublic)!;
            var onTick = timerType.GetMethod("OnTick", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var digTimer = Activator.CreateInstance(timerType, player, treasureMap, site, map);

            for (var i = 0; i < 30 && !treasureMap.Completed; i++)
            {
                onTick.Invoke(digTimer, BindingFlags.DoNotWrapExceptions, null, null, null);
            }

            var chest = FindChest(map, site);

            Assert.True(treasureMap.Completed);
            Assert.NotNull(chest);
            Assert.NotNull(chest.Guardians);
            Assert.NotEmpty(chest.Guardians);
        }
        finally
        {
            Cleanup(map, site);
            treasureMap.Delete();
            player.Delete();
        }
    }

    private static TreasureMapChest FindChest(Map map, Point3D site)
    {
        foreach (var chest in map.GetItemsInRange<TreasureMapChest>(site, 0))
        {
            return chest;
        }

        return null;
    }

    // Sweeps the site rather than trusting references, so a dig that throws partway still leaves
    // nothing behind for the next case: the chest, both dirt piles and any guardian spawned so far.
    private static void Cleanup(Map map, Point3D site)
    {
        var leftovers = new List<IEntity>();

        foreach (var m in map.GetMobilesInRange<BaseCreature>(site, 3))
        {
            leftovers.Add(m);
        }

        foreach (var item in map.GetItemsInRange<TreasureMapChest>(site, 1))
        {
            leftovers.Add(item);
        }

        foreach (var item in map.GetItemsInRange<TreasureChestDirt>(site, 1))
        {
            leftovers.Add(item);
        }

        foreach (var entity in leftovers)
        {
            entity.Delete();
        }
    }

    private static PlayerMobile CreatePlayerMobile(Map map, Point3D location)
    {
        var mobile = new PlayerMobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.MoveToWorld(location, map);
        return mobile;
    }
}
