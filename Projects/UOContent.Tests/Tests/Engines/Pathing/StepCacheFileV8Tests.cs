using System;
using System.Collections.Generic;
using System.IO;
using Server.Engines.Pathing.Cache;
using Xunit;

namespace Server.Tests.Pathfinding;

// v8 = compact index on top of the v7 compression format. The trailer drops the per-chunk file
// offset (reconstructed by cumulative record length in write order) and packs the key to u32.
// These tests focus on the multi-chunk case — single-chunk round-trips (covered by v6/v7 tests)
// never exercise cumulative offset reconstruction, which is the only new risk here.
[Collection("Sequential Pathfinding Tests")]
public class StepCacheFileV8Tests
{
    private static StepChunk VariedChunk(int seed, int multis)
    {
        var c = new StepChunk { BuiltMultisVersion = multis };
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            c.WalkMask[i] = (byte)((i + seed) & 0xFF);
            c.WetMask[i] = (byte)((i * 7 + seed) & 0xFF);
            c.SourceZ[i] = (sbyte)(((i + seed) % 40) - 20);
            c.WalkZN[i] = (sbyte)(c.SourceZ[i] + (i % 3));
            c.SwimZS[i] = (sbyte)(c.SourceZ[i] - (i % 2));
        }
        return c;
    }

    private static StepChunk UniformChunk(sbyte z, int multis)
    {
        var c = new StepChunk { BuiltMultisVersion = multis };
        Array.Fill(c.WalkMask, (byte)0xC1);
        Array.Fill(c.SourceZ, z);
        foreach (var arr in new[]
                 {
                     c.WalkZN, c.WalkZNE, c.WalkZE, c.WalkZSE, c.WalkZS, c.WalkZSW, c.WalkZW, c.WalkZNW,
                     c.SwimZN, c.SwimZNE, c.SwimZE, c.SwimZSE, c.SwimZS, c.SwimZSW, c.SwimZW, c.SwimZNW
                 })
        {
            Array.Fill(arr, z);
        }
        return c;
    }

    private static void AssertBaseEqual(StepChunk a, StepChunk b)
    {
        Assert.Equal(a.BuiltMultisVersion, b.BuiltMultisVersion);
        Assert.True(a.WalkMask.AsSpan().SequenceEqual(b.WalkMask));
        Assert.True(a.WetMask.AsSpan().SequenceEqual(b.WetMask));
        Assert.True(a.SourceZ.AsSpan().SequenceEqual(b.SourceZ));
        var az = new[] { a.WalkZN, a.WalkZNE, a.WalkZE, a.WalkZSE, a.WalkZS, a.WalkZSW, a.WalkZW, a.WalkZNW,
                         a.SwimZN, a.SwimZNE, a.SwimZE, a.SwimZSE, a.SwimZS, a.SwimZSW, a.SwimZW, a.SwimZNW };
        var bz = new[] { b.WalkZN, b.WalkZNE, b.WalkZE, b.WalkZSE, b.WalkZS, b.WalkZSW, b.WalkZW, b.WalkZNW,
                         b.SwimZN, b.SwimZNE, b.SwimZE, b.SwimZSE, b.SwimZS, b.SwimZSW, b.SwimZW, b.SwimZNW };
        for (var i = 0; i < az.Length; i++)
        {
            Assert.True(az[i].AsSpan().SequenceEqual(bz[i]), $"base Z array {i} differs");
        }
    }

    private static string WriteMany(IReadOnlyList<(int cx, int cy, StepChunk c)> chunks)
    {
        var path = Path.Combine(Path.GetTempPath(), $"swbv8_{Guid.NewGuid():N}.swb");
        var idx = 0;
        StepCacheFile.Write(path, 1u, (uint)chunks.Count, (out int ox, out int oy, out StepChunk oc) =>
        {
            if (idx >= chunks.Count) { ox = oy = 0; oc = null!; return false; }
            var e = chunks[idx++];
            ox = e.cx; oy = e.cy; oc = e.c;
            return true;
        });
        return path;
    }

    [Fact]
    public void MultiChunk_RoundTrips_WithDerivedOffsets()
    {
        // Distinct chunks at distinct coords. A wrong derived offset would read another chunk's
        // bytes, so per-chunk identity verifies cumulative offset reconstruction across records.
        var chunks = new List<(int, int, StepChunk)>
        {
            (1, 1, VariedChunk(seed: 3, multis: 2)),
            (2, 5, UniformChunk(z: 14, multis: 9)),    // tiny record (stored-raw path) in the middle
            (10, 3, VariedChunk(seed: 99, multis: 4)),
            (300, 200, VariedChunk(seed: 17, multis: 5)), // large packed-key coords (high 16 bits)
        };

        var path = WriteMany(chunks);
        try
        {
            using var reader = StepCacheFile.OpenForLazy(path);
            Assert.NotNull(reader);
            Assert.Equal((uint)chunks.Count, reader!.ChunkCount);

            foreach (var (cx, cy, src) in chunks)
            {
                Assert.True(reader.Has(cx, cy), $"missing chunk ({cx},{cy})");
                var rt = reader.TryReadChunk(cx, cy);
                Assert.NotNull(rt);
                AssertBaseEqual(src, rt!);
            }

            // A coordinate that was never written must not resolve.
            Assert.Null(reader.TryReadChunk(7, 7));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void V7_IsRejected()
    {
        var path = WriteMany(new List<(int, int, StepChunk)> { (0, 0, UniformChunk(z: 10, multis: 1)) });
        try
        {
            var bytes = File.ReadAllBytes(path);
            bytes[4] = 7; bytes[5] = 0; bytes[6] = 0; bytes[7] = 0; // version 7 < MinSupportedVersion 8
            File.WriteAllBytes(path, bytes);
            Assert.Null(StepCacheFile.OpenForLazy(path));
        }
        finally { File.Delete(path); }
    }
}
