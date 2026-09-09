using System;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Spawners;

[Collection("Sequential UOContent Tests")]
public class SpawnerEntryOwnershipTests
{
    [Fact]
    public void Disabled_DefaultsFalse_AndRoundTripsBinary()
    {
        var spawner = new Spawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Rabbit", "Bird");
        Assert.False(spawner.Entries[0].Disabled);
        Assert.True(spawner.Entries[0].Enabled);

        spawner.Entries[1].Disabled = true;

        var bytes = SpawnerBlob.Write(spawner);
        var loaded = SpawnerBlob.Read<Spawner>(bytes, (Serial)0x40001234);

        Assert.Equal(2, loaded.Entries.Count);
        Assert.False(loaded.Entries[0].Disabled);
        Assert.True(loaded.Entries[1].Disabled);

        spawner.Delete();
        loaded.Delete();
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
        var spawner = new Spawner();
        Assert.Empty(spawner.Entries);

        var a = spawner.AddEntry("Rabbit", 100, 2, false);
        var b = spawner.AddEntry("Bird", 50, 1, false, "Hue 33", null);
        Assert.Equal(2, spawner.Entries.Count);
        Assert.Same(a, spawner.Entries[0]);
        Assert.Equal("Hue 33", spawner.Entries[1].Properties);

        spawner.RemoveEntry(a);
        Assert.Single(spawner.Entries);
        Assert.Same(b, spawner.Entries[0]);

        spawner.ClearEntries();
        Assert.Empty(spawner.Entries);

        spawner.Delete();
    }

    [Fact]
    public void Start_WorksAfterStop_WhenOwnerHasEntries()
    {
        var spawner = new Spawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Rabbit");
        Assert.True(spawner.Running);
        spawner.Stop();
        Assert.False(spawner.Running);
        spawner.Start();
        Assert.True(spawner.Running);
        spawner.Delete();
    }

    [Fact]
    public void Dupe_ClonesEntriesIntoIndependentList()
    {
        var source = new Spawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Rabbit", "Bird");
        source.Entries[1].Disabled = true;

        var copy = new Spawner();
        source.Dupe(copy);

        Assert.Equal(2, copy.Entries.Count);
        Assert.NotSame(source.Entries[0], copy.Entries[0]);
        Assert.Equal("Bird", copy.Entries[1].SpawnedName);
        Assert.True(copy.Entries[1].Disabled);
        Assert.NotEqual(source.Guid, copy.Guid);

        source.AddEntry("Orc", 100, 1, false);
        Assert.Equal(2, copy.Entries.Count);

        source.Delete();
        copy.Delete();
    }

    [Fact]
    public void CopyEntriesTo_ReplacesTargetEntries()
    {
        var source = new Spawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Rabbit");
        var target = new Spawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Orc", "Troll");

        source.CopyEntriesTo(target);

        Assert.Single(target.Entries);
        Assert.Equal("Rabbit", target.Entries[0].SpawnedName);
        Assert.NotSame(source.Entries[0], target.Entries[0]);

        source.Delete();
        target.Delete();
    }
}
