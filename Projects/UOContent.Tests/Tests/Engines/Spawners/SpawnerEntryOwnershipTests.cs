using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Server.Tests;
using Server.Text;
using Xunit;

namespace UOContent.Tests.Engines.Spawners;

[Collection("Sequential UOContent Tests")]
public class SpawnerEntryOwnershipTests : IDisposable
{
    // Deleted in Dispose so a failed assertion cannot leave spawners registered in the world.
    private readonly List<Item> _created = [];

    public void Dispose()
    {
        for (var i = 0; i < _created.Count; i++)
        {
            _created[i].Delete();
        }
    }

    private T Track<T>(T item) where T : Item
    {
        _created.Add(item);
        return item;
    }

    private Spawner NewSpawner(params string[] names) =>
        Track(new Spawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, names));

    [Fact]
    public void Disabled_DefaultsFalse_AndRoundTripsBinary()
    {
        var spawner = NewSpawner("Rabbit", "Bird");
        Assert.False(spawner.Entries[0].Disabled);
        Assert.True(spawner.Entries[0].Enabled);

        spawner.Entries[1].Disabled = true;

        var bytes = SpawnerBlob.Write(spawner);
        var loaded = Track(SpawnerBlob.Read<Spawner>(bytes, (Serial)0x40001234));

        Assert.Equal(2, loaded.Entries.Count);
        Assert.False(loaded.Entries[0].Disabled);
        Assert.True(loaded.Entries[1].Disabled);
    }

    [Fact]
    public void Disabled_IsOmittedFromJsonWhenFalse_AndWrittenWhenTrue()
    {
        var entry = new SpawnerEntry("Rabbit");
        var json = JsonSerializer.Serialize(entry, SpawnerJsonSerializer.Options);
        Assert.DoesNotContain("disabled", json);

        entry.Disabled = true;
        json = JsonSerializer.Serialize(entry, SpawnerJsonSerializer.Options);
        Assert.Contains("\"disabled\": true", json);
    }

    [Fact]
    public void AddRemoveClear_OperateOnOwnerList()
    {
        var spawner = Track(new Spawner());
        Assert.Empty(spawner.Entries);

        var a = spawner.AddEntry("Rabbit", 100, 2, false);
        var b = spawner.AddEntry("Bird", 50, 1, false, "Hue 33", null);
        Assert.Equal(2, spawner.Entries.Count);
        Assert.Same(a, spawner.Entries[0]);
        Assert.Equal("Hue 33", spawner.Entries[1].Properties);

        spawner.RemoveEntry(a);
        Assert.Single(spawner.Entries);
        Assert.Same(b, spawner.Entries[0]);

        spawner.RemoveAllEntries();
        Assert.Empty(spawner.Entries);
    }

    [Fact]
    public void Dupe_ClonesEntriesIntoIndependentList()
    {
        var source = NewSpawner("Rabbit", "Bird");
        source.Entries[1].Disabled = true;

        var copy = Track(new Spawner());
        source.Dupe(copy);

        Assert.Equal(2, copy.Entries.Count);
        Assert.NotSame(source.Entries[0], copy.Entries[0]);
        Assert.Equal("Bird", copy.Entries[1].SpawnedName);
        Assert.True(copy.Entries[1].Disabled);
        Assert.NotEqual(source.Guid, copy.Guid);

        source.AddEntry("Orc", 100, 1, false);
        Assert.Equal(2, copy.Entries.Count);
    }

    [Fact]
    public void CopyEntriesTo_ReplacesTargetEntries()
    {
        var source = NewSpawner("Rabbit");
        var target = NewSpawner("Orc", "Troll");

        source.CopyEntriesTo(target);

        Assert.Single(target.Entries);
        Assert.Equal("Rabbit", target.Entries[0].SpawnedName);
        Assert.NotSame(source.Entries[0], target.Entries[0]);
    }

    [Fact]
    public void CopyEntriesTo_Self_IsNoOp()
    {
        var spawner = NewSpawner("Rabbit", "Bird");
        var first = spawner.Entries[0];
        var second = spawner.Entries[1];

        spawner.CopyEntriesTo(spawner);

        Assert.Equal(2, spawner.Entries.Count);
        Assert.Same(first, spawner.Entries[0]);
        Assert.Same(second, spawner.Entries[1]);
    }

    [Fact]
    public void RemoveEntry_ForeignEntry_IsIgnored()
    {
        var a = NewSpawner("Rabbit");
        var b = NewSpawner("Bird");

        var foreign = b.Entries[0];
        var spawned = Track(new Item(0x1f13));
        foreign.AddToSpawned(spawned);

        a.RemoveEntry(foreign);

        Assert.Single(a.Entries);
        Assert.Equal("Rabbit", a.Entries[0].SpawnedName);
        Assert.Single(b.Entries);
        Assert.Same(foreign, b.Entries[0]);

        Assert.False(spawned.Deleted);
        Assert.Single(foreign.Spawned);
    }

    // Manual: remove the Skip, run, and read the numbers from the thrown assertion.
}
