// Validates that the spawnBounds JSON emitted by tools/spawner-json-migrate/migrate.mjs
// deserializes to the SAME Rectangle3D that the runtime homeRange path produces.
// This test MUST pass before bulk-migrating the spawn data files.
//
// toBounds formula (from migrate.mjs):
//   hr == 0 -> { x1:x, y1:y, z1:z, x2:x+1, y2:y+1, z2:z }
//   hr  > 0 -> { x1:x-hr, y1:y-hr, z1:-128, x2:x+hr+1, y2:y+hr+1, z2:128 }
//
// Rectangle3DConverter x1/y1/z1/x2/y2/z2 form creates Rectangle3D(Point3D(x1,y1,z1), Point3D(x2,y2,z2))
// where _start==(x1,y1,z1) and _end==(x2,y2,z2). Rectangle3D.End is exclusive.

using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class BoundsEquivalenceTests
{
    private const int LocX = 100;
    private const int LocY = 200;
    private const int LocZ = 5;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(7)]
    public void SpawnBoundsJson_EqualsHomeRangePath(int hr)
    {
        BaseSpawner fromHomeRange = null;
        BaseSpawner fromSpawnBounds = null;
        try
        {
            // Path A: legacy homeRange field -> ApplyDto sets SpawnBounds
            var homeRangeJson = BuildHomeRangeJson(hr);
            var dtosA = JsonSerializer.Deserialize<List<SpawnerDto>>(homeRangeJson, SpawnerJsonSerializer.Options);
            fromHomeRange = Assert.Single(dtosA).ToSpawner();
            var expected = Assert.IsType<Spawner>(fromHomeRange).SpawnBounds;

            // Path B: migrated spawnBounds field -> SpawnerDataDto.ToSpawner sets SpawnBounds
            var spawnBoundsJson = BuildSpawnBoundsJson(hr);
            var dtosB = JsonSerializer.Deserialize<List<SpawnerDto>>(spawnBoundsJson, SpawnerJsonSerializer.Options);
            fromSpawnBounds = Assert.Single(dtosB).ToSpawner();
            var actual = Assert.IsType<Spawner>(fromSpawnBounds).SpawnBounds;

            Assert.Equal(expected, actual);
        }
        finally
        {
            fromHomeRange?.Delete();
            fromSpawnBounds?.Delete();
        }
    }

    private static string BuildHomeRangeJson(int hr) => $$"""
        [
          {
            "$type": "Spawner",
            "location": [{{LocX}}, {{LocY}}, {{LocZ}}],
            "map": "Felucca",
            "count": 1,
            "homeRange": {{hr}},
            "entries": []
          }
        ]
        """;

    private static string BuildSpawnBoundsJson(int hr)
    {
        int x1, y1, z1, x2, y2, z2;
        if (hr == 0)
        {
            // hr==0: _start=(x,y,z), _end=(x+1,y+1,z) — single tile, depth 0
            (x1, y1, z1, x2, y2, z2) = (LocX, LocY, LocZ, LocX + 1, LocY + 1, LocZ);
        }
        else
        {
            // hr>0: _start=(x-hr,y-hr,-128), _end=(x+hr+1,y+hr+1,128) — hr*2+1 wide, depth 256
            (x1, y1, z1, x2, y2, z2) = (LocX - hr, LocY - hr, -128, LocX + hr + 1, LocY + hr + 1, 128);
        }

        return $$"""
            [
              {
                "$type": "Spawner",
                "location": [{{LocX}}, {{LocY}}, {{LocZ}}],
                "map": "Felucca",
                "count": 1,
                "spawnBounds": { "x1": {{x1}}, "y1": {{y1}}, "z1": {{z1}}, "x2": {{x2}}, "y2": {{y2}}, "z2": {{z2}} },
                "entries": []
              }
            ]
            """;
    }
}
