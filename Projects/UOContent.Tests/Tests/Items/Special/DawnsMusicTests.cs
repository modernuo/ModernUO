using System;
using System.Reflection;
using Server;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class DawnsMusicTests
{
    // Multi data is not loaded in tests, so the house covers its own tile by declaration.
    private sealed class TestHouse(Mobile owner) : SmallOldHouse(owner, 0x64)
    {
        public override bool IsInside(Point3D p, int height) => !Deleted && p.X == X && p.Y == Y;
    }

    [Fact]
    public void GearIsConsumedWhenItsTrackIsAdded()
    {
        var from = new PlayerMobile { Player = true };
        var box = new DawnsMusicBox();
        var track = Enum.GetValues<MusicName>()[0];
        box.Tracks.Remove(track);
        var gear = new DawnsMusicGear(track);

        try
        {
            gear.OnDoubleClick(from);
            typeof(Target).GetMethod("OnTarget", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(from.Target, [from, box]);

            Assert.Contains(track, box.Tracks);
            Assert.True(gear.Deleted);
        }
        finally
        {
            gear.Delete();
            box.Delete();
            from.Delete();
        }
    }

    [Fact]
    public void BoxLockedDownToAnyoneIsUsableByAVisitor()
    {
        var owner = new PlayerMobile(World.NewMobile);
        owner.DefaultMobileInit();
        var visitor = new PlayerMobile(World.NewMobile);
        visitor.DefaultMobileInit();
        visitor.Player = true;
        owner.MoveToWorld(new Point3D(1400, 1400, 0), Map.Felucca);
        var house = new TestHouse(owner);
        var box = new DawnsMusicBox { Level = SecureLevel.Anyone };

        try
        {
            house.MoveToWorld(new Point3D(1400, 1400, 0), Map.Felucca);
            box.MoveToWorld(new Point3D(1400, 1400, 0), Map.Felucca);

            Assert.False(house.HasAccess(visitor));
            Assert.True(box.HasAccess(visitor));
        }
        finally
        {
            box.Delete();
            house.Delete();
            visitor.Delete();
            owner.Delete();
        }
    }
}
