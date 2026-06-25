using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class LegacyHomeRangeTests
{
    [Fact]
    public void HomeRange_NoSpawnBounds_ProducesCenteredBounds()
    {
        const string legacy = """
        [
          {
            "$type": "Spawner",
            "guid": "3df0543a-373c-4673-a98b-8191686f4ab3",
            "location": [100, 200, 5],
            "map": "Felucca",
            "count": 1,
            "homeRange": 3,
            "entries": [ { "name": "Fisherman", "maxCount": 1, "probability": 100 } ]
          }
        ]
        """;

        var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(legacy, SpawnerJsonSerializer.Options);
        var dto = Assert.Single(dtos);
        var s = Assert.IsType<Spawner>(dto.ToSpawner());

        try
        {
            // homeRange 3 -> Rectangle3D(100-3, 200-3, -128, 7, 7, 256)
            Assert.Equal(new Rectangle3D(97, 197, -128, 7, 7, 256), s.SpawnBounds);
        }
        finally
        {
            s.Delete();
        }
    }

    [Fact]
    public void HomeRangeZero_ProducesSingleTileBounds()
    {
        const string legacy = """
        [
          {
            "$type": "Spawner",
            "location": [100, 200, 5],
            "map": "Felucca",
            "count": 1,
            "homeRange": 0,
            "entries": [ { "name": "Fisherman", "maxCount": 1, "probability": 100 } ]
          }
        ]
        """;

        var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(legacy, SpawnerJsonSerializer.Options);
        var dto = Assert.Single(dtos);
        var s = Assert.IsType<Spawner>(dto.ToSpawner());

        try
        {
            // homeRange 0 -> Rectangle3D(100, 200, 5, 1, 1, 0)
            Assert.Equal(new Rectangle3D(100, 200, 5, 1, 1, 0), s.SpawnBounds);
        }
        finally
        {
            s.Delete();
        }
    }
}
