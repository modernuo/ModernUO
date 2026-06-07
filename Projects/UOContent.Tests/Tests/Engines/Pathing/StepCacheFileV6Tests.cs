using System;
using System.IO;
using Server.Engines.Pathing.Cache;
using Xunit;

namespace Server.Tests.Pathfinding;

// v6 = predictive-Z residuals on top of the v5 uniform-elision format. Each base directional
// Z array is stored as a masked residual against the cell's own SourceZ; arrays that match
// their prediction are omitted entirely (ZArrayMask bit clear) and synthesized at read.
[Collection("Sequential Pathfinding Tests")]
public class StepCacheFileV6Tests
{
    [Theory]
    [InlineData((sbyte)0, (sbyte)0)]
    [InlineData((sbyte)10, (sbyte)10)]
    [InlineData((sbyte)0, (sbyte)10)]
    [InlineData((sbyte)10, (sbyte)0)]
    [InlineData((sbyte)-20, (sbyte)15)]
    [InlineData(sbyte.MinValue, sbyte.MaxValue)]
    [InlineData(sbyte.MaxValue, sbyte.MinValue)]
    [InlineData(sbyte.MinValue, (sbyte)1)]
    [InlineData((sbyte)127, (sbyte)-1)]
    public void Residual_RoundTrips_Losslessly_ForAllInputs(sbyte z, sbyte predict)
    {
        var residual = StepCacheFile.EncodeResidual(z, predict);
        Assert.Equal(z, StepCacheFile.DecodeZ(predict, residual));
    }

    [Theory]
    [InlineData((byte)0b0000_0001, 0, (sbyte)42, (sbyte)42)] // bit set -> sourceZ
    [InlineData((byte)0b0000_0000, 0, (sbyte)42, (sbyte)0)]  // bit clear -> 0
    [InlineData((byte)0b1000_0000, 7, (sbyte)-13, (sbyte)-13)]
    [InlineData((byte)0b0111_1111, 7, (sbyte)-13, (sbyte)0)]
    public void Predict_UsesSourceZWhenBitSet_ZeroOtherwise(byte maskByte, int bit, sbyte sourceZ, sbyte expected)
    {
        Assert.Equal(expected, StepCacheFile.Predict(maskByte, bit, sourceZ));
    }

    // ---- builders ----

