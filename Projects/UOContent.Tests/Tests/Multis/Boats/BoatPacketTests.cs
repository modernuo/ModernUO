using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Server;
using Server.Multis;
using Server.Multis.Boats;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests
{
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

            var beholder = new Mock<Mobile>((Serial)0x100);
            beholder.Object.DefaultMobileInit();
            beholder.Object.Location = new Point3D(10, 20, 15);
            beholder.Object.Map = Map.Felucca;
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == beholder.Object))).Returns(true);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == item1))).Returns(true);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == item2))).Returns(false);

            var list = new List<IEntity> { item1, beholder.Object };
            var notContained = new List<IEntity> { item2 };
            var boat = new TestBoat(0x3000, list, notContained)
            {
                Location = new Point3D(10, 20, 15),
                Facing = Direction.Right,
                Map = Map.Felucca
            };

            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == boat))).Returns(true);

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = ProtocolChanges.HighSeas;
            var expected = new MoveBoatHS(beholder.Object, boat, d, speed, list, xOffset, yOffset).Compile();

            ns.SendMoveBoatHS(beholder.Object, boat, d, speed, boat.GetMovingEntities(true), xOffset, yOffset);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
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

            var beholder = new Mock<Mobile>((Serial)0x100);
            beholder.Object.DefaultMobileInit();
            beholder.Object.Location = new Point3D(10, 20, 15);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == beholder.Object))).Returns(true);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == item1))).Returns(true);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == item2))).Returns(false);

            var list = new List<IEntity> { item1, beholder.Object };
            var notContained = new List<IEntity> { item2 };
            var boat = new TestBoat(0x3000, list, notContained)
            {
                Location = new Point3D(10, 20, 15),
                Facing = Direction.Right
            };

            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == boat))).Returns(true);

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = ProtocolChanges.HighSeas;

            var expected = new DisplayBoatHS(beholder.Object, boat).Compile();

            ns.SendDisplayBoatHS(beholder.Object, boat);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        public class TestBoat : BaseBoat
        {
            private readonly List<IEntity> components;
            private readonly List<IEntity> notContained;

            public TestBoat(Serial serial, List<IEntity> list, List<IEntity> notContainedList) : base(serial)
            {
                components = list;
                notContained = notContainedList;
                Components = new MultiComponentList(new List<MultiTileEntry>());
            }

            public override MultiComponentList Components { get; }

            public override bool Contains(int x, int y) => !notContained.Any(e => e.X == x && e.Y == y);

            public override MovingEntitiesEnumerable GetMovingEntities(bool includeBoat = false) =>
                new(this, true, new Map.PooledEnumerable<IEntity>(components));
        }
    }
}
