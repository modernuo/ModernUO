using System;
using System.Collections.Generic;
using System.IO;
using Server.Engines.Pathing.Cache;
using Xunit;

namespace Server.Tests.Pathfinding;

/// <summary>
/// The .swb encoding, exercised through Write → OpenForLazy → TryReadChunk. Three transforms stack
/// in a record and each can silently corrupt the ones under it, so every chunk shape here is
/// asserted byte-identical after a round trip:
///
///   predictive-Z — a directional-Z array that matches its prediction is omitted entirely,
///   compression — each record deflates independently, or stores raw when that doesn't shrink it,
///   compact index — the trailer carries no file offsets; the reader sums record lengths instead.
///
/// Several tests assert on file size, because a round trip alone cannot tell you a transform ran:
/// an encoder that elided nothing and compressed nothing would still round-trip perfectly.
/// </summary>
[Collection("Sequential Pathfinding Tests")]
public class StepCacheFileFormatTests
{
    // ---- chunk builders ----

    /// <summary>
    /// Per-cell varying masks and Zs. Nothing about it is uniform or predictable, so it exercises
    /// the Full record with residual arrays present.
    /// </summary>
    private static StepChunk VariedChunk(int seed = 0)
    {
        var c = new StepChunk();
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            c.WalkMask[i] = (byte)((i + seed) & 0xFF);
            c.WetMask[i] = (byte)((i * 7 + seed) & 0xFF);
            c.SourceZ[i] = (sbyte)((i + seed) % 40 - 20);
            c.WalkZN[i] = (sbyte)(c.SourceZ[i] + i % 3);
            c.SwimZS[i] = (sbyte)(c.SourceZ[i] - i % 2);
        }

