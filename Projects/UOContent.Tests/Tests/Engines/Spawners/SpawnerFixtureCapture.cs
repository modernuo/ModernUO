using System;
using System.IO;
using System.Reflection;
using Server.Engines.Spawners;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Spawners;

[Collection("Sequential UOContent Tests")]
public class SpawnerFixtureCapture
{
    // Freezes the BaseSpawner v12 save layout as test input; refuses to run against newer code.
    [SkippableFact]
    public void CaptureLegacyBlobs()
    {
        Skip.If(Environment.GetEnvironmentVariable("MODERNUO_CAPTURE_SPAWNER_FIXTURES") != "1");

        var version = (int)typeof(BaseSpawner)
            .GetField("SerializationVersion", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetRawConstantValue()!;
        Assert.True(version == 12, $"Fixtures must be captured with BaseSpawner v12; current version is {version}.");

        var dir = Path.Combine(AppContext.BaseDirectory, "Tests", "Engines", "Spawners", "Fixtures");
        Directory.CreateDirectory(dir);

        var spawner = new Spawner(2, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Rabbit", "Bird")
        {
            Name = "FixtureSpawner"
        };
        spawner.Entries[1].Properties = "Hue 33";
        File.WriteAllBytes(Path.Combine(dir, "spawner.v12-v1.bin"), SpawnerBlob.Write(spawner));

        var proximity = new ProximitySpawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, 5, "boo", true, "Rat", "Bird")
        {
            Name = "FixtureProximity"
        };
        File.WriteAllBytes(Path.Combine(dir, "proximity.v12-v1-v0.bin"), SpawnerBlob.Write(proximity));

        var region = new RegionSpawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, "Orc", "Troll")
        {
            Name = "FixtureRegion"
        };
        File.WriteAllBytes(Path.Combine(dir, "region.v12-v1-v0.bin"), SpawnerBlob.Write(region));

        spawner.Delete();
        proximity.Delete();
        region.Delete();
    }
}
