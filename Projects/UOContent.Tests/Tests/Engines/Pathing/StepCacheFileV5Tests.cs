using System;
using System.IO;
using Server.Engines.Pathing.Cache;
using Xunit;

namespace Server.Tests.Pathfinding;

// v5 = uniform-chunk elision on top of the v4 swim-layer format. A uniform chunk (no strata,
// no swim layer, all 19 base arrays constant) serializes to ~28 bytes; Full chunks (incl. swim
// layer + strata) round-trip byte-identically.
[Collection("Sequential Pathfinding Tests")]
public class StepCacheFileV5Tests
{
    private static StepChunk UniformChunk(byte walk = 0xC1, byte wet = 0x00, sbyte z = 10, int multis = 7)
    {
        var c = new StepChunk { BuiltMultisVersion = multis };
        Array.Fill(c.WalkMask, walk);
        Array.Fill(c.WetMask, wet);
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

    private static StepChunk VariedChunk(int multis = 3)
    {
        var c = new StepChunk { BuiltMultisVersion = multis };
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            c.WalkMask[i] = (byte)(i & 0xFF);
            c.WetMask[i] = (byte)((i * 7) & 0xFF);
            c.SourceZ[i] = (sbyte)((i % 40) - 20);
            c.WalkZN[i] = (sbyte)(c.SourceZ[i] + (i % 3));
            c.SwimZS[i] = (sbyte)(c.SourceZ[i] - (i % 2));
        }
        return c;
    }

    private static StepChunk SwimChunk(int multis = 6)
    {
        var c = VariedChunk(multis);
        c.AllocateSwimLayer();
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            c.SwimSourceZ[i] = (sbyte)((i % 30) - 15);
            c.SwimMask[i] = (byte)((i * 5) & 0xFF);
            c.SwimZN_Layer[i] = (sbyte)(i % 7);
            c.SwimZNW_Layer[i] = (sbyte)(-(i % 4));
        }
        return c;
    }

    private static void AssertChunksEqual(StepChunk a, StepChunk b)
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

        Assert.Equal(a.HasSwimLayer, b.HasSwimLayer);
        if (a.HasSwimLayer)
        {
            Assert.True(a.SwimSourceZ.AsSpan().SequenceEqual(b.SwimSourceZ));
            Assert.True(a.SwimMask.AsSpan().SequenceEqual(b.SwimMask));
            var al = new[] { a.SwimZN_Layer, a.SwimZNE_Layer, a.SwimZE_Layer, a.SwimZSE_Layer,
                             a.SwimZS_Layer, a.SwimZSW_Layer, a.SwimZW_Layer, a.SwimZNW_Layer };
            var bl = new[] { b.SwimZN_Layer, b.SwimZNE_Layer, b.SwimZE_Layer, b.SwimZSE_Layer,
                             b.SwimZS_Layer, b.SwimZSW_Layer, b.SwimZW_Layer, b.SwimZNW_Layer };
            for (var i = 0; i < al.Length; i++)
            {
                Assert.True(al[i].AsSpan().SequenceEqual(bl[i]), $"swim-layer Z array {i} differs");
            }
        }
    }

    private static string Write1(StepChunk c, int cx, int cy)
    {
        var path = Path.Combine(Path.GetTempPath(), $"swbv5_{Guid.NewGuid():N}.swb");
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

    [Fact]
    public void IsUniform_TrueForAllIdentical_FalseForVariedStrataOrSwim()
    {
        Assert.True(UniformChunk().IsUniform());

        var varied = UniformChunk();
        varied.WalkZE[42] = 99;
        Assert.False(varied.IsUniform());

        var strata = UniformChunk();
        var offsets = new ushort[StepChunk.CellsPerChunk];
        Array.Fill(offsets, StepChunk.NoStrata);
        offsets[0] = 0;
        strata.SetStrata(offsets, new byte[] { 0 });
        Assert.False(strata.IsUniform());

        var swim = UniformChunk();
        swim.AllocateSwimLayer(); // a uniform-looking base but with a swim layer is NOT uniform
        Assert.False(swim.IsUniform());
    }

    [Fact]
    public void Uniform_RoundTrips_Identically_AndIsCompact()
    {
        var src = UniformChunk(walk: 0xC1, wet: 0x00, z: 12, multis: 9);
        var rt = RoundTrip(src, 5, 6, out var fileLen);
        Assert.True(fileLen < 200, $"uniform .swb too large: {fileLen}");
        AssertChunksEqual(src, rt);
    }

    [Fact]
    public void Varied_Full_RoundTrips_Identically()
    {
        var src = VariedChunk(multis: 4);
        AssertChunksEqual(src, RoundTrip(src, 1, 2, out _));
    }

    [Fact]
    public void SwimLayer_Full_RoundTrips_Identically()
    {
        var src = SwimChunk(multis: 8);
        var rt = RoundTrip(src, 7, 8, out _);
        Assert.True(rt.HasSwimLayer);
        AssertChunksEqual(src, rt);
    }

    [Fact]
    public void Strata_Full_RoundTrips_Identically()
    {
        var src = VariedChunk(multis: 5);
        var offsets = new ushort[StepChunk.CellsPerChunk];
        Array.Fill(offsets, StepChunk.NoStrata);
        offsets[10] = 0;
        var data = new byte[1 + StepChunk.StratumByteLength];
        data[0] = 1;
        src.SetStrata(offsets, data);

        var rt = RoundTrip(src, 3, 4, out _);
        AssertChunksEqual(src, rt);
        Assert.True(rt.IsCellMultiZ(10));
        Assert.True(rt.StrataData.SequenceEqual(src.StrataData));
    }

    [Fact]
    public void OlderVersion_IsRejected()
    {
        var path = Write1(UniformChunk(), 0, 0);
        try
        {
            var bytes = File.ReadAllBytes(path);
            bytes[4] = 4; bytes[5] = 0; bytes[6] = 0; bytes[7] = 0; // version 4 < MinSupportedVersion 5
            File.WriteAllBytes(path, bytes);
            Assert.Null(StepCacheFile.OpenForLazy(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void SwimAndStrata_Full_RoundTrips_Identically()
    {
        // Exercises the combined trailer ordering: swim-layer trailer THEN strata trailer.
        var src = SwimChunk(multis: 11);
        var offsets = new ushort[StepChunk.CellsPerChunk];
        Array.Fill(offsets, StepChunk.NoStrata);
        offsets[20] = 0;
        var data = new byte[1 + StepChunk.StratumByteLength];
        data[0] = 1;
        src.SetStrata(offsets, data);

        var rt = RoundTrip(src, 9, 9, out _);
        Assert.True(rt.HasSwimLayer);
        Assert.True(rt.IsCellMultiZ(20));
        AssertChunksEqual(src, rt);
        Assert.True(rt.StrataData.SequenceEqual(src.StrataData));
    }
}
