using System;
using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Server.Regions;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class SpawnerDtoRoundTripTests
{
    [Fact]
    public void Spawner_RoundTrips_ThroughDto()
    {
        Spawner original = null;
        BaseSpawner rebuilt = null;
        try
        {
            original = new Spawner(2, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(7), 0,
                new Rectangle3D(100, 100, 0, 5, 5, 0), "Fisherman");
            original.MoveToWorld(new Point3D(105, 105, 0), Map.Felucca);

            var json = JsonSerializer.Serialize(new List<SpawnerDto> { original.ToDto() }, SpawnerJsonSerializer.Options);
            Assert.Contains("\"$type\": \"Spawner\"", json);
            Assert.Contains("\"count\": 2", json);

            var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(json, SpawnerJsonSerializer.Options);
            rebuilt = Assert.Single(dtos).ToSpawner();
            var s = Assert.IsType<Spawner>(rebuilt);
            Assert.Equal(2, s.Count);
            Assert.Equal(TimeSpan.FromMinutes(3), s.MinDelay);
            Assert.Equal(new Rectangle3D(100, 100, 0, 5, 5, 0), s.SpawnBounds);
            Assert.Equal("Fisherman", Assert.Single(s.Entries).SpawnedName);
        }
        finally
        {
            rebuilt?.Delete();
            original?.Delete();
        }
    }

    [Fact]
    public void Spawner_OmitsDomainDefaults()
    {
        Spawner original = null;
        try
        {
            original = new Spawner("Fisherman");
            original.MoveToWorld(new Point3D(110, 110, 0), Map.Felucca);
            var json = JsonSerializer.Serialize(new List<SpawnerDto> { original.ToDto() }, SpawnerJsonSerializer.Options);
            Assert.DoesNotContain("minDelay", json);
            Assert.DoesNotContain("maxDelay", json);
            Assert.DoesNotContain("\"team\"", json);
            Assert.DoesNotContain("maxSpawnAttempts", json);
        }
        finally
        {
            original?.Delete();
        }
    }

    [Fact]
    public void RegionSpawner_RoundTrips_RegionByName()
    {
        var region = new BaseRegion("DtoTestRegion", Map.Felucca, 50, new Rectangle3D(1400, 1670, 0, 40, 40, 0));
        region.Register();
        RegionSpawner original = null;
        BaseSpawner rebuilt = null;
        try
        {
            original = new RegionSpawner("Fisherman") { SpawnRegion = region };
            original.MoveToWorld(new Point3D(1416, 1683, 0), Map.Felucca);
            var json = JsonSerializer.Serialize(new List<SpawnerDto> { original.ToDto() }, SpawnerJsonSerializer.Options);
            Assert.Contains("\"$type\": \"RegionSpawner\"", json);
            Assert.Contains("DtoTestRegion", json);

            var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(json, SpawnerJsonSerializer.Options);
            rebuilt = Assert.Single(dtos).ToSpawner();
            Assert.Equal("DtoTestRegion", Assert.IsType<RegionSpawner>(rebuilt).SpawnRegion?.Name);
        }
        finally
        {
            rebuilt?.Delete();
            original?.Delete();
            region.Unregister();
        }
    }

    [Fact]
    public void ProximitySpawner_RoundTrips_Fields()
    {
        ProximitySpawner original = null;
        BaseSpawner rebuilt = null;
        try
        {
            original = new ProximitySpawner("Fisherman") { TriggerRange = 4, InstantFlag = true, SpawnMessage = 500000 };
            original.MoveToWorld(new Point3D(120, 120, 0), Map.Felucca);
            var json = JsonSerializer.Serialize(new List<SpawnerDto> { original.ToDto() }, SpawnerJsonSerializer.Options);
            Assert.Contains("\"$type\": \"ProximitySpawner\"", json);
            Assert.Contains("\"triggerRange\": 4", json);
            Assert.Contains("\"instant\": true", json);

            var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(json, SpawnerJsonSerializer.Options);
            rebuilt = Assert.Single(dtos).ToSpawner();
            var p = Assert.IsType<ProximitySpawner>(rebuilt);
            Assert.Equal(4, p.TriggerRange);
            Assert.True(p.InstantFlag);
            Assert.Equal(500000, p.SpawnMessage.Number);
        }
        finally
        {
            rebuilt?.Delete();
            original?.Delete();
        }
    }
}
