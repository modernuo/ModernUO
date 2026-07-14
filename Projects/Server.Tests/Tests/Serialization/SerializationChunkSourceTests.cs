using System;
using System.Collections.Generic;
using Xunit;

namespace Server.Tests;

public class SerializationChunkSourceTests
{
    private class TestEntity : IGenericSerializable
    {
        public int PayloadSize { get; init; } = 16;

        public void Serialize(IGenericWriter writer)
        {
            for (var i = 0; i < PayloadSize; i++)
            {
                writer.Write((byte)(i & 0xFF));
            }
        }
    }

    private class TestPersistence : GenericPersistence
    {
        public int PayloadSize { get; init; } = 16;

        public TestPersistence() : base("ChunkSourceTest", 100)
        {
        }

        public (byte Thread, int Position, int Length) Placement => (_selfThread, _selfPosition, _selfLength);

        public override void Serialize(IGenericWriter writer)
        {
            for (var i = 0; i < PayloadSize; i++)
            {
                writer.Write((byte)(i & 0xFF));
            }
        }

        public override void Deserialize(IGenericReader reader)
        {
        }
    }

    private static List<TestEntity> Drain(SerializationChunkSource source)
    {
        var drained = new List<TestEntity>();
        while (source.TryTake(out var chunk))
        {
            for (var i = 0; i < chunk.Count; i++)
            {
                drained.Add((TestEntity)chunk.Buffer[i]);
            }

            source.Return(chunk.Buffer, chunk.Count);
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
        var heavy = new TestPersistence();

        try
        {
            var small1 = new TestEntity();
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
        finally
        {
            heavy.Unregister();
        }
    }

    [Fact]
    public void SetOwnerPublishesPartialChunkOnChange()
    {
        var source = new SerializationChunkSource();
        var ownerA = new TestPersistence();
        var ownerB = new TestPersistence();

        try
        {
            source.SetOwner(ownerA);
            source.Push(new TestEntity());
            source.Push(new TestEntity());

            // Same owner: partial chunk stays private to the producer.
            source.SetOwner(ownerA);
            Assert.False(source.TryTake(out _));

            // Owner change publishes the partial chunk, keeping chunks persistence-homogeneous.
            source.SetOwner(ownerB);
            Assert.True(source.TryTake(out var chunk));
            Assert.Same(ownerA, chunk.Owner);
            Assert.Equal(2, chunk.Count);
            source.Return(chunk.Buffer, chunk.Count);

            source.Push(new TestEntity());
            source.Flush();
            Assert.True(source.TryTake(out chunk));
            Assert.Same(ownerB, chunk.Owner);
        }
        finally
        {
            ownerA.Unregister();
            ownerB.Unregister();
        }
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
    public void WorkersDrainAllEntitiesAndLogSegments()
    {
        var source = new SerializationChunkSource();
        var owner = new TestPersistence { PayloadSize = 512 * 1024 };
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

            // A large payload published as a dedicated single chunk (a persistence
            // self-payload), interleaved with the bare entity stream.
            source.SetOwner(owner);
            for (var i = 0; i < entities.Count; i++)
            {
                if (i == 5000)
                {
                    source.PushSingle(owner);
                }

                source.Push(entities[i]);
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

            Assert.Equal(entities.Count + 1, totalEntities);

            // The self-payload recorded its placement on the persistence itself.
            var (selfThread, selfPosition, selfLength) = owner.Placement;
            Assert.Equal(owner.PayloadSize, selfLength);
            Assert.InRange(selfThread, (byte)0, (byte)(workers.Length - 1));
            var selfHeap = workers[selfThread].GetHeap(selfPosition, selfLength);
            Assert.Equal(0, selfHeap[0]);
            Assert.Equal((selfLength - 1) & 0xFF, selfHeap[^1]);

            // Every entity appears exactly once in the worker segment logs, with a
            // consistent span on that worker's heap: positions are implicit (contiguous
            // writes), identity comes from the buffer-entities log.
            var seen = new HashSet<TestEntity>();
            long expectedBytes = owner.PayloadSize;
            foreach (var e in entities)
            {
                expectedBytes += e.PayloadSize;
            }

            foreach (var worker in workers)
            {
                var lengths = worker.Lengths;
                var bufferEntities = worker.BufferEntities;

                foreach (var segment in worker.Segments)
                {
                    Assert.Same(owner, segment.Owner);
                    Assert.Equal(-1, segment.SlotOffset);

                    var heapPos = (int)segment.HeapStart;
                    for (var i = 0; i < segment.RecordCount; i++)
                    {
                        var entity = (TestEntity)bufferEntities[segment.EntitiesStart + i];
                        var length = lengths[segment.LengthsStart + i];

                        Assert.True(seen.Add(entity));
                        Assert.Equal(entity.PayloadSize, length);

                        var heap = workers[Array.IndexOf(workers, worker)].GetHeap(heapPos, length);
                        Assert.Equal(0, heap[0]);
                        Assert.Equal((length - 1) & 0xFF, heap[^1]);

                        heapPos += length;
                    }
                }
            }

            Assert.Equal(entities.Count, seen.Count);
            Assert.Equal(expectedBytes, totalBytes);
        }
        finally
        {
            owner.Unregister();
            foreach (var worker in workers)
            {
                worker.Exit();
            }
        }
    }
}
