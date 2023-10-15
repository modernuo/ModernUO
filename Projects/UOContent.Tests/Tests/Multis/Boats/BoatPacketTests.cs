using System.Collections.Generic;
using System.Linq;
using Server;
using Server.Multis;
using Server.Multis.Boats;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

public class BoatPacketTests : IClassFixture<ServerFixture>
{
    [Theory]
    [InlineData(Direction.West, 10, 100, 200)]
    public void TestMoveBoatHS(Direction d, int speed, int xOffset, int yOffset)
    {
        var item1 = new Item((Serial)0x1000)
        {
            ItemID = 0x13B9,
            Location = new Point3D(11, 21, 16),
            Map = Map.Felucca,
            Visible = true
        };
        var item2 = new Item((Serial)0x2000)
        {
            ItemID = 0x13B9,
            Location = new Point3D(100, 200, 16),
            Map = Map.Felucca,
            Visible = true
        };

        var beholder = new MockedMobile((Serial)0x100);
        beholder.DefaultMobileInit();
        beholder.Location = new Point3D(10, 20, 15);
        beholder.Map = Map.Felucca;
        beholder.CanSeeEntities.Add(item1);

        var list = new List<IEntity> { item1, beholder };
        var notContained = new List<IEntity> { item2 };
        var boat = new TestBoat((Serial)0x3000, list, notContained)
        {
            Location = new Point3D(10, 20, 15),
            Facing = Direction.Right,
            Map = Map.Felucca
        };

        beholder.CanSeeEntities.Add(boat);

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.ProtocolChanges = ProtocolChanges.HighSeas;
        var expected = new MoveBoatHS(beholder, boat, d, speed, list, xOffset, yOffset).Compile();

        ns.SendMoveBoatHS(beholder, boat, d, speed, boat.GetMovingEntities(true), xOffset, yOffset);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestDisplayBoatHS()
    {
        var item1 = new Item((Serial)0x1000)
        {
            ItemID = 0x13B9,
            Location = new Point3D(11, 21, 16),
            Map = Map.Felucca
        };
        var item2 = new Item((Serial)0x2000)
        {
            ItemID = 0x13B9,
            Location = new Point3D(100, 200, 16),
            Map = Map.Felucca
        };

        var beholder = new MockedMobile((Serial)0x100);
        beholder.DefaultMobileInit();
        beholder.Location = new Point3D(10, 20, 15);

        beholder.CanSeeEntities.Add(item1);

        var list = new List<IEntity> { item1, beholder };
        var notContained = new List<IEntity> { item2 };
        var boat = new TestBoat((Serial)0x3000, list, notContained)
        {
            Location = new Point3D(10, 20, 15),
            Facing = Direction.Right
        };

        beholder.CanSeeEntities.Add(boat);

        var ns = PacketTestUtilities.CreateTestNetState();
        ns.ProtocolChanges = ProtocolChanges.HighSeas;

        var expected = new DisplayBoatHS(beholder, boat).Compile();

        ns.SendDisplayBoatHS(beholder, boat);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
    }

    private class TestBoat : BaseBoat
    {
        private readonly List<IEntity> _list;
        private readonly List<IEntity> _notContainedList;

        public TestBoat(Serial serial, List<IEntity> list, List<IEntity> notContainedList) : base(serial)
        {
            _list = list;
            _notContainedList = notContainedList;
        }

        public override MultiComponentList Components { get; } = new(new List<MultiTileEntry>());

        public override bool Contains(int x, int y) => !_notContainedList.Any(e => e.X == x && e.Y == y);

        public override MovingEntitiesEnumerable GetMovingEntities(bool includeBoat = false) =>
            new(this, true, new PooledEnumeration.PooledEnumerable<IEntity>(_list));
    }

    private class MockedMobile : Mobile
    {
        public HashSet<IEntity> CanSeeEntities = new();

        public MockedMobile(Serial serial) : base(serial)
        {
        }

        public override bool CanSee(Mobile m) => m == this;

        public override bool CanSee(Item i) => CanSeeEntities.Contains(i);
    }
}
