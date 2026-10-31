using System;
using System.Collections.Generic;
using Server;
using Server.Engines.Spawners;
using Server.Mobiles;
using Xunit;

namespace UOContent.Tests.Engines.Spawners;

[Collection("Sequential UOContent Tests")]
public class SpawnerCantWalkTests
{
    // Open default ground (walkable land at Z 0), clear of the prefab scenes.
    private static readonly Point3D SpawnerLocation = new(1800, 1800, 0);

    [Fact]
    public void CantWalkEntry_SpreadsOverBounds_InsteadOfStackingOnSpawner()
    {
        const int amount = 30;

        var spawner = new Spawner();
        try
        {
            spawner.InitSpawn(amount, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
            spawner.MoveToWorld(SpawnerLocation, Map.Felucca);
            spawner.AddEntry("Rabbit", 100, amount, false, "CantWalk true");

            spawner.Respawn();

            Assert.Equal(amount, spawner.Spawned.Count);

            var bounds = spawner.SpawnBounds;
            var onSpawnerTile = 0;
            var tiles = new HashSet<Point2D>();

            foreach (var spawned in spawner.Spawned.Keys)
            {
                var m = Assert.IsAssignableFrom<Mobile>(spawned);
                Assert.True(m.CantWalk);
                Assert.True(
                    m.X >= bounds.Start.X && m.X < bounds.End.X && m.Y >= bounds.Start.Y && m.Y < bounds.End.Y,
                    $"{m.Location} is outside the spawn bounds"
                );

                tiles.Add(new Point2D(m.X, m.Y));
                if (m.X == SpawnerLocation.X && m.Y == SpawnerLocation.Y)
                {
                    onSpawnerTile++;
                }
            }

            // 30 picks over 81 tiles: a handful may land on the spawner's tile by chance, never all of them.
            Assert.True(onSpawnerTile < amount / 2, $"{onSpawnerTile} of {amount} spawned on the spawner's own tile");
            Assert.True(tiles.Count > 1, "every spawn stacked on one tile");
            Assert.NotEqual(SpawnPositionMode.Abandoned, spawner.SpawnPositionMode);
        }
        finally
        {
            spawner.Delete();
        }
    }
}
