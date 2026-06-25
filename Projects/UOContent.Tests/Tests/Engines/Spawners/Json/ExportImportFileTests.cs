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
        Spawner original = null;
        BaseSpawner rebuilt = null;
        var path = Path.GetTempFileName();
        try
        {
            original = new Spawner(3, TimeSpan.FromMinutes(4), TimeSpan.FromMinutes(8), 1,
                new Rectangle3D(200, 200, 0, 9, 9, 0), "Tanner");
            original.MoveToWorld(new Point3D(204, 204, 0), Map.Felucca);

            JsonConfig.Serialize(path, new List<SpawnerDto> { original.ToDto() }, SpawnerJsonSerializer.Options);

            var dtos = JsonConfig.Deserialize<List<SpawnerDto>>(path, SpawnerJsonSerializer.Options);
            rebuilt = Assert.Single(dtos).ToSpawner();
            var s = Assert.IsType<Spawner>(rebuilt);
            Assert.Equal(3, s.Count);
            Assert.Equal(1, s.Team);
            Assert.Equal(new Rectangle3D(200, 200, 0, 9, 9, 0), s.SpawnBounds);
        }
        finally
        {
            rebuilt?.Delete();
            original?.Delete();
            File.Delete(path);
        }
    }
}
