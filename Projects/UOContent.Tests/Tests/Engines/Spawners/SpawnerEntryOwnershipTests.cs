using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

    [Fact]
    public void CopyEntriesTo_Self_IsNoOp()
    {
        var spawner = new Spawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Rabbit", "Bird");
        var first = spawner.Entries[0];
        var second = spawner.Entries[1];

        spawner.CopyEntriesTo(spawner);

        Assert.Equal(2, spawner.Entries.Count);
        Assert.Same(first, spawner.Entries[0]);
        Assert.Same(second, spawner.Entries[1]);

        spawner.Delete();
    }

    [Fact]
    public void RemoveEntry_ForeignEntry_IsIgnored()
    {
        var a = new Spawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Rabbit");
        var b = new Spawner(1, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2), 0, default, "Bird");

        var foreign = b.Entries[0];
        var spawned = new Item(0x1f13);
        foreign.AddToSpawned(spawned);

        a.RemoveEntry(foreign);

        Assert.Single(a.Entries);
        Assert.Equal("Rabbit", a.Entries[0].SpawnedName);
        Assert.Single(b.Entries);
        Assert.Same(foreign, b.Entries[0]);

        // A foreign entry's live spawns must survive a RemoveEntry on the wrong spawner.
        Assert.False(spawned.Deleted);
        Assert.Single(foreign.Spawned);

        spawned.Delete();
        a.Delete();
        b.Delete();
    }

    // Manual benchmark harness. Skipped in normal CI/test runs; run it directly (temporarily
    // removing the Skip) to collect numbers when evaluating the spawn-path performance impact
    // of a change. See task-7-report.md for recorded before/after numbers.
    [Fact(Skip = "manual benchmark")]
    public void Benchmark_SpawnPath_Manual()
    {
        const int iterations = 100_000;
        const int warmup = 1_000;

        var report = new StringBuilder();
        report.AppendLine();

        foreach (var entryCount in new[] { 1, 10, 50 })
        {
            var spawner = new Spawner(1000, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
            spawner.MoveToWorld(new Point3D(1500, 1500, 0), Map.Felucca);

            // SpawnedMaxCount = 0 makes every entry IsFull immediately, so Spawn() walks the
            // entry list to compute probsum, finds it <= 0, and returns without constructing
            // anything. This isolates the O(entries) selection cost from spawn/construction cost.
            for (var i = 0; i < entryCount; i++)
            {
                spawner.AddEntry("Rabbit", 100, 0, false);
            }

            for (var i = 0; i < warmup; i++)
            {
                spawner.Spawn();
            }

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                spawner.Spawn();
            }

            sw.Stop();

            var nsPerCall = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / iterations;
            report.AppendLine(
                $"Spawn() entries={entryCount,2}: {nsPerCall,8:F1} ns/call  ({iterations} iterations, {sw.ElapsedMilliseconds} ms total)"
            );

            spawner.Delete();
        }

        {
            var spawner = new Spawner(1000, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
            spawner.MoveToWorld(new Point3D(1500, 1500, 0), Map.Felucca);

            for (var i = 0; i < 10; i++)
            {
                spawner.AddEntry("Rabbit", 100, 1, false);
            }

            spawner.Spawn();
            Assert.Single(spawner.Spawned);
            var rabbit = spawner.Spawned.Keys.First();

            for (var i = 0; i < warmup; i++)
            {
                spawner.Remove(rabbit);
            }

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                // Idempotent after the first successful removal; every call still runs
                // Defrag() over the spawner's entries.
                spawner.Remove(rabbit);
            }

            sw.Stop();

            var nsPerCall = sw.Elapsed.TotalMilliseconds * 1_000_000.0 / iterations;
            report.AppendLine(
                $"Remove(ISpawnable) entries=10: {nsPerCall,8:F1} ns/call  ({iterations} iterations, {sw.ElapsedMilliseconds} ms total)"
            );

            (rabbit as Mobile)?.Delete();
            spawner.Delete();
        }

        // Throw so the numbers surface in test output when this Fact is run manually
        // (Skip removed temporarily) - Console.WriteLine is not surfaced by the runner.
        throw new Xunit.Sdk.XunitException(report.ToString());
    }
}
