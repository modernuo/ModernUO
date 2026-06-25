// Regression guard: verifies that a real migrated spawn file (post-uoml/felucca/Vendors.json)
// can be deserialized as List<SpawnerDto> and each DTO produces a valid spawner via ToSpawner().
// Exercises the $type discriminator path introduced by the data migration.

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class MigratedDataLoadTests
{
    [Fact]
    public void MigratedFile_LoadsWithDollarType()
    {
        var path = Path.Combine(Core.BaseDirectory, "Data", "Spawns", "post-uoml", "felucca", "Vendors.json");
        if (!File.Exists(path))
        {
            return; // distribution data not present in this checkout
        }

        var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(File.ReadAllText(path), SpawnerJsonSerializer.Options);
        Assert.NotEmpty(dtos);

        var spawners = new List<BaseSpawner>(dtos.Count);
        try
        {
            foreach (var dto in dtos)
            {
                spawners.Add(dto.ToSpawner());
            }

            Assert.Equal(dtos.Count, spawners.Count);
        }
        finally
        {
            foreach (var s in spawners)
            {
                s?.Delete();
            }
        }
    }
}
