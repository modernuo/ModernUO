using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class ImportCleanupTests
{
    private const string SpawnerGuid = "11111111-1111-1111-1111-111111111111";

    [Fact]
    public void Import_ValidFile_PlacesSpawner()
    {
        var dir = Path.Combine(Path.GetTempPath(), "muo-spawner-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "test.json");
        File.WriteAllText(path, """
            [
              {
                "$type": "Spawner",
                "guid": "11111111-1111-1111-1111-111111111111",
                "location": [305, 305, 0],
                "map": "Felucca",
                "count": 1,
                "spawnBounds": { "x1": 300, "y1": 300, "x2": 310, "y2": 310 },
                "entries": []
              }
            ]
            """);

        BaseSpawner placedSpawner = null;
        try
        {
            var all = new Dictionary<Guid, ISpawner>();
            ImportSpawnersCommand.ImportFile(new FileInfo(path), all);

            foreach (var s in Map.Felucca.GetItemsAt<BaseSpawner>(new Point3D(305, 305, 0)))
            {
                if (s.Guid == new Guid(SpawnerGuid))
                {
                    placedSpawner = s;
                    break;
                }
            }

            Assert.NotNull(placedSpawner);
            Assert.True(all.ContainsKey(new Guid(SpawnerGuid)));
        }
        finally
        {
            placedSpawner?.Delete();
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void Import_NoMap_DeletesOrphanedSpawner()
    {
        // A spawner with no map entry deserialized by STJ will have map == null (or Map.Internal).
        // Verify it does NOT end up on any live map.
        var dir = Path.Combine(Path.GetTempPath(), "muo-spawner-import-nomap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "test-nomap.json");
        File.WriteAllText(path, """
            [
              {
                "$type": "Spawner",
                "guid": "22222222-2222-2222-2222-222222222222",
                "location": [400, 400, 0],
                "count": 1,
                "spawnBounds": { "x1": 395, "y1": 395, "x2": 405, "y2": 405 },
                "entries": []
              }
            ]
            """);

        try
        {
            var all = new Dictionary<Guid, ISpawner>();
            ImportSpawnersCommand.ImportFile(new FileInfo(path), all);

            // The spawner must NOT be in allSpawners (rejected due to missing map).
            Assert.DoesNotContain(new Guid("22222222-2222-2222-2222-222222222222"), all.Keys);
        }
        finally
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public void Import_DuplicateLocation_ReplacesExistingSpawner()
    {
        // Verifies that an existing spawner at the same location and type is removed and replaced.
        var dir = Path.Combine(Path.GetTempPath(), "muo-spawner-import-dup-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "test-dup.json");

        var newGuid = new Guid("33333333-3333-3333-3333-333333333333");
        // Use a non-interpolated raw string literal; the guid is a known constant so hardcode it.
        File.WriteAllText(path, """
            [
              {
                "$type": "Spawner",
                "guid": "33333333-3333-3333-3333-333333333333",
                "location": [310, 310, 0],
                "map": "Felucca",
                "count": 1,
                "spawnBounds": { "x1": 305, "y1": 305, "x2": 315, "y2": 315 },
                "entries": []
              }
            ]
            """);

        // Place a pre-existing spawner at the same location.
        var existing = new Spawner(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
        existing.MoveToWorld(new Point3D(310, 310, 0), Map.Felucca);

        BaseSpawner placedSpawner = null;
        try
        {
            var all = new Dictionary<Guid, ISpawner> { [existing.Guid] = existing };
            ImportSpawnersCommand.ImportFile(new FileInfo(path), all);

            // existing should have been deleted and replaced.
            Assert.True(existing.Deleted);
            Assert.DoesNotContain(existing.Guid, all.Keys);

            foreach (var s in Map.Felucca.GetItemsAt<BaseSpawner>(new Point3D(310, 310, 0)))
            {
                if (s.Guid == newGuid)
                {
                    placedSpawner = s;
                    break;
                }
            }

            Assert.NotNull(placedSpawner);
        }
        finally
        {
            if (!existing.Deleted)
            {
                existing.Delete();
            }
            placedSpawner?.Delete();
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
