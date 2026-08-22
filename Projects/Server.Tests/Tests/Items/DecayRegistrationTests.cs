using System;
using Server.Items;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class DecayRegistrationTests
{
    // The wheel is static and these tests move the clock, so start each from a known schedule.
    public DecayRegistrationTests() => DecayScheduler.ResetForTests();

    // Stands in for a region that refuses decay, e.g. HouseRegion over a locked down item.
    private class RefusesDecayItem : Item
    {
        public RefusesDecayItem() : base(0x1234)
        {
        }

        public override bool OnDecay() => false;
    }

    // The item is already dequeued, so a refusal that does not reschedule strands it forever.
    [Fact]
    public void ItemWhoseDecayIsRefused_StaysTrackedAndSurvives()
    {
        var start = Core._now;

        try
        {
            var item = new RefusesDecayItem();
            item.MoveToWorld(new Point3D(106, 100, 0), Map.Felucca);

            AdvanceDecay(start, item.DecayTime + TimeSpan.FromMinutes(2), item);

            Assert.False(item.Deleted, "A refused decay must not delete the item.");
            Assert.True(
                DecayScheduler.IsRegistered(item),
                "A refused decay must leave the item tracked so it is retried."
            );

            item.Delete();
        }
        finally
        {
            Core._now = start;
        }
    }

    // Drives the scheduler and the game clock without depending on the shared timer wheel.
    private static void AdvanceDecay(DateTime start, TimeSpan duration, Item item)
    {
        var deadline = start + duration;

        for (var now = start; now <= deadline && !item.Deleted; now += TimeSpan.FromMilliseconds(256))
        {
            Core._now = now;
            DecayScheduler.ProcessTick(now);
        }
    }

    [Fact]
    public void ItemOnGround_ActuallyDecaysAfterDecayTime()
    {
        var start = Core._now;

        try
        {
            var item = new Item(0x1234);
            item.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);

            Assert.True(DecayScheduler.IsRegistered(item));

            AdvanceDecay(start, item.DecayTime + TimeSpan.FromMinutes(2), item);

            Assert.True(item.Deleted, "Item on the ground must decay once DecayTime elapses.");
        }
        finally
        {
            Core._now = start;
        }
    }

    [Fact]
    public void ItemOnGround_DoesNotDecayBeforeDecayTime()
    {
        var start = Core._now;

        try
        {
            var item = new Item(0x1234);
            item.MoveToWorld(new Point3D(102, 100, 0), Map.Felucca);

            AdvanceDecay(start, item.DecayTime - TimeSpan.FromMinutes(2), item);

            Assert.False(item.Deleted, "Item must not decay before DecayTime elapses.");

            item.Delete();
        }
        finally
        {
            Core._now = start;
        }
    }

    [Fact]
    public void ItemPlacedInWorld_IsRegisteredForDecay()
    {
        var item = new Item(0x1234);
        item.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);

        Assert.True(item.CanDecay());
        Assert.True(DecayScheduler.IsRegistered(item), "Item on the ground must be tracked for decay.");

        item.Delete();
    }

    // The real client path: Mobile.Lift internalizes, then Mobile.Drop calls MoveToWorld.
    [Fact]
    public void ItemLiftedThenDroppedToGround_IsRegisteredForDecay()
    {
        var item = new Item(0x1234);
        item.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);

        // Player lifts the item to the cursor.
        item.Internalize();
        Assert.Equal(Map.Internal, item.Map);

        // Player drops it back onto the ground.
        item.MoveToWorld(new Point3D(101, 100, 0), Map.Felucca);

        Assert.True(item.CanDecay());
        Assert.True(
            DecayScheduler.IsRegistered(item),
            "Item dropped to the ground after being lifted must be tracked for decay."
        );

        item.Delete();
    }

    // Held on the cursor means Map.Internal, which must not be tracked.
    [Fact]
    public void ItemLiftedToCursor_IsNotRegisteredForDecay()
    {
        var item = new Item(0x1234);
        item.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);

        item.Internalize();

        Assert.False(item.CanDecay());
        Assert.False(DecayScheduler.IsRegistered(item), "Item held on the cursor must not be tracked for decay.");

        item.Delete();
    }

    // A new item is on Map.Internal, so construction must not touch the scheduler.
    [Fact]
    public void NewItem_IsNotTracked()
    {
        var item = new Item(0x1234);

        Assert.Equal(DecayScheduler.SlotNone, item.DecaySlot);
        Assert.False(DecayScheduler.IsRegistered(item), "A newly constructed item must not be tracked.");

        item.Delete();
    }

    // Unregister() trusts DecaySlot; IsRegistered() scans the structures. Assert both to catch drift.
    [Fact]
    public void DecaySlot_AgreesWithStructures_AcrossLifecycle()
    {
        var item = new Item(0x1234);

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
        var pack = new Container(0xE75);
        pack.MoveToWorld(new Point3D(105, 100, 0), Map.Felucca);

        var item = new Item(0x1234);
        pack.AddItem(item);
        Assert.False(DecayScheduler.IsRegistered(item));

        pack.Destroy();

        Assert.True(item.CanDecay());
        Assert.True(DecayScheduler.IsRegistered(item), "Contents spilled by Container.Destroy must be tracked.");

        item.Delete();
    }

    // A GM freezes an item, time passes, then unfreezes it. The stale LastMoved must not
    // let the scheduler delete it on the next tick; it gets a fresh decay window instead.
    [Fact]
    public void StaleImmovableItemMadeMovable_GetsAFreshDecayWindow()
    {
        var start = Core._now;

        try
        {
            var item = new Item(0x1234);
            item.MoveToWorld(new Point3D(107, 100, 0), Map.Felucca);

            item.Movable = false;
            Assert.False(DecayScheduler.IsRegistered(item), "A frozen item must not be tracked for decay.");

            // Months pass while the item sits frozen; LastMoved goes stale.
            Core._now = start + TimeSpan.FromDays(30);
            var flipped = Core._now;

            item.Movable = true;

            Assert.True(DecayScheduler.IsRegistered(item), "An unfrozen item must be tracked for decay.");

            AdvanceDecay(flipped, item.DecayTime - TimeSpan.FromMinutes(2), item);
            Assert.False(item.Deleted, "An unfrozen item must get a full decay window, not vanish immediately.");

            AdvanceDecay(Core._now, TimeSpan.FromMinutes(4), item);
            Assert.True(item.Deleted, "An unfrozen item must still decay once the fresh window elapses.");
        }
        finally
        {
            Core._now = start;
        }
    }

    // Same transition through the Visible setter: unhiding a long-hidden item.
    [Fact]
    public void StaleHiddenItemMadeVisible_GetsAFreshDecayWindow()
    {
        var start = Core._now;

        try
        {
            var item = new Item(0x1234);
            item.MoveToWorld(new Point3D(109, 100, 0), Map.Felucca);

            item.Visible = false;
            Assert.False(DecayScheduler.IsRegistered(item), "A hidden item must not be tracked for decay.");

            Core._now = start + TimeSpan.FromDays(30);
            var flipped = Core._now;

            item.Visible = true;

            Assert.True(DecayScheduler.IsRegistered(item), "An unhidden item must be tracked for decay.");

            AdvanceDecay(flipped, item.DecayTime - TimeSpan.FromMinutes(2), item);
            Assert.False(item.Deleted, "An unhidden item must get a full decay window, not vanish immediately.");

            AdvanceDecay(Core._now, TimeSpan.FromMinutes(4), item);
            Assert.True(item.Deleted, "An unhidden item must still decay once the fresh window elapses.");
        }
        finally
        {
            Core._now = start;
        }
    }

    // A refusal restarts the countdown without rewriting LastMoved.
    [Fact]
    public void RefusedDecay_DoesNotRewriteLastMoved()
    {
        var start = Core._now;

        try
        {
            var item = new RefusesDecayItem();
            item.MoveToWorld(new Point3D(110, 100, 0), Map.Felucca);
            var lastMoved = item.LastMoved;

            AdvanceDecay(start, item.DecayTime + TimeSpan.FromMinutes(2), item);

            Assert.False(item.Deleted, "A refused decay must not delete the item.");
            Assert.True(DecayScheduler.IsRegistered(item), "A refused decay must leave the item tracked.");
            Assert.Equal(lastMoved, item.LastMoved);

            item.Delete();
        }
        finally
        {
            Core._now = start;
        }
    }

    // The fresh window must survive a save/load cycle, or a restart mid-window deletes the item.
    [Fact]
    public void FreshDecayWindow_SurvivesSerialization()
    {
        var start = Core._now;

        try
        {
            var item = new Item(0x1234);
            item.MoveToWorld(new Point3D(111, 100, 0), Map.Felucca);

            item.Movable = false;
            Core._now = start + TimeSpan.FromDays(30);
            item.Movable = true;

            var expected = item.ScheduledDecayTime;

            var writer = new BufferWriter(new byte[512], true);
            item.Serialize(writer);

            var copy = new Item(item.Serial);
            copy.Deserialize(new BufferReader(writer.Buffer));

            // LastMoved persists as whole minutes; allow that much slack.
            Assert.True(
                (copy.ScheduledDecayTime - expected).Duration() <= TimeSpan.FromMinutes(1),
                "The restarted decay window must survive a save/load cycle."
            );

            item.Delete();
            copy.Delete();
        }
        finally
        {
            Core._now = start;
        }
    }

    // A real move supersedes the reset stamp; it must be dropped so the CompactInfo can collapse.
    [Fact]
    public void MovingAnItem_ClearsASupersededDecayResetStamp()
    {
        var start = Core._now;

        try
        {
            var item = new Item(0x1234);
            item.MoveToWorld(new Point3D(112, 100, 0), Map.Felucca);

            item.Movable = false;
            Core._now = start + TimeSpan.FromDays(30);
            item.Movable = true;

            Assert.NotEqual(default, item.DecayResetTime);

            // A real move supersedes the stamp.
            Core._now += TimeSpan.FromMinutes(1);
            item.MoveToWorld(new Point3D(113, 100, 0), Map.Felucca);

            Assert.Equal(default, item.DecayResetTime);
            Assert.Equal(item.LastMoved + item.DecayTime, item.ScheduledDecayTime);
            Assert.True(DecayScheduler.IsRegistered(item));

            item.Delete();
        }
        finally
        {
            Core._now = start;
        }
    }

    // A raw Map assignment (e.g. a GM changing Map through props) is a move: it must enroll
    // an untracked item for decay instead of leaving it to linger forever.
    [Fact]
    public void ItemMovedToRealMapViaMapSetter_IsRegisteredForDecay()
    {
        var item = new Item(0x1234);
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
        var stack = new Item(0x1234) { Stackable = true, Amount = 10 };
        stack.MoveToWorld(new Point3D(114, 100, 0), Map.Felucca);

        var remainder = Mobile.LiftItemDupe(stack, 3);

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

    // Dropping into a container must untrack; taking it back out to the ground must re-track.
    [Fact]
    public void ItemMovedIntoContainerThenBackToGround_IsRegisteredForDecay()
    {
        var pack = new Container(0xE75);
        pack.MoveToWorld(new Point3D(100, 100, 0), Map.Felucca);

        var item = new Item(0x1234);
        pack.AddItem(item);

        Assert.False(DecayScheduler.IsRegistered(item), "Item inside a container must not be tracked for decay.");

        item.Internalize();
        item.MoveToWorld(new Point3D(101, 100, 0), Map.Felucca);

        Assert.True(item.CanDecay());
        Assert.True(DecayScheduler.IsRegistered(item), "Item taken out of a container must be tracked for decay.");

        item.Delete();
        pack.Delete();
    }
}
