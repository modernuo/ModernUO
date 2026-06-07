using System;
using System.IO;
using Server.Engines.Pathing.Cache;
using Xunit;

namespace Server.Tests.Pathfinding;

// v7 = per-chunk libdeflate compression on top of the v6 predictive-Z format. Each record is
// compressed independently (random access preserved) behind a u32 uncompressed-length prefix;
// records that do not shrink (tiny Uniform records) are stored raw.
[Collection("Sequential Pathfinding Tests")]
public class StepCacheFileV7Tests
{
    private static StepChunk VariedChunk(int multis = 3)
    {
        var c = new StepChunk { BuiltMultisVersion = multis };
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            c.WalkMask[i] = (byte)(i & 0xFF);
            c.WetMask[i] = (byte)((i * 7) & 0xFF);
            c.SourceZ[i] = (sbyte)(i % 40 - 20);
            c.WalkZN[i] = (sbyte)(c.SourceZ[i] + i % 3);
            c.SwimZS[i] = (sbyte)(c.SourceZ[i] - i % 2);
        }
        return c;
    }

    private static StepChunk UniformChunk(byte walk = 0xC1, sbyte z = 10, int multis = 7)
    {
        var c = new StepChunk { BuiltMultisVersion = multis };
        Array.Fill(c.WalkMask, walk);
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

    private static string Write1(StepChunk c, int cx, int cy)
    {
        var path = Path.Combine(Path.GetTempPath(), $"swbv7_{Guid.NewGuid():N}.swb");
        var emitted = false;
        StepCacheFile.Write(path, 1u, 1u, (out int ox, out int oy, out StepChunk oc) =>
        {
            if (emitted) { ox = oy = 0; oc = null!; return false; }
            emitted = true; ox = cx; oy = cy; oc = c; return true;
        });
        return path;
    }

    private static StepChunk RoundTrip(StepChunk src, int cx, int cy, out long fileLen)
    {
        var path = Write1(src, cx, cy);
        try
        {
            fileLen = new FileInfo(path).Length;
            using var reader = StepCacheFile.OpenForLazy(path);
            Assert.NotNull(reader);
            var rt = reader!.TryReadChunk(cx, cy);
            Assert.NotNull(rt);
            return rt!;
        }
        finally { File.Delete(path); }
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

    [Fact]
    public void Varied_Compresses_AndRoundTrips()
    {
        var src = VariedChunk(multis: 4);
        var rt = RoundTrip(src, 1, 2, out var fileLen);
        AssertBaseEqual(src, rt);
        // The uncompressed v6 Full record for a varied chunk is > 5 KB. Compressed + header(48)
        // + index(20), the whole file must be well under that — proving compression engaged.
        Assert.True(fileLen < 4000, $"expected compression to shrink the record; file was {fileLen} bytes");
    }

    [Fact]
    public void Uniform_StoredRaw_RoundTrips()
    {
        // A Uniform record body is ~24 bytes; libdeflate cannot shrink it, so WriteChunk stores it
        // raw (payload length == uncompressed length). The reader must take the raw path and rebuild.
        var src = UniformChunk(walk: 0xC1, z: 12, multis: 9);
        var rt = RoundTrip(src, 5, 6, out var fileLen);
        AssertBaseEqual(src, rt);
        Assert.True(fileLen < 200, $"uniform record should stay tiny; file was {fileLen} bytes");
    }

    [Fact]
    public void V6_IsRejected()
    {
        var path = Write1(UniformChunk(), 0, 0);
        try
        {
            var bytes = File.ReadAllBytes(path);
            bytes[4] = 6; bytes[5] = 0; bytes[6] = 0; bytes[7] = 0; // version 6 < MinSupportedVersion 7
            File.WriteAllBytes(path, bytes);
            Assert.Null(StepCacheFile.OpenForLazy(path));
        }
        finally { File.Delete(path); }
    }
}
