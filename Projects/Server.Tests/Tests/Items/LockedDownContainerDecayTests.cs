using System;
using Server.Items;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class LockedDownContainerDecayTests : IDisposable
{
    private readonly EntityCleanup _cleanup = new();

    public LockedDownContainerDecayTests()
    {
        DecayScheduler.ResetForTests();

        // Content assigns these at startup (BaseHouse); the engine tests have no content.
        if (Item.LockedDownFlag == 0)
        {
            Item.LockedDownFlag = 1;
            Item.SecureFlag = 2;
        }
    }

    public void Dispose() => _cleanup.Dispose();

    private class TestChest : Container
    {
        public TestChest() : base(0xE43)
        {
        }
    }

    private class KeepsContentsChest : Container
    {
        public KeepsContentsChest() : base(0xE43)
        {
        }

        public override bool ContentsDecay => false;
    }

    private Item NewLoot() => _cleanup.Track(new Item(0x1F03) { Movable = true, Visible = true });

    private TestChest PlaceLockedDownChest(int x)
    {
        var chest = _cleanup.Track(new TestChest { Movable = false });
        chest.MoveToWorld(new Point3D(x, 100, 0), Map.Felucca);
        chest.IsLockedDown = true;
        return chest;
    }

    [Fact]
    public void ItemDroppedIntoLockedDownContainer_IsTracked()
    {
        var chest = PlaceLockedDownChest(120);
        var loot = NewLoot();

        chest.AddItem(loot);

        Assert.True(loot.CanDecay());
        Assert.True(DecayScheduler.IsRegistered(loot));

        chest.Delete();
    }

    [Fact]
    public void ItemInOrdinaryContainer_IsNotTracked()
    {
        var chest = _cleanup.Track(new TestChest());
        chest.MoveToWorld(new Point3D(121, 100, 0), Map.Felucca);
        var loot = NewLoot();

        chest.AddItem(loot);

        Assert.False(loot.CanDecay());
        Assert.False(DecayScheduler.IsRegistered(loot));

        chest.Delete();
    }

    [Fact]
    public void LockingAndReleasingContainer_RegistersAndUnregistersContents()
    {
        var chest = _cleanup.Track(new TestChest { Movable = false });
        chest.MoveToWorld(new Point3D(122, 100, 0), Map.Felucca);
        var loot = NewLoot();
        chest.AddItem(loot);

        Assert.False(DecayScheduler.IsRegistered(loot));

        chest.IsLockedDown = true;
        Assert.True(DecayScheduler.IsRegistered(loot));

        chest.IsSecure = true; // secure containers keep their contents
        Assert.False(DecayScheduler.IsRegistered(loot));

        chest.IsSecure = false;
        Assert.True(DecayScheduler.IsRegistered(loot));

        chest.IsLockedDown = false;
        Assert.False(DecayScheduler.IsRegistered(loot));

        chest.Delete();
    }

    [Fact]
    public void ContainerThatKeepsContents_DoesNotTrackThem()
    {
        var board = _cleanup.Track(new KeepsContentsChest { Movable = false });
        board.MoveToWorld(new Point3D(123, 100, 0), Map.Felucca);
        board.IsLockedDown = true;
        var piece = NewLoot();

        board.AddItem(piece);

        Assert.False(DecayScheduler.IsRegistered(piece));

        board.Delete();
    }

    [Fact]
    public void NestedContainerContents_AreNotTracked()
    {
        var chest = PlaceLockedDownChest(124);
        var bag = _cleanup.Track(new TestChest());
        chest.AddItem(bag);
        var loot = NewLoot();
        bag.AddItem(loot);

        Assert.True(DecayScheduler.IsRegistered(bag));
        Assert.False(DecayScheduler.IsRegistered(loot));

        chest.Delete();
    }

    [Fact]
    public void LockedDownItemInsideLockedDownContainer_IsNotTracked()
    {
        var chest = PlaceLockedDownChest(125);
        var loot = NewLoot();
        chest.AddItem(loot);
        loot.IsLockedDown = true;

        Assert.False(DecayScheduler.IsRegistered(loot));

        chest.Delete();
    }

    [Fact]
    public void TrackedContents_DecayOnSchedule()
    {
        using var clock = new SimulatedClockScope();

        var chest = PlaceLockedDownChest(126);
        var loot = NewLoot();
        chest.AddItem(loot);

        clock.AdvanceDecay(loot.DecayTime + TimeSpan.FromMinutes(2), loot);

        Assert.True(loot.Deleted, "Contents of a locked-down container must decay.");
        Assert.False(chest.Deleted, "The locked-down container itself must not decay.");

        chest.Delete();
    }
}
