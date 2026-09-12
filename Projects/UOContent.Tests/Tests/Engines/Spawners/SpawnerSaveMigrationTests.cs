using System;
using System.IO;
using Server;
using Server.Engines.Spawners;
using Server.Items;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Spawners;

// Byte-for-byte save blobs through the same BufferWriter path production saves use.
internal static class SpawnerBlob
{
    public static byte[] Write(Item item)
    {
        var writer = new BufferWriter(true);
        item.Serialize(writer);
        return writer.Buffer[..(int)writer.Position];
    }

    public static T Read<T>(byte[] bytes, Serial serial) where T : Item
    {
        var item = (T)Activator.CreateInstance(typeof(T), serial)!;
        var reader = new BufferReader(bytes);
        item.Deserialize(reader);
        return item;
    }
}

[Collection("Sequential UOContent Tests")]
public class SpawnerSaveMigrationTests
{
    private static byte[] Fixture(string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Tests", "Engines", "Spawners", "Fixtures", name));

    [Fact]
    public void V12Spawner_EntriesAreAdoptedByOwner()
    {
        var loaded = SpawnerBlob.Read<Spawner>(Fixture("spawner.v12-v1.bin"), (Serial)0x40001001);

        Assert.Equal("FixtureSpawner", loaded.Name);
        Assert.Equal(2, loaded.Entries.Count);
        Assert.Equal("Rabbit", loaded.Entries[0].SpawnedName);
        Assert.Equal("Hue 33", loaded.Entries[1].Properties);
        Assert.False(loaded.Entries[0].Disabled);
        Assert.NotNull(loaded.Spawned);
        Assert.Empty(loaded.Spawned);

        loaded.Delete();
    }

    [Fact]
    public void V12ProximityAndRegion_EntriesAreAdoptedThroughSubclasses()
    {
        var prox = SpawnerBlob.Read<ProximitySpawner>(Fixture("proximity.v12-v1-v0.bin"), (Serial)0x40001002);
        Assert.Equal(2, prox.Entries.Count);
        Assert.Equal("Rat", prox.Entries[0].SpawnedName);
        Assert.Equal("Bird", prox.Entries[1].SpawnedName);
        Assert.Equal(5, prox.TriggerRange);
        Assert.True(prox.InstantFlag);

        var region = SpawnerBlob.Read<RegionSpawner>(Fixture("region.v12-v1-v0.bin"), (Serial)0x40001003);
        Assert.Equal(2, region.Entries.Count);
        Assert.Equal("Orc", region.Entries[0].SpawnedName);
        Assert.Equal("Troll", region.Entries[1].SpawnedName);

        prox.Delete();
        region.Delete();
    }

    [Fact]
    public void NewFormat_RoundTripsByteIdentical_WithLiveSpawnReferences()
    {
        var spawner = new Spawner(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, default, "Rabbit");
        spawner.MoveToWorld(new Point3D(1500, 1500, 0), Map.Felucca);
        spawner.Spawn();
        Assert.Single(spawner.Spawned);

        var first = SpawnerBlob.Write(spawner);
        var loaded = SpawnerBlob.Read<Spawner>(first, (Serial)0x40001004);
        Assert.Single(loaded.Entries);
        Assert.Single(loaded.Entries[0].Spawned);
        Assert.Single(loaded.Spawned);

        var second = SpawnerBlob.Write(loaded);
        Assert.Equal(first, second);

        loaded.Delete();
        spawner.Delete();
    }
}
