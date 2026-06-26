using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class SpawnerCompactWriterTests
{
    [Fact]
    public void SerializeCompact_ProducesCompactLoadableFormat()
    {
        Spawner original = null;
        BaseSpawner rebuilt = null;
        try
        {
            original = new Spawner("Fisherman");
            original.MoveToWorld(new Point3D(200, 200, 0), Map.Felucca);
            original.SpawnBounds = new Rectangle3D(195, 195, -128, 11, 11, 256); // == homeRange 5

            var json = SpawnerJsonSerializer.SerializeCompact(new List<SpawnerDto> { original.ToDto() });

            // No BOM (StartsWith '[' proves it), UTF-8, LF, trailing newline.
            Assert.StartsWith("[", json);
            Assert.DoesNotContain("\r", json);
            Assert.EndsWith("]\n", json);

            // $type first; scalar containers inline; entries (array of objects) expanded.
            Assert.Contains("\"$type\": \"Spawner\"", json);
            Assert.Contains("\"location\": [200, 200, 0]", json);
            Assert.Contains("\"homeRange\": 5", json);
            Assert.DoesNotContain("spawnBounds", json);
            Assert.Contains("\"entries\": [\n", json);          // array of objects -> expanded
            Assert.Contains("{ \"name\": \"Fisherman\",", json); // each entry inline

            // Round-trips back to an equivalent spawner.
            var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(json, SpawnerJsonSerializer.Options);
            rebuilt = Assert.Single(dtos).ToSpawner();
            Assert.Equal(new Rectangle3D(195, 195, -128, 11, 11, 256), rebuilt.SpawnBounds);
        }
        finally
        {
            rebuilt?.Delete();
            original?.Delete();
        }
    }
}
