using System.Collections.Generic;
using Xunit;

namespace Server.Tests;

public class SerializationChunkSourceTests
{
    private class TestEntity : IGenericSerializable
    {
        public byte SerializedThread { get; set; }
        public int SerializedPosition { get; set; }
        public int SerializedLength { get; set; }

        public int PayloadSize { get; init; } = 16;

        public void Serialize(IGenericWriter writer)
        {
            for (var i = 0; i < PayloadSize; i++)
            {
                writer.Write((byte)(i & 0xFF));
            }
        }
    }

    private static List<TestEntity> Drain(SerializationChunkSource source)
    {
        var drained = new List<TestEntity>();
        while (source.TryTake(out var chunk))
        {
            if (chunk.Single != null)
            {
                drained.Add((TestEntity)chunk.Single);
            }
            else
            {
                for (var i = 0; i < chunk.Count; i++)
                {
                    drained.Add((TestEntity)chunk.Buffer[i]);
                }

                source.Return(chunk.Buffer, chunk.Count);
            }
        }

        return drained;
    }

    [Fact]
    public void PartialChunkIsNotVisibleUntilFlush()
    {
        var source = new SerializationChunkSource();
        var entities = new List<TestEntity>();

        for (var i = 0; i < 100; i++)
        {
            var e = new TestEntity();
            entities.Add(e);
            source.Push(e);
        }

        Assert.False(source.TryTake(out _));

        source.Flush();

        var drained = Drain(source);
        Assert.Equal(entities, drained);

        // Flush again should publish nothing
        source.Flush();
        Assert.False(source.TryTake(out _));
    }

    [Fact]
    public void FullChunkPublishesWithoutFlush()
    {
        var source = new SerializationChunkSource();

        for (var i = 0; i < 4096; i++)
        {
            source.Push(new TestEntity());
        }

        Assert.True(source.TryTake(out var chunk));
        Assert.Null(chunk.Single);
        Assert.Equal(4096, chunk.Count);
        source.Return(chunk.Buffer, chunk.Count);

        // Nothing partial left behind
        source.Flush();
        Assert.False(source.TryTake(out _));
    }

    [Fact]
    public void PushSingleDoesNotDisturbPartialChunk()
    {
        var source = new SerializationChunkSource();

        var small1 = new TestEntity();
        var heavy = new TestEntity { SerializedLength = 2 * 1024 * 1024 };
        var small2 = new TestEntity();

        source.Push(small1);
        source.PushSingle(heavy); // published immediately as a single, ahead of the partial chunk
        source.Push(small2);

        Assert.True(source.TryTake(out var chunk));
        Assert.Same(heavy, chunk.Single);
        Assert.Equal(1, chunk.Count);

        Assert.False(source.TryTake(out _));
        source.Flush();

        var drained = Drain(source);
        Assert.Equal([small1, small2], drained);
    }

    [Fact]
    public void ReturnedBuffersAreClearedAndReused()
    {
        var source = new SerializationChunkSource();

        for (var i = 0; i < 4096; i++)
        {
            source.Push(new TestEntity());
        }

        Assert.True(source.TryTake(out var chunk));
        var buffer = chunk.Buffer;
        source.Return(buffer, chunk.Count);

        Assert.All(buffer, Assert.Null);

        // Next fill rents the pooled buffer instead of allocating
        source.Push(new TestEntity());
        source.Flush();
        Assert.True(source.TryTake(out var reused));
        Assert.Same(buffer, reused.Buffer);
    }

    [Fact]
    public void WorkersDrainAllEntitiesAndStampPositions()
    {
        var source = new SerializationChunkSource();
        var workers = new SerializationThreadWorker[2];
        for (var i = 0; i < workers.Length; i++)
        {
            workers[i] = new SerializationThreadWorker(i, source);
            workers[i].AllocateHeap();
        }

        try
        {
            var entities = new List<TestEntity>();
            for (var i = 0; i < 10_000; i++)
            {
                entities.Add(new TestEntity { PayloadSize = 16 + i % 64 });
            }

            foreach (var worker in workers)
            {
                worker.Wake();
            }

            // A large payload published as a dedicated single chunk (like persistence
            // self-payloads), interleaved with the bare entity stream.
            var heavy = new TestEntity { PayloadSize = 512 * 1024 };
            entities.Insert(5000, heavy);

            for (var i = 0; i < entities.Count; i++)
            {
                var e = entities[i];
                if (e == heavy)
                {
                    source.PushSingle(e);
                }
                else
                {
                    source.Push(e);
                }
            }

            // Mirrors World.PauseSerializationThreads
            source.Flush();
            foreach (var worker in workers)
            {
                worker.Sleep();
            }

            long totalEntities = 0;
            long totalBytes = 0;
            foreach (var worker in workers)
            {
                totalEntities += worker.EntitiesSerialized;
                totalBytes += worker.BytesSerialized;
            }

            Assert.Equal(entities.Count, totalEntities);

            long expectedBytes = 0;
            foreach (var e in entities)
            {
                expectedBytes += e.PayloadSize;

                // Every entity serialized exactly once with a consistent span on its worker's heap
                Assert.Equal(e.PayloadSize, e.SerializedLength);
                Assert.InRange(e.SerializedThread, (byte)0, (byte)(workers.Length - 1));

                var heap = workers[e.SerializedThread].GetHeap(e.SerializedPosition, e.SerializedLength);
                Assert.Equal(0, heap[0]);
                Assert.Equal((e.SerializedLength - 1) & 0xFF, heap[^1]);
            }

            Assert.Equal(expectedBytes, totalBytes);
        }
        finally
        {
            foreach (var worker in workers)
            {
                worker.Exit();
            }
        }
    }
}
