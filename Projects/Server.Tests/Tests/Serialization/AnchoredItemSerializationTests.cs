using System;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class AnchoredItemSerializationTests
{
    private static byte[] SerializeItem(Item item)
    {
        var writer = new BufferWriter(new byte[256], true);
        item.Serialize(writer);
        return writer.Buffer[..(int)writer.Position];
    }

    /// <summary>
    /// Item v11 stores LastMoved and DecayResetTime as anchored time: the serialized bytes
    /// are a function of item state only, not of when the save runs. Pre-v11 stored
    /// minutes-since-moved and delta time, which rewrote the bytes on every save.
    /// </summary>
    [Fact]
    public void ItemBytes_AreStable_AcrossSavesAtDifferentTimes()
    {
        var start = Core._now;

        try
        {
            var item = new Item(0x1F13);
            item.MoveToWorld(new Point3D(120, 100, 0), Map.Felucca);
            item.RestartDecay();

            var first = SerializeItem(item);

            // A save hours later, with no state change, must produce identical bytes.
            Core._now = start + TimeSpan.FromHours(5);
            var second = SerializeItem(item);

            Assert.Equal(first, second);

            item.Delete();
        }
        finally
        {
            Core._now = start;
        }
    }

    /// <summary>
    /// Pre-v11 LastMoved was stored at whole-minute precision relative to the save time and
    /// could never round-trip exactly. Anchored storage is absolute and exact.
    /// </summary>
    [Fact]
    public void LastMovedAndDecayReset_RoundTripExactly()
    {
        var item = new Item(0x1F13);
        item.MoveToWorld(new Point3D(121, 100, 0), Map.Felucca);

        // Sub-minute precision that the old minutes encoding would have destroyed.
        var moved = Core.Now - TimeSpan.FromSeconds(90.5) - TimeSpan.FromMilliseconds(123);
        item.LastMoved = moved;

        item.RestartDecay();
        var decayReset = item.DecayResetTime;
        Assert.NotEqual(default(DateTime), decayReset);

        var bytes = SerializeItem(item);

        var restored = new Item((Serial)0x7ffff123u);
        restored.Deserialize(new BufferReader(bytes));

        Assert.Equal(moved, restored.LastMoved);
        Assert.Equal(decayReset, restored.DecayResetTime);

        item.Delete();
    }
}
