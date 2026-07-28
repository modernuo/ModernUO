using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

/// <summary>
/// Bounds behaviour of the Huffman compressor when the destination is too small.
///
/// This is reachable in production: NetState only checks that the send buffer has *some* writable
/// space before handing the remainder to Compress, so a nearly-full buffer can offer a span of one
/// to three bytes. The internal guard is computed as an unsigned <c>output.Length - 4</c>, which
/// underflows for those sizes and stops bounding the writes at all.
/// </summary>
public class NetworkCompressionBoundsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RefusesOutputTooSmallToBound(int outputSize)
    {
        var input = new byte[64];
        Array.Fill(input, (byte)'A');

        // Sentinel-filled backing array; only the middle window is offered to the compressor, so
        // any write past the span shows up as a modified sentinel rather than silent corruption.
        var backing = new byte[256];
        Array.Fill(backing, (byte)0xCC);

        const int windowStart = 64;
        var output = backing.AsSpan(windowStart, outputSize);

        var written = NetworkCompression.Compress(input, output);

        Assert.Equal(0, written);

        for (var i = 0; i < backing.Length; i++)
        {
            Assert.Equal(0xCC, backing[i]);
        }
    }

    [Fact]
    public void StillCompressesWhenOutputIsLargeEnough()
    {
        var input = new byte[64];
        Array.Fill(input, (byte)'A');

        var output = new byte[256];

        var written = NetworkCompression.Compress(input, output);

        Assert.True(written > 0);
        Assert.True(written <= output.Length);
    }

    [Fact]
    public void ReportsFailureRatherThanOverrunningATightOutput()
    {
        // Large input against a small-but-bounded output: the guard is well-defined here, so this
        // must fail cleanly rather than write past the end.
        var input = new byte[4096];
        Array.Fill(input, (byte)'A');

        var backing = new byte[256];
        Array.Fill(backing, (byte)0xCC);

        const int windowStart = 64;
        const int windowSize = 16;
        var output = backing.AsSpan(windowStart, windowSize);

        NetworkCompression.Compress(input, output);

        for (var i = 0; i < windowStart; i++)
        {
            Assert.Equal(0xCC, backing[i]);
        }

        for (var i = windowStart + windowSize; i < backing.Length; i++)
        {
            Assert.Equal(0xCC, backing[i]);
        }
    }
}
