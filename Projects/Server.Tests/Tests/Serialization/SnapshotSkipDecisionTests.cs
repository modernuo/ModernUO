using System.Collections.Generic;
using System.IO;
using Server.Items;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class SnapshotSkipDecisionTests
{
    private class SkippingContainer : Container
    {
        public SkippingContainer() : base(0xE75)
        {
        }

        public override bool SkipsChildSerialization => true;
    }

    private class SnapshotSkipItem : Item
    {
        public SnapshotSkipItem() : base(0x1234)
        {
        }
    }

    // T is a test-only type so the persistence's find-entity registration does not replace World's for Item.
    private class SnapshotSkipPersistence : GenericEntityPersistence<SnapshotSkipItem>
    {
        public SnapshotSkipPersistence(int priority) : base("SnapshotSkip", priority, 1, 0x7FFFFFFF)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
        }

        public override void Deserialize(IGenericReader reader)
        {
        }
    }

    /// <summary>
    /// The snapshot writer runs after the world unfreezes. An item moved into or out of a
    /// child-skipping container in that window must be saved as it was at the freeze, matching
    /// its parent's frozen child list.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SkipDecision_IsTakenAtTheFreeze_NotWhenTheSnapshotIsWritten(bool slotRanges)
    {
        using var pipeline = new PersistencePipelineScope(2, installWorkers: true, directoryPrefix: "muo-skipdecision");
        var source = pipeline.Source;

        var persistence = pipeline.Track(new SnapshotSkipPersistence(2010));
        persistence.RegisterType(typeof(SnapshotSkipItem));

        var skipping = new SkippingContainer();
        var normal = new Container(0xE75);
        var leaving = new SnapshotSkipItem();
        var entering = new SnapshotSkipItem();

        try
        {
            skipping.DropItem(leaving);
            normal.DropItem(entering);

            persistence.EntitiesBySerial[leaving.Serial] = leaving;
            persistence.EntitiesBySerial[entering.Serial] = entering;

            pipeline.WakeWorkers();
            source.SetOwner(persistence);

            if (slotRanges)
            {
                Assert.True(persistence.TrySnapshotEntries(out var slotCount));
                source.PushSlotRanges(persistence, slotCount);
            }
            else
            {
                source.Push(leaving);
                source.Push(entering);
            }

            pipeline.FlushAndSleepWorkers();

            // Unfrozen: the loop moves both items before the writer reaches them.
            normal.DropItem(leaving);
            skipping.DropItem(entering);

            persistence.WriteSnapshot(pipeline.Directory);
            persistence.PostWorldSave();

            var (serials, totalLength) = ReadIndex(Path.Combine(pipeline.Directory, "SnapshotSkip", "SnapshotSkip.idx"));

            Assert.Equal([entering.Serial], serials);

            var binLength = new FileInfo(Path.Combine(pipeline.Directory, "SnapshotSkip", "SnapshotSkip.bin")).Length;
            Assert.Equal(totalLength, binLength);
        }
        finally
        {
            skipping.Delete();
            normal.Delete();
        }
    }

    private static (List<Serial> Serials, long TotalLength) ReadIndex(string path)
    {
        var reader = new BufferReader(File.ReadAllBytes(path));

        Assert.Equal(5, reader.ReadInt());
        reader.ReadLong(); // save-start anchor

        var typeCount = reader.ReadInt();
        for (var i = 0; i < typeCount; i++)
        {
            reader.ReadStringRaw();
        }

        var count = reader.ReadInt();
        var serials = new List<Serial>(count);
        var totalLength = 0L;

        for (var i = 0; i < count; i++)
        {
            reader.ReadUShort(); // type index
            serials.Add((Serial)reader.ReadUInt());
            reader.ReadLong(); // created
            var position = reader.ReadLong();
            var length = reader.ReadInt();

            Assert.Equal(totalLength, position);
            totalLength += length;
        }

        return (serials, totalLength);
    }
}
