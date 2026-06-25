using System;
using System.Collections.Generic;
using System.IO;
using Server;
using Server.Engines.Spawners;
using Server.Json;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class ExportImportFileTests
{
    [Fact]
    public void Serialize_ThenDeserialize_File_PreservesSpawner()
    {
        var spawner = new Spawner(3, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(8), 1,
            new Rectangle3D(200, 200, 0, 9, 9, 0), "Tanner");
        spawner.MoveToWorld(new Point3D(204, 204, 0), Map.Felucca);

        var path = Path.GetTempFileName();
        try
        {
            JsonConfig.Serialize(path, new List<BaseSpawner> { spawner }, SpawnerJsonSerializer.Options);

            var loaded = JsonConfig.Deserialize<List<BaseSpawner>>(path, SpawnerJsonSerializer.Options);
            var s = Assert.IsType<Spawner>(Assert.Single(loaded));
            Assert.Equal(3, s.Count);
            Assert.Equal(1, s.Team);
            Assert.Equal(new Rectangle3D(200, 200, 0, 9, 9, 0), s.SpawnBounds);

            s.Delete();
        }
        finally
        {
            File.Delete(path);
            spawner?.Delete();
        }
    }
}
