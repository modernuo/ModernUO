using System;
using Server.Buffers;
using Xunit;

namespace Server.Tests.Tests.Buffers;

public class STArrayPoolTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 16)]
    [InlineData(56, 64)]
    [InlineData(120, 128)]
    [InlineData(65535, 65536)]
    [InlineData(1024 * 1024 * 15, 1024 * 1024 * 16)]
    public void ValidMinimumLengths(int requestedLength, int expectedLength)
    {
        var arr = STArrayPool<byte>.Shared.Rent(requestedLength);
        Assert.Equal(expectedLength, arr.Length);
    }

    [Fact]
    public void NegativeLengthThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
            {
                var arr = STArrayPool<byte>.Shared.Rent(-1);
            }
        );
    }

    [Fact]
    public void CachesOnlyUpToCPUCountPerBucket()
    {
        STArrayPool<byte>.Shared.ResetForTesting();

        var cores = Environment.ProcessorCount;
        var arrays1 = new byte[cores * 8 + 2][]; // 1 for the cache, and 8 * CPU for the stacks
        var weakReferences1 = new WeakReference[cores * 8 + 2];

        var arrays2 = new byte[cores * 8 + 2][]; // 1 for the cache, and 8 * CPU for the stacks
        var weakReferences2 = new WeakReference[cores * 8 + 2];

        for (var i = 0; i < arrays1.Length; i++)
        {
            arrays1[i] = STArrayPool<byte>.Shared.Rent(32);
            weakReferences1[i] = new WeakReference(arrays1[i]);

            arrays2[i] = STArrayPool<byte>.Shared.Rent(64);
            weakReferences2[i] = new WeakReference(arrays1[i]);
        }

        for (var i = 0; i < arrays1.Length; i++)
        {
            STArrayPool<byte>.Shared.Return(arrays1[i]);
            arrays1[i] = null;

            STArrayPool<byte>.Shared.Return(arrays2[i]);
            arrays2[i] = null;
        }

        GC.Collect();
        for (var i = 0; i < weakReferences1.Length; i++)
        {
            // When the last one is returned, the one right before it is dropped.
            Assert.Equal(i != weakReferences1.Length - 2, weakReferences1[i].IsAlive);
            Assert.Equal(i != weakReferences2.Length - 2, weakReferences2[i].IsAlive);
        }
    }
}
