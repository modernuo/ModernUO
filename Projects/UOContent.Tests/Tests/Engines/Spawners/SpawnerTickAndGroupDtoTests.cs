using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Engines.Spawners;
using Xunit;

namespace Server.Tests.Engines.Spawners;

[Collection("Sequential UOContent Tests")]
public class SpawnerTickAndGroupDtoTests
{
    private sealed class GatedSpawner : Spawner
    {
        public int Ticks;
        public bool Gate = false;

        public GatedSpawner() : base(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, default, "Rabbit")
        {
        }

        public GatedSpawner(Serial serial) : base(serial)
        {
        }

        public override void OnTick()
        {
            Ticks++;
            if (Gate)
            {
                base.OnTick();
            }
        }
    }

    [Fact]
    public void OnTick_IsVirtual_AndManualSpawnBypassesIt()
    {
        var spawner = new GatedSpawner();
        spawner.MoveToWorld(new Point3D(1500, 1500, 0), Map.Felucca);
        try
        {
            spawner.OnTick();
            Assert.Equal(1, spawner.Ticks);
            Assert.Empty(spawner.Spawned);      // gate closed: base.OnTick not reached

            spawner.Spawn();                    // manual API does not go through OnTick
            Assert.Equal(1, spawner.Ticks);
            Assert.Single(spawner.Spawned);
        }
        finally
        {
            spawner.Delete();
        }
    }

    [Fact]
    public void Dto_RoundTrip_CarriesGroup()
    {
        var spawner = new Spawner(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, default, "Rabbit");
        spawner.MoveToWorld(new Point3D(1500, 1500, 0), Map.Felucca);
        Spawner loaded = null;
        try
        {
            spawner.Group = true;
            var json = SpawnerJsonSerializer.SerializeCompact(new List<SpawnerDto> { spawner.ToDto() });
            Assert.Contains("\"group\"", json);

            var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(json, SpawnerJsonSerializer.Options);
            loaded = (Spawner)dtos[0].ToSpawner();
            Assert.True(loaded.Group);
        }
        finally
        {
            loaded?.Delete();
            spawner.Delete();
        }
    }
}
