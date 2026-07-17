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
