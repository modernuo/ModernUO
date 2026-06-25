using System;
using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class SpawnerRoundTripTests
{
    private static Map Map => Map.Felucca;

    [Fact]
    public void Spawner_RoundTrips_TypeAndCoreFields()
    {
        var spawner = new Spawner(2, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(7), 0,
            new Rectangle3D(100, 100, 0, 5, 5, 0), "Fisherman");
        spawner.MoveToWorld(new Point3D(105, 105, 0), Map);

        var json = JsonSerializer.Serialize<List<BaseSpawner>>(
            new List<BaseSpawner> { spawner }, SpawnerJsonSerializer.Options);

        Assert.Contains("\"$type\": \"Spawner\"", json);
        Assert.Contains("\"count\": 2", json);

        var roundTripped = JsonSerializer.Deserialize<List<BaseSpawner>>(json, SpawnerJsonSerializer.Options);
        var s = Assert.IsType<Spawner>(Assert.Single(roundTripped));
        Assert.Equal(2, s.Count);
        Assert.Equal(TimeSpan.FromMinutes(3), s.MinDelay);
        Assert.Equal(TimeSpan.FromMinutes(7), s.MaxDelay);
        Assert.Equal(new Rectangle3D(100, 100, 0, 5, 5, 0), s.SpawnBounds);
        Assert.Single(s.Entries);
        Assert.Equal("Fisherman", s.Entries[0].SpawnedName);

        s.Delete();
        spawner.Delete();
    }

    [Fact]
    public void Spawner_OmitsDomainDefaults()
    {
        // Default delays (5/10 min), team 0, default maxSpawnAttempts → omitted.
        var spawner = new Spawner("Fisherman");
        spawner.MoveToWorld(new Point3D(110, 110, 0), Map);

        var json = JsonSerializer.Serialize<List<BaseSpawner>>(
            new List<BaseSpawner> { spawner }, SpawnerJsonSerializer.Options);

        Assert.DoesNotContain("minDelay", json);
        Assert.DoesNotContain("maxDelay", json);
        Assert.DoesNotContain("\"team\"", json);
        Assert.DoesNotContain("maxSpawnAttempts", json);

        spawner.Delete();
    }
}
