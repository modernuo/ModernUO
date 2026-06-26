using System;
using System.Collections.Generic;
using Server.Engines.Spawners;
using Server.Json;
using Xunit;

namespace UOContent.Tests.Engines.Spawners.Json;

public class SpawnerDiscoveryValidationTests
{
    [JsonDiscoverableType("dup")]
    private sealed record DupA : SpawnerDto
    {
        protected override BaseSpawner CreateEmpty() => new Spawner();
    }

    [JsonDiscoverableType("dup")]
    private sealed record DupB : SpawnerDto
    {
        protected override BaseSpawner CreateEmpty() => new Spawner();
    }

    [Fact]
    public void DuplicateDiscriminator_Throws()
    {
        var map = new Dictionary<string, Type>();
        var (disc, _) = SpawnerJsonSerializer.Validate(typeof(DupA), map);
        map[disc] = typeof(DupA);

        var ex = Assert.Throws<Exception>(() => SpawnerJsonSerializer.Validate(typeof(DupB), map));
        Assert.Contains("discriminator 'dup'", ex.Message);
    }
}
