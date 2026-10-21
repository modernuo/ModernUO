using System;
using Server.Items;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class DecayRegistrationTests : IDisposable
{
    private readonly EntityCleanup _cleanup = new();

    // The wheel is static and these tests move the clock, so start each from a known schedule.
    public DecayRegistrationTests() => DecayScheduler.ResetForTests();

    public void Dispose() => _cleanup.Dispose();

    // Stands in for a region that refuses decay, e.g. HouseRegion over a locked down item.
    private class RefusesDecayItem : Item
    {
        public RefusesDecayItem() : base(0x1234)
        {
        }

        public override bool OnDecay() => false;
    }

    [Fact]
    public void ItemOnGround_ActuallyDecaysAfterDecayTime()
    {
        using var clock = new SimulatedClockScope();

        var item = _cleanup.Track(new Item(0x1234));
        item.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);

        Assert.True(DecayScheduler.IsRegistered(item));

        clock.AdvanceDecay(item.DecayTime + TimeSpan.FromMinutes(2), item);

        Assert.True(item.Deleted, "Item on the ground must decay once DecayTime elapses.");
    }

    [Fact]
    public void ItemOnGround_DoesNotDecayBeforeDecayTime()
    {
        using var clock = new SimulatedClockScope();

        var item = _cleanup.Track(new Item(0x1234));
        item.MoveToWorld(new Point3D(102, 100, 0), Map.Felucca);

        clock.AdvanceDecay(item.DecayTime - TimeSpan.FromMinutes(2), item);

        Assert.False(item.Deleted, "Item must not decay before DecayTime elapses.");

        item.Delete();
    }

    // The real client path: Mobile.Lift internalizes, then Mobile.Drop calls MoveToWorld.
    // Held on the cursor means Map.Internal, which must not be tracked.
    // A new item is on Map.Internal, so construction must not touch the scheduler.
    // Unregister() trusts DecaySlot; IsRegistered() scans the structures. Assert both to catch drift.
    [Fact]
    public void DecaySlot_AgreesWithStructures_AcrossLifecycle()
    {
        var item = _cleanup.Track(new Item(0x1234));

        item.MoveToWorld(new Point3D(103, 100, 0), Map.Felucca);
        Assert.NotEqual(DecayScheduler.SlotNone, item.DecaySlot);
        Assert.True(DecayScheduler.IsRegistered(item));

        // Lifted to the cursor: untracked, and no stale slot left behind.
        item.Internalize();
        Assert.Equal(DecayScheduler.SlotNone, item.DecaySlot);
        Assert.False(DecayScheduler.IsRegistered(item));

        // Back to the ground: tracked again.
        item.MoveToWorld(new Point3D(104, 100, 0), Map.Felucca);
        Assert.NotEqual(DecayScheduler.SlotNone, item.DecaySlot);
        Assert.True(DecayScheduler.IsRegistered(item));

        item.Delete();
        Assert.False(DecayScheduler.IsRegistered(item), "A deleted item must leave no trace in the scheduler.");
    }

    // Contents spilled onto the ground must start decaying.
    [Fact]
    public void ContainerDestroy_ContentsDroppedToGroundAreTracked()
    {
        var pack = _cleanup.Track(new Container(0xE75));
        pack.MoveToWorld(new Point3D(105, 100, 0), Map.Felucca);

        var item = _cleanup.Track(new Item(0x1234));
        pack.AddItem(item);
        Assert.False(DecayScheduler.IsRegistered(item));

        pack.Destroy();

        Assert.True(item.CanDecay());
        Assert.True(DecayScheduler.IsRegistered(item), "Contents spilled by Container.Destroy must be tracked.");

        item.Delete();
    }

    // Unfreezing an item with a stale LastMoved must grant a fresh decay window,
    // not delete it on the next tick.
    [Fact]
    public void StaleImmovableItemMadeMovable_GetsAFreshDecayWindow()
    {
        using var clock = new SimulatedClockScope();

        var item = _cleanup.Track(new Item(0x1234));
        item.MoveToWorld(new Point3D(107, 100, 0), Map.Felucca);

        item.Movable = false;
        Assert.False(DecayScheduler.IsRegistered(item), "A frozen item must not be tracked for decay.");

        clock.Now = clock.Start + TimeSpan.FromDays(30);

        item.Movable = true;

        Assert.True(DecayScheduler.IsRegistered(item), "An unfrozen item must be tracked for decay.");

        clock.AdvanceDecay(item.DecayTime - TimeSpan.FromMinutes(2), item);
        Assert.False(item.Deleted, "An unfrozen item must get a full decay window, not vanish immediately.");

        clock.AdvanceDecay(TimeSpan.FromMinutes(4), item);
        Assert.True(item.Deleted, "An unfrozen item must still decay once the fresh window elapses.");
    }

    // Same transition through the Visible setter: unhiding a long-hidden item.
    [Fact]
    public void StaleHiddenItemMadeVisible_GetsAFreshDecayWindow()
    {
        using var clock = new SimulatedClockScope();

        var item = _cleanup.Track(new Item(0x1234));
        item.MoveToWorld(new Point3D(109, 100, 0), Map.Felucca);

        item.Visible = false;
        Assert.False(DecayScheduler.IsRegistered(item), "A hidden item must not be tracked for decay.");

        clock.Now = clock.Start + TimeSpan.FromDays(30);

        item.Visible = true;

        Assert.True(DecayScheduler.IsRegistered(item), "An unhidden item must be tracked for decay.");

        clock.AdvanceDecay(item.DecayTime - TimeSpan.FromMinutes(2), item);
        Assert.False(item.Deleted, "An unhidden item must get a full decay window, not vanish immediately.");

        clock.AdvanceDecay(TimeSpan.FromMinutes(4), item);
        Assert.True(item.Deleted, "An unhidden item must still decay once the fresh window elapses.");
    }

    // A refusal restarts the countdown without rewriting LastMoved. The item is already dequeued
    // when OnDecay refuses, so a refusal that does not reschedule strands it forever.
    [Fact]
    public void RefusedDecay_DoesNotRewriteLastMoved()
    {
        using var clock = new SimulatedClockScope();

        var item = _cleanup.Track(new RefusesDecayItem());
        item.MoveToWorld(new Point3D(110, 100, 0), Map.Felucca);
        var lastMoved = item.LastMoved;

        clock.AdvanceDecay(item.DecayTime + TimeSpan.FromMinutes(2), item);

        Assert.False(item.Deleted, "A refused decay must not delete the item.");
        Assert.True(DecayScheduler.IsRegistered(item), "A refused decay must leave the item tracked.");
        Assert.Equal(lastMoved, item.LastMoved);

        item.Delete();
    }

    // The fresh window must survive a save/load cycle, or a restart mid-window deletes the item.
    [Fact]
    public void FreshDecayWindow_SurvivesSerialization()
    {
        using var clock = new SimulatedClockScope();

        var item = _cleanup.Track(new Item(0x1234));
        item.MoveToWorld(new Point3D(111, 100, 0), Map.Felucca);

        item.Movable = false;
        clock.Now = clock.Start + TimeSpan.FromDays(30);
        item.Movable = true;

        var expected = item.ScheduledDecayTime;

        var writer = new BufferWriter(new byte[512], true);
        item.Serialize(writer);

        var copy = new Item(item.Serial);
        copy.Deserialize(new BufferReader(writer.Buffer));

        // The stamp is stored as a delta, so it ages only by the real time between
        // write and read - milliseconds here, the downtime in production.
        Assert.True(
            (copy.ScheduledDecayTime - expected).Duration() <= TimeSpan.FromSeconds(5),
            "The restarted decay window must survive a save/load cycle."
        );

        item.Delete();
        copy.Delete();
    }

    // A real move supersedes the reset stamp; it must be dropped so the CompactInfo can collapse.
    [Fact]
    public void MovingAnItem_ClearsASupersededDecayResetStamp()
    {
        using var clock = new SimulatedClockScope();

        var item = _cleanup.Track(new Item(0x1234));
        item.MoveToWorld(new Point3D(112, 100, 0), Map.Felucca);

        item.Movable = false;
        clock.Now = clock.Start + TimeSpan.FromDays(30);
        item.Movable = true;

        Assert.NotEqual(default, item.DecayResetTime);

        clock.Now += TimeSpan.FromMinutes(1);
        item.MoveToWorld(new Point3D(113, 100, 0), Map.Felucca);

        Assert.Equal(default, item.DecayResetTime);
        Assert.Equal(item.LastMoved + item.DecayTime, item.ScheduledDecayTime);
        Assert.True(DecayScheduler.IsRegistered(item));

        item.Delete();
    }

    // Losing decay eligibility makes the stamp meaningless; it must be dropped so the
    // CompactInfo is not held for as long as the item stays ineligible.
    [Fact]
    public void ItemBecomingIneligible_DropsTheDecayResetStamp()
    {
        using var clock = new SimulatedClockScope();

        var item = _cleanup.Track(new Item(0x1234));
        item.MoveToWorld(new Point3D(115, 100, 0), Map.Felucca);

        item.Movable = false;
        clock.Now = clock.Start + TimeSpan.FromDays(30);
        item.Movable = true;

        Assert.NotEqual(default, item.DecayResetTime);

        item.Movable = false;

        Assert.Equal(default, item.DecayResetTime);

        item.Delete();
    }

    // Moving a stamped item into a container programmatically (no drop, no SetLastMoved)
    // must also drop the stamp.
    [Fact]
    public void StampedItemAddedToContainer_DropsTheDecayResetStamp()
    {
        using var clock = new SimulatedClockScope();

        var pack = _cleanup.Track(new Container(0xE75));
        pack.MoveToWorld(new Point3D(116, 100, 0), Map.Felucca);

        var item = _cleanup.Track(new Item(0x1234));
        item.MoveToWorld(new Point3D(117, 100, 0), Map.Felucca);

        item.Movable = false;
        clock.Now = clock.Start + TimeSpan.FromDays(30);
        item.Movable = true;

        Assert.NotEqual(default, item.DecayResetTime);

        pack.AddItem(item);

        Assert.Equal(default, item.DecayResetTime);

        pack.Delete();
    }

    // A raw Map assignment (e.g. a GM changing Map through props) is a move: it must
    // enroll an untracked item for decay.
    [Fact]
    public void ItemMovedToRealMapViaMapSetter_IsRegisteredForDecay()
    {
        var item = _cleanup.Track(new Item(0x1234));
        Assert.False(DecayScheduler.IsRegistered(item));

        item.Map = Map.Felucca;

        Assert.True(item.CanDecay());
        Assert.True(DecayScheduler.IsRegistered(item), "Item placed on a map via the Map setter must be tracked.");

        item.Delete();
    }

    // LiftItemDupe places the remainder of a partially lifted ground stack via raw
    // Location/Map assignments, with no MoveToWorld fallback: it must still be tracked.
    [Fact]
    public void PartialLiftOfGroundStack_LeavesRemainderRegisteredForDecay()
    {
        var stack = _cleanup.Track(new Item(0x1234) { Stackable = true, Amount = 10 });
        stack.MoveToWorld(new Point3D(114, 100, 0), Map.Felucca);

        var remainder = _cleanup.Track(Mobile.LiftItemDupe(stack, 3));

        Assert.NotNull(remainder);
        Assert.Equal(7, remainder.Amount);
        Assert.Null(remainder.Parent);
        Assert.Equal(Map.Felucca, remainder.Map);
        Assert.True(
            DecayScheduler.IsRegistered(remainder),
            "The remainder of a partially lifted ground stack must be tracked for decay."
        );

        stack.Delete();
        remainder.Delete();
    }
}
