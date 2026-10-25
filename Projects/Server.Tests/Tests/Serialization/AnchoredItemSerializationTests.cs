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
        Item restored = null;

        try
        {
            item.MoveToWorld(new Point3D(121, 100, 0), Map.Felucca);

            // Sub-minute precision that the old minutes encoding would have destroyed.
            var moved = Core.Now - TimeSpan.FromSeconds(90.5) - TimeSpan.FromMilliseconds(123);
            item.LastMoved = moved;

            item.RestartDecay();
            var decayReset = item.DecayResetTime;
            Assert.NotEqual(default(DateTime), decayReset);

            var bytes = SerializeItem(item);

            // Deserialize enters the copy into the map's sectors, so it must be deleted too.
            restored = new Item((Serial)0x7ffff123u);
            restored.Deserialize(new BufferReader(bytes));

            Assert.Equal(moved, restored.LastMoved);
            Assert.Equal(decayReset, restored.DecayResetTime);
        }
        finally
        {
            item.Delete();
            restored?.Delete();
        }
    }

    /// <summary>
    /// LastMoved is the real time of the move, so a load after downtime must not shift it.
    /// Shifting it rewrote every item record on the first save after every restart.
    /// </summary>
    [Fact]
    public void LastMoved_IsNotShiftedByTheLoadAnchor()
    {
        var item = new Item(0x1F13);
        var moved = Core.Now - TimeSpan.FromDays(30);
        item.LastMoved = moved;

        var restored = new Item((Serial)0x7ffff124u);
        restored.Deserialize(new BufferReader(SerializeItem(item)) { AnchoredTimeShift = TimeSpan.FromHours(2) });

        Assert.Equal(moved, restored.LastMoved);
        Assert.Equal(default(DateTime), restored.DecayResetTime);

        item.Delete();
        restored.Delete();
    }

    /// <summary>
    /// Downtime still does not count toward decay: a countdown running when the save started
    /// resumes with the time it had left, carried by DecayResetTime.
    /// </summary>
    [Fact]
    public void Downtime_IsCreditedToARunningDecayCountdown()
    {
        var start = Core._now;

        try
        {
            var item = new Item(0x1F13);
            item.LastMoved = start - TimeSpan.FromMinutes(10);
            var deadline = item.ScheduledDecayTime;

            var downtime = TimeSpan.FromHours(2);
            Core._now = start + downtime;
            item.CreditDowntimeToDecay(start, downtime);

            Assert.Equal(start - TimeSpan.FromMinutes(10), item.LastMoved);
            Assert.Equal(deadline + downtime, item.ScheduledDecayTime);

            item.Delete();
        }
        finally
        {
            Core._now = start;
        }
    }

    /// <summary>
    /// The common case: an item whose countdown had already run out gets no credit, so a restart
    /// leaves its record byte-identical.
    /// </summary>
    [Fact]
    public void ItemAlreadyDueAtSave_GetsNoCredit_AndKeepsItsBytes()
    {
        var start = Core._now;

        try
        {
            var item = new Item(0x1F13);
            item.LastMoved = start - TimeSpan.FromDays(3);
            var before = SerializeItem(item);

            var downtime = TimeSpan.FromMinutes(30);
            Core._now = start + downtime;
            item.CreditDowntimeToDecay(start, downtime);

            Assert.Equal(default(DateTime), item.DecayResetTime);
            Assert.Equal(before, SerializeItem(item));

            item.Delete();
        }
        finally
        {
            Core._now = start;
        }
    }

    /// <summary>
    /// Stat-gain stamps are absolute and not shifted at load either; downtime counts toward a
    /// cooldown that is only minutes long.
    /// </summary>
    [Fact]
    public void StatGainStamps_AreNotShiftedByTheLoadAnchor()
    {
        var mobile = new Mobile(World.NewMobile);
        mobile.DefaultMobileInit();
        mobile.LastStrGain = Core.Now - TimeSpan.FromMinutes(3);
        mobile.LastIntGain = Core.Now - TimeSpan.FromMinutes(2);
        mobile.LastDexGain = Core.Now - TimeSpan.FromMinutes(1);

        var writer = new BufferWriter(new byte[4096], true);
        mobile.Serialize(writer);

        var restored = new Mobile(World.NewMobile);
        restored.Deserialize(new BufferReader(writer.Buffer[..(int)writer.Position]) { AnchoredTimeShift = TimeSpan.FromHours(2) });

        Assert.Equal(mobile.LastStrGain, restored.LastStrGain);
        Assert.Equal(mobile.LastIntGain, restored.LastIntGain);
        Assert.Equal(mobile.LastDexGain, restored.LastDexGain);

        mobile.Delete();
        restored.Delete();
    }
}
