using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Server.Regions;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class RegionSpawnerRoundTripTests
{
    [Fact]
    public void RegionSpawner_RoundTrips_RegionByName()
    {
        // The test environment does not load game regions (no AssemblyHandler.Invoke("Initialize")),
        // so we create and register a test BaseRegion on Felucca directly.
        var region = new BaseRegion(
            "TestSpawnRegion",
            Map.Felucca,
            50,
            new Rectangle3D(1400, 1670, -128, 40, 40, 256)
        );
        region.Register();

        try
        {
            var spawner = new RegionSpawner("Fisherman") { SpawnRegion = region };
            spawner.MoveToWorld(new Point3D(1416, 1683, 0), Map.Felucca);

            var json = JsonSerializer.Serialize<List<BaseSpawner>>(
                new List<BaseSpawner> { spawner }, SpawnerJsonSerializer.Options);
            Assert.Contains("\"$type\": \"RegionSpawner\"", json);
            Assert.Contains(region.Name, json);

            var rt = JsonSerializer.Deserialize<List<BaseSpawner>>(json, SpawnerJsonSerializer.Options);
            var s = Assert.IsType<RegionSpawner>(Assert.Single(rt));
            Assert.Equal(region.Name, s.SpawnRegion?.Name);

            s.Delete();
            spawner.Delete();
        }
        finally
        {
            region.Unregister();
        }
    }
}
