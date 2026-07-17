using System;
using System.Collections.Generic;
using Xunit;

namespace Server.Tests;

// Constructing a persistence mutates the static registry (an unsynchronized SortedSet);
// tests that do so must share the sequential collection.
[Collection("Sequential Server Tests")]
public class ShadowDictionaryEntriesTests
{
    private class TestEntity : ISerializable
    {
        public TestEntity(Serial serial) => Serial = serial;

        public Serial Serial { get; }
        public DateTime Created { get; set; }
        public bool Deleted => false;

        public void Delete()
        {
        }

        public void Serialize(IGenericWriter writer)
        {
            writer.Write(Serial);
            writer.Write(0xC0FFEE);
        }

        public void Deserialize(IGenericReader reader)
        {
        }
    }

    private static TestEntity GetSlotValue(Array entries, int slot) =>
        System.Runtime.CompilerServices.Unsafe.As<ShadowEntry<TestEntity>[]>(entries)[slot].Value;

    [Fact]
    public void RuntimeLayoutIsSupported()
    {
        // If this fails on a runtime upgrade, saves still work via the fallback path,
        // but the parallel iteration fast path is silently lost — this test makes it loud.
        Assert.True(ShadowDictionaryEntries.Supported);
    }

    [Fact]
    public void SerializeRangeCoversExactlyTheLiveEntities()
    {
        var persistence = new GenericEntityPersistence<TestEntity>("ShadowTest", 1000, 1, 0x7FFFFFFF);

        try
        {
            var rng = new System.Random(0xBEEF);
            var dict = persistence.EntitiesBySerial;

            // Heavy churn: adds, removes, and re-adds to exercise freelist reuse and resizes,
            // leaving free slots scattered through the entries array.
            var serials = new List<Serial>();
            for (var i = 0; i < 50_000; i++)
            {
                var serial = (Serial)(uint)rng.Next(1, int.MaxValue);
                if (dict.TryAdd(serial, new TestEntity(serial)))
                {
                    serials.Add(serial);
                }

                if (i % 4 == 3)
                {
                    var index = rng.Next(serials.Count);
                    dict.Remove(serials[index]);
                    serials.RemoveAt(index);
                }
            }

            Assert.True(persistence.TrySnapshotEntries(out var slotCount));
            Assert.True(slotCount >= dict.Count);

            var source = (ISlotRangeSource)persistence;
            var writer = new BufferWriter(new byte[dict.Count * 16], true);
            var lengths = new List<int>();

            // Serialize in worker-sized slices, like the drain does.
            var serialized = 0;
            for (var offset = 0; offset < slotCount; offset += 4096)
            {
                serialized += source.SerializeRange(writer, lengths, offset, Math.Min(4096, slotCount - offset));
            }

            Assert.Equal(dict.Count, serialized);
            Assert.Equal(dict.Count, lengths.Count);

            // Re-walk the same slots in the same order, pairing each occupied slot with the
            // next logged length — exactly how the snapshot writer locates each record.
            var entriesField = ShadowDictionaryEntries.GetEntriesField<TestEntity>();
            var entries = (Array)entriesField.GetValue(dict);

            var position = 0;
            var lengthIndex = 0;
            var matched = 0;

            for (var slot = 0; slot < entries.Length; slot++)
            {
                // Occupancy is exactly the non-null values, same as production.
                var entity = GetSlotValue(entries, slot);
                if (entity == null)
                {
                    continue;
                }

                var length = lengths[lengthIndex++];
                Assert.Equal(8, length); // serial + int

                var span = writer.Buffer.AsSpan(position, length);
                Assert.Equal(entity.Serial, (Serial)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(span));

                position += length;
                matched++;
            }

            Assert.Equal(dict.Count, matched);
        }
        finally
        {
            persistence.Unregister();
        }
    }

    [Fact]
    public void SnapshotFailsGracefullyOnEmptyDictionary()
    {
        var persistence = new GenericEntityPersistence<TestEntity>("ShadowTestEmpty", 1001, 1, 0x7FFFFFFF);

        try
        {
            Assert.False(persistence.TrySnapshotEntries(out var slotCount));
            Assert.Equal(0, slotCount);
        }
        finally
        {
            persistence.Unregister();
        }
    }
}
