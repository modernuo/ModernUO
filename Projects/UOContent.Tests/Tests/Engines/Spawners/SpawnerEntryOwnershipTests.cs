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
}
