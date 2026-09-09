using System;
using System.IO;
using Server.Engines.Spawners;
using Server.Tests;
using Xunit;

namespace UOContent.Tests.Engines.Spawners;

[Collection("Sequential UOContent Tests")]
public class SpawnerFixtureCapture
{
    // Run once, on the commit BEFORE the entry-ownership change, with
    // MODERNUO_CAPTURE_SPAWNER_FIXTURES=1, to freeze the legacy save layout as test input.
    [SkippableFact]
    public void CaptureLegacyBlobs()
    {
        Skip.If(Environment.GetEnvironmentVariable("MODERNUO_CAPTURE_SPAWNER_FIXTURES") != "1");

        var dir = Path.Combine(AppContext.BaseDirectory, "Tests", "Engines", "Spawners", "Fixtures");
        Directory.CreateDirectory(dir);

        var spawner = new Spawner(2, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Rabbit", "Bird")
        {
            Name = "FixtureSpawner"
        };
        spawner.Entries[1].Properties = "Hue 33";
        File.WriteAllBytes(Path.Combine(dir, "spawner.v12-v1.bin"), SpawnerBlob.Write(spawner));

        var proximity = new ProximitySpawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, 5, "boo", true, "Rat")
        {
            Name = "FixtureProximity"
        };
        File.WriteAllBytes(Path.Combine(dir, "proximity.v12-v1-v0.bin"), SpawnerBlob.Write(proximity));

        var region = new RegionSpawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, "Orc")
        {
            Name = "FixtureRegion"
        };
        File.WriteAllBytes(Path.Combine(dir, "region.v12-v1-v0.bin"), SpawnerBlob.Write(region));

        spawner.Delete();
        proximity.Delete();
        region.Delete();
    }
}
