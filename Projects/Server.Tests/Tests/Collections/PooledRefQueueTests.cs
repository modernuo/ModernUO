using System;
using Server.Collections;
using Server.Random;
using Xunit;

namespace Server.Tests;

public sealed class PooledRefQueueTests : IDisposable
{
    private class MockedRandomSource : IRandomSource
    {
        private int _queueCount;
        private int _mockedValue;

        public MockedRandomSource(int queueCount, int mockedValue)
        {
            _queueCount = queueCount;
            _mockedValue = mockedValue;
        }

        public int Next() => throw new NotImplementedException();

        public int Next(int maxValue) => _mockedValue;

        public int Next(int minValue, int count) => throw new NotImplementedException();

        public uint Next(uint maxValue) => throw new NotImplementedException();

        public uint Next(uint minValue, uint count) => throw new NotImplementedException();

        public long Next(long maxValue) => throw new NotImplementedException();

        public long Next(long minValue, long count) => throw new NotImplementedException();

        public double NextDouble() => throw new NotImplementedException();

        public void NextBytes(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        public int NextInt() => throw new NotImplementedException();

        public uint NextUInt() => throw new NotImplementedException();

        public ulong NextULong() => throw new NotImplementedException();

        public bool NextBool() => throw new NotImplementedException();

        public byte NextByte() => throw new NotImplementedException();

        public float NextFloat() => throw new NotImplementedException();

        public float NextFloatNonZero() => throw new NotImplementedException();

        public double NextDoubleNonZero() => throw new NotImplementedException();

        public double NextDoubleHighRes() => throw new NotImplementedException();
    }

    public void Dispose() => RandomSources.SetRng(null);

    private static void PrepareRng(int queueCount, int rngValue)
    {
        var mockedRng = new MockedRandomSource(queueCount, rngValue);
        RandomSources.SetRng(mockedRng);
    }

    [Fact]
    public void TestPeekRandom1()
    {
        // Random value for _head = 0, _tail = 5, _size = 5,
        using var queue = PooledRefQueue<int>.Create(10);
        queue.Enqueue(0);
        queue.Enqueue(1);
        queue.Enqueue(2);
        queue.Enqueue(3); // <-----
        queue.Enqueue(4);
        queue.Enqueue(5);

        PrepareRng(6, 3);
        Assert.Equal(3, queue.PeekRandom());
    }

    [Fact]
    public void TestPeekRandom2()
    {
        // Random value for _head = 3, _tail = 10, _size = 7,
        using var queue = PooledRefQueue<int>.Create(10);
        queue.Enqueue(0);
        queue.Enqueue(1);
        queue.Enqueue(2);

        queue.Enqueue(3);
        queue.Enqueue(4);
        queue.Enqueue(5);
        queue.Enqueue(6); // <---
        queue.Enqueue(7);
        queue.Enqueue(8);
        queue.Enqueue(9);

        queue.Dequeue();
        queue.Dequeue();
        queue.Dequeue();

        PrepareRng(7, 3);
        Assert.Equal(6, queue.PeekRandom());
    }

    [Theory]
    [InlineData(3, 6)]
    [InlineData(8, 11)]
    [InlineData(6, 9)]
    [InlineData(7, 10)]
    public void TestPeekRandom3(int rngValue, int expectedIndex)
    {
        // Random value for _head = 3, _tail = 2, _size = 10,
        using var queue = PooledRefQueue<int>.Create(10);
        queue.Enqueue(0);
        queue.Enqueue(1);
        queue.Enqueue(2);

        queue.Enqueue(3);
        queue.Enqueue(4);
        queue.Enqueue(5);
        queue.Enqueue(6);
        queue.Enqueue(7);
        queue.Enqueue(8);
        queue.Enqueue(9);

        queue.Dequeue();
        queue.Dequeue();
        queue.Dequeue();

        queue.Enqueue(10);
        queue.Enqueue(11);
        queue.Enqueue(12);

        PrepareRng(10, rngValue);
        Assert.Equal(expectedIndex, queue.PeekRandom());
    }
}