    // A FULL chunk (not uniform: masks/SourceZ vary per cell) whose every directional-Z equals
    // its masked prediction => all 16 base Z arrays must elide. Doubles as the coastline case:
    // per-cell partial walkability with SourceZ != 0, flat where walkable, 0 where not.
    private static StepChunk FlatFullChunk(int multis = 3, sbyte baseZ = 10)
    {
        var c = new StepChunk { BuiltMultisVersion = multis };
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            c.WalkMask[i] = (byte)(i & 0xFF);
            c.WetMask[i] = (byte)(~i & 0xFF);
            c.SourceZ[i] = (sbyte)(baseZ + i % 7 - 3); // varies, mostly != 0
        }
        SetFlatDirectional(c);
        return c;
    }

    // Sets every directional-Z to its masked prediction (walkable/wet -> SourceZ, else 0),
    // i.e. perfectly flat terrain. Such arrays all elide under v6.
    private static void SetFlatDirectional(StepChunk c)
    {
        var walk = new[] { c.WalkZN, c.WalkZNE, c.WalkZE, c.WalkZSE, c.WalkZS, c.WalkZSW, c.WalkZW, c.WalkZNW };
        var swim = new[] { c.SwimZN, c.SwimZNE, c.SwimZE, c.SwimZSE, c.SwimZS, c.SwimZSW, c.SwimZW, c.SwimZNW };
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            for (var b = 0; b < 8; b++)
            {
                walk[b][i] = (sbyte)((c.WalkMask[i] >> b & 1) != 0 ? c.SourceZ[i] : 0);
                swim[b][i] = (sbyte)((c.WetMask[i] >> b & 1) != 0 ? c.SourceZ[i] : 0);
            }
        }
    }

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

    private static StepChunk SwimChunk(int multis = 6)
    {
        var c = VariedChunk(multis);
        c.AllocateSwimLayer();
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            c.SwimSourceZ[i] = (sbyte)(i % 30 - 15);
            c.SwimMask[i] = (byte)((i * 5) & 0xFF);
            c.SwimZN_Layer[i] = (sbyte)(i % 7);
            c.SwimZNW_Layer[i] = (sbyte)-(i % 4);
        }
        return c;
    }

    // ---- round-trip plumbing ----

    private static string Write1(StepChunk c, int cx, int cy)
    {
        var path = Path.Combine(Path.GetTempPath(), $"swbv6_{Guid.NewGuid():N}.swb");
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

    // ---- transform tests (Task 2) ----

    [Fact]
    public void FlatFull_AllArraysElide_RoundTripsAndIsCompact()
    {
        var src = FlatFullChunk();
        var rt = RoundTrip(src, 5, 6, out var fileLen);
        AssertChunksEqual(src, rt);
        // Full record with all 16 Z arrays elided: header(48) + ~783-byte record + index(20).
        // A v5 full record alone is > 5 KB, so a sub-1100-byte file proves elision fired.
        Assert.True(fileLen < 1100, $"expected all base Z arrays to elide; file was {fileLen} bytes");
    }

    [Fact]
    public void SlopedSubset_OnlyVaryingArraysPresent_RoundTrips()
    {
        var flat = FlatFullChunk();
        var flatPath = Write1(flat, 1, 1);
        long flatLen;
        try { flatLen = new FileInfo(flatPath).Length; } finally { File.Delete(flatPath); }

        // Bump WalkZN by +1 on cells walkable to the N (slope in one direction only) -> exactly
        // one base Z array (WalkZN, bit 0) becomes present; the other 15 still elide.
        var sloped = FlatFullChunk();
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            if ((sloped.WalkMask[i] & 1) != 0)
            {
                sloped.WalkZN[i] = (sbyte)(sloped.WalkZN[i] + 1);
            }
        }

        var rt = RoundTrip(sloped, 2, 3, out var slopedLen);
        AssertChunksEqual(sloped, rt);
        Assert.True(slopedLen > flatLen, "one present array should grow the record vs all-flat");
        Assert.True(slopedLen <= flatLen + StepChunk.CellsPerChunk, "only one 256-byte residual array should be added");
    }

    [Fact]
    public void Varied_Full_RoundTrips_Identically()
    {
        var src = VariedChunk(multis: 4);
        AssertChunksEqual(src, RoundTrip(src, 1, 2, out _));
    }

    // ---- shape coverage (Task 3) ----

    [Fact]
    public void Coastline_NonzeroSourceZ_PartialWalkability_AllElide()
    {
        // FlatFullChunk already models a coastline: per-cell partial walk/wet masks, SourceZ != 0,
        // flat where walkable and 0 (baker default) where not. A plain SourceZ residual would emit
        // -SourceZ on every unwalkable direction; the masked predictor must drive ALL arrays to elide.
        var src = FlatFullChunk(multis: 2, baseZ: 25);
        var rt = RoundTrip(src, 7, 7, out var fileLen);
        AssertChunksEqual(src, rt);
        Assert.True(fileLen < 1100, $"masked predictor should elide every array on flat coastline; file was {fileLen} bytes");
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
    public void SwimAndStrata_Full_RoundTrips_Identically()
    {
        // Combined trailer ordering: swim-layer trailer THEN strata trailer, after the residual blocks.
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

    [Fact]
    public void OlderVersion_IsRejected()
    {
        var path = Write1(FlatFullChunk(), 0, 0);
        try
        {
            var bytes = File.ReadAllBytes(path);
            bytes[4] = 5; bytes[5] = 0; bytes[6] = 0; bytes[7] = 0; // version 5 < MinSupportedVersion 6
            File.WriteAllBytes(path, bytes);
            Assert.Null(StepCacheFile.OpenForLazy(path));
        }
        finally { File.Delete(path); }
    }
}