        return c;
    }

    /// <summary>Every cell identical — the Uniform record, ~28 bytes on disk.</summary>
    private static StepChunk UniformChunk(sbyte z = 10)
    {
        var c = new StepChunk();
        Array.Fill(c.WalkMask, (byte)0xC1);
        Array.Fill(c.SourceZ, z);
        foreach (var arr in AllBaseZArrays(c))
        {
            Array.Fill(arr, z);
        }

        return c;
    }

    /// <summary>
    /// Flat terrain, but NOT uniform: masks and SourceZ vary per cell while every directional Z
    /// equals its masked prediction. That is the exact shape predictive-Z is built for, so all 16
    /// arrays must elide. It doubles as the coastline case — partial walkability, non-zero SourceZ,
    /// and 0 in every blocked direction, which is where a naive (unmasked) predictor would emit a
    /// -SourceZ residual on every blocked direction and elide nothing.
    /// </summary>
    private static StepChunk FlatFullChunk(sbyte baseZ = 10)
    {
        var c = new StepChunk();
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            c.WalkMask[i] = (byte)(i & 0xFF);
            c.WetMask[i] = (byte)(~i & 0xFF);
            c.SourceZ[i] = (sbyte)(baseZ + i % 7 - 3);
        }

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

        return c;
    }

    private static StepChunk WithSwimLayer(StepChunk c)
    {
        c.AllocateSwimLayer();
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            c.SwimSourceZ[i] = (sbyte)(i % 30 - 15);
            c.SwimMask[i] = (byte)(i * 5 & 0xFF);
            c.SwimZN_Layer[i] = (sbyte)(i % 7);
            c.SwimZNW_Layer[i] = (sbyte)-(i % 4);
        }

        return c;
    }

    private static StepChunk WithStrataAt(StepChunk c, int cell)
    {
        var offsets = new ushort[StepChunk.CellsPerChunk];
        Array.Fill(offsets, StepChunk.NoStrata);
        offsets[cell] = 0;

        var data = new byte[1 + StepChunk.StratumByteLength];
        data[0] = 1;
        c.SetStrata(offsets, data);

        return c;
    }

    private static sbyte[][] AllBaseZArrays(StepChunk c) =>
    [
        c.WalkZN, c.WalkZNE, c.WalkZE, c.WalkZSE, c.WalkZS, c.WalkZSW, c.WalkZW, c.WalkZNW,
        c.SwimZN, c.SwimZNE, c.SwimZE, c.SwimZSE, c.SwimZS, c.SwimZSW, c.SwimZW, c.SwimZNW
    ];

    private static sbyte[][] AllSwimLayerArrays(StepChunk c) =>
    [
        c.SwimZN_Layer, c.SwimZNE_Layer, c.SwimZE_Layer, c.SwimZSE_Layer,
        c.SwimZS_Layer, c.SwimZSW_Layer, c.SwimZW_Layer, c.SwimZNW_Layer
    ];

    // ---- round-trip plumbing ----

    private static string Write(params (int cx, int cy, StepChunk c)[] chunks)
    {
        var path = Path.Combine(Path.GetTempPath(), $"swb_{Guid.NewGuid():N}.swb");
        StepCacheFile.Write(path, 1u, chunks);
        return path;
    }

    private static StepChunk RoundTrip(StepChunk src, out long fileLength, int cx = 3, int cy = 4)
    {
        var path = Write((cx, cy, src));
        try
        {
            fileLength = new FileInfo(path).Length;

            using var reader = StepCacheFile.OpenForLazy(path);
            Assert.NotNull(reader);

            var rt = reader!.TryReadChunk(cx, cy);
            Assert.NotNull(rt);
            return rt!;
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static StepChunk RoundTrip(StepChunk src) => RoundTrip(src, out _);

    private static void AssertIdentical(StepChunk expected, StepChunk actual)
    {
        Assert.True(expected.WalkMask.AsSpan().SequenceEqual(actual.WalkMask), "WalkMask differs");
        Assert.True(expected.WetMask.AsSpan().SequenceEqual(actual.WetMask), "WetMask differs");
        Assert.True(expected.SourceZ.AsSpan().SequenceEqual(actual.SourceZ), "SourceZ differs");

        var ez = AllBaseZArrays(expected);
        var az = AllBaseZArrays(actual);
        for (var i = 0; i < ez.Length; i++)
        {
            Assert.True(ez[i].AsSpan().SequenceEqual(az[i]), $"base Z array {i} differs");
        }

        Assert.Equal(expected.HasSwimLayer, actual.HasSwimLayer);
        if (expected.HasSwimLayer)
        {
            Assert.True(expected.SwimSourceZ.AsSpan().SequenceEqual(actual.SwimSourceZ), "SwimSourceZ differs");
            Assert.True(expected.SwimMask.AsSpan().SequenceEqual(actual.SwimMask), "SwimMask differs");

            var el = AllSwimLayerArrays(expected);
            var al = AllSwimLayerArrays(actual);
            for (var i = 0; i < el.Length; i++)
            {
                Assert.True(el[i].AsSpan().SequenceEqual(al[i]), $"swim-layer Z array {i} differs");
            }
        }

        Assert.True(expected.StrataData.SequenceEqual(actual.StrataData), "StrataData differs");
    }

    // ---- predictive-Z transform ----

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
    public void Residual_RoundTripsLosslessly_AcrossTheFullSByteRange(sbyte z, sbyte predict)
    {
        var residual = StepCacheFile.EncodeResidual(z, predict);
        Assert.Equal(z, StepCacheFile.DecodeZ(predict, residual));
    }

    [Theory]
    [InlineData((byte)0b0000_0001, 0, (sbyte)42, (sbyte)42)] // passable -> predict SourceZ
    [InlineData((byte)0b0000_0000, 0, (sbyte)42, (sbyte)0)]  // blocked  -> predict 0
    [InlineData((byte)0b1000_0000, 7, (sbyte)-13, (sbyte)-13)]
    [InlineData((byte)0b0111_1111, 7, (sbyte)-13, (sbyte)0)]
    public void Predict_IsSourceZWherePassable_ZeroWhereBlocked(byte maskByte, int bit, sbyte sourceZ, sbyte expected)
    {
        Assert.Equal(expected, StepCacheFile.Predict(maskByte, bit, sourceZ));
    }

    [Fact]
    public void FlatChunk_ElidesEveryZArray()
    {
        var src = FlatFullChunk();
        var rt = RoundTrip(src, out var fileLength);

        AssertIdentical(src, rt);

        // A Full record carrying all 16 Z arrays runs past 5 KB. Landing under 1100 bytes is only
        // possible if every one of them elided.
        Assert.True(fileLength < 1100, $"expected every base Z array to elide; file was {fileLength} bytes");
    }

    [Fact]
    public void CoastlineChunk_ElidesEveryZArray()
    {
        // Partial walkability with a non-zero SourceZ: the shape that defeats an unmasked predictor.
        var src = FlatFullChunk(baseZ: 25);
        var rt = RoundTrip(src, out var fileLength);

        AssertIdentical(src, rt);
        Assert.True(fileLength < 1100, $"masked predictor should elide every array; file was {fileLength} bytes");
    }

    [Fact]
    public void SlopeInOneDirection_StoresOnlyThatZArray()
    {
        var flat = FlatFullChunk();
        RoundTrip(flat, out var flatLength);

        // Raise WalkZN on cells walkable to the north. Exactly one array (WalkZN) now disagrees
        // with its prediction; the other 15 must still elide.
        var sloped = FlatFullChunk();
        for (var i = 0; i < StepChunk.CellsPerChunk; i++)
        {
            if ((sloped.WalkMask[i] & 1) != 0)
            {
                sloped.WalkZN[i]++;
            }
        }

        var rt = RoundTrip(sloped, out var slopedLength);

        AssertIdentical(sloped, rt);
        Assert.True(slopedLength > flatLength, "a present Z array should grow the record");
        Assert.True(
            slopedLength <= flatLength + StepChunk.CellsPerChunk,
            $"only one 256-byte residual array should have been added; grew by {slopedLength - flatLength}"
        );
    }

    // ---- compression ----

    [Fact]
    public void VariedChunk_Compresses_AndRoundTrips()
    {
        var src = VariedChunk(seed: 4);
        var rt = RoundTrip(src, out var fileLength);

        AssertIdentical(src, rt);

        // The uncompressed Full record for a varied chunk exceeds 5 KB.
        Assert.True(fileLength < 4000, $"expected compression to shrink the record; file was {fileLength} bytes");
    }

    [Fact]
    public void UniformChunk_StoredRaw_RoundTrips()
    {
        // A Uniform body is ~28 bytes and deflate cannot shrink it, so the writer stores it raw and
        // the reader has to notice that from the payload length alone.
        var src = UniformChunk(z: 12);
        var rt = RoundTrip(src, out var fileLength);

        AssertIdentical(src, rt);
        Assert.True(fileLength < 200, $"uniform record should stay tiny; file was {fileLength} bytes");
    }

    // ---- optional trailers ----

    [Fact]
    public void SwimLayer_RoundTrips() => AssertIdentical(
        WithSwimLayer(VariedChunk()),
        RoundTrip(WithSwimLayer(VariedChunk()))
    );

    [Fact]
    public void Strata_RoundTrips()
    {
        var src = WithStrataAt(VariedChunk(), cell: 10);
        var rt = RoundTrip(src);

        AssertIdentical(src, rt);
        Assert.True(rt.IsCellMultiZ(10));
    }

    [Fact]
    public void SwimLayerAndStrata_RoundTripTogether()
    {
        // Both trailers present at once, which is the only case that pins their relative order.
        var src = WithStrataAt(WithSwimLayer(VariedChunk()), cell: 20);
        var rt = RoundTrip(src);

        AssertIdentical(src, rt);
        Assert.True(rt.HasSwimLayer);
        Assert.True(rt.IsCellMultiZ(20));
    }

    // ---- compact index ----

    [Fact]
    public void MultipleChunks_ResolveIndividually_FromDerivedOffsets()
    {
        // The index stores no offsets, so a reader that mis-sums record lengths would hand back a
        // neighbouring chunk's bytes. Distinct content per coordinate is what catches that. The mix
        // of record sizes matters: a raw-stored Uniform sits between two compressed Full records,
        // and one coordinate is large enough to exercise the packed key's high 16 bits.
        var chunks = new List<(int cx, int cy, StepChunk c)>
        {
            (1, 1, VariedChunk(seed: 3)),
            (2, 5, UniformChunk(z: 14)),
            (10, 3, VariedChunk(seed: 99)),
            (300, 200, VariedChunk(seed: 17))
        };

        var path = Write(chunks.ToArray());
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
                AssertIdentical(src, rt!);
            }

            Assert.Null(reader.TryReadChunk(7, 7)); // never written
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void EmptyChunkSet_WritesAReadableFile()
    {
        var path = Write();
        try
        {
            using var reader = StepCacheFile.OpenForLazy(path);
            Assert.NotNull(reader);
            Assert.Equal(0u, reader!.ChunkCount);
            Assert.Null(reader.TryReadChunk(0, 0));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---- version gate ----

    [Theory]
    [InlineData(0u)]
    [InlineData(5u)]
    [InlineData(8u)]
    [InlineData(StepCacheFile.FormatVersion + 1)]
    public void UnsupportedVersion_IsRejected(uint version)
    {
        var path = Write((0, 0, UniformChunk()));
        try
        {
            var bytes = File.ReadAllBytes(path);
            BitConverter.GetBytes(version).CopyTo(bytes, 4); // Version sits right after Magic
            File.WriteAllBytes(path, bytes);

            Assert.Null(StepCacheFile.OpenForLazy(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
