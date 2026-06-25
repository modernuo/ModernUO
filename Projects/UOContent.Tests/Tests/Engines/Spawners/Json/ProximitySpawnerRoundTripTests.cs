using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class ProximitySpawnerRoundTripTests
{
    [Fact]
    public void ProximitySpawner_RoundTrips_ProximityFields()
    {
        ProximitySpawner spawner = null;
        ProximitySpawner s = null;
        try
        {
            spawner = new ProximitySpawner("Fisherman") { TriggerRange = 4, InstantFlag = true };
            spawner.SpawnMessage = 500000;
            spawner.MoveToWorld(new Point3D(120, 120, 0), Map.Felucca);

            var json = JsonSerializer.Serialize<List<BaseSpawner>>(
                new List<BaseSpawner> { spawner }, SpawnerJsonSerializer.Options);
            Assert.Contains("\"$type\": \"ProximitySpawner\"", json);
            Assert.Contains("\"triggerRange\": 4", json);
            Assert.Contains("\"instant\": true", json);

            var rt = JsonSerializer.Deserialize<List<BaseSpawner>>(json, SpawnerJsonSerializer.Options);
            s = Assert.IsType<ProximitySpawner>(Assert.Single(rt));
            Assert.Equal(4, s.TriggerRange);
            Assert.True(s.InstantFlag);
            Assert.Equal(500000, s.SpawnMessage.Number);
        }
        finally
        {
            s?.Delete();
            spawner?.Delete();
        }
    }
}
