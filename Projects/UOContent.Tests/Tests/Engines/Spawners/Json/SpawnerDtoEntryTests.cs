using System;
using System.Collections.Generic;
using System.Text.Json;
using Server;
using Server.Engines.Spawners;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

[Collection("Sequential UOContent Tests")]
public class SpawnerDtoEntryTests
{
    [Fact]
    public void Export_WritesEntriesAtSameJsonPosition_WithDisabledOnlyWhenSet()
    {
        var spawner = new Spawner(1, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10), 0, default, "Rabbit", "Bird");
        spawner.Entries[1].Disabled = true;

        var json = SpawnerJsonSerializer.SerializeCompact(new List<SpawnerDto> { spawner.ToDto() });

        Assert.Contains("\"entries\": [\n      { \"name\": \"Rabbit\"", json);
        Assert.Contains("{ \"name\": \"Bird\"", json);
        Assert.Equal(1, CountOccurrences(json, "\"disabled\": true"));

        spawner.Delete();
    }

    [Fact]
    public void Import_AdoptsDeserializedEntryObjects()
    {
        var json = """
            [{"$type":"Spawner","guid":"11111111-1111-1111-1111-111111111111","location":[1500,1500,0],"map":"Felucca","count":1,"minDelay":"00:05:00","maxDelay":"00:10:00","homeRange":4,"entries":[{"name":"Rabbit","probability":100,"maxCount":1,"disabled":true}]}]
            """;
        var dtos = JsonSerializer.Deserialize<List<SpawnerDto>>(json, SpawnerJsonSerializer.Options);
        var spawner = dtos![0].ToSpawner();

        Assert.Single(spawner.Entries);
        Assert.True(spawner.Entries[0].Disabled);
        Assert.Same(dtos[0].EntryView[0], spawner.Entries[0]);

        spawner.Delete();
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
