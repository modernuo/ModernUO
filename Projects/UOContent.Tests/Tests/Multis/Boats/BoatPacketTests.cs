using System;
using System.Collections.Generic;
using Moq;
using Server;
using Server.Items;
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
            var boat = new Mock<BaseBoat>((Serial)0x2);
            boat.Object.Location = new Point3D(10, 20, 15);
            boat.Object.Facing = Direction.Right;

            // Item on the boat
            var item1 = new Mock<BaseWeapon>((Serial)0x2);
            item1.Setup(m => m.ItemID).Returns(0x13B9);
            item1.Object.Location = new Point3D(10, 20, 15);

            // Item not the boat
            var item2 = new Mock<BaseWeapon>((Serial)0x2);
            item2.Setup(m => m.ItemID).Returns(0x13B9);
            item2.Object.Location = new Point3D(100, 200, 15);

            var beholder = new Mock<Mobile>((Serial)0x1024u);
            beholder.Object.Location = new Point3D(10, 20, 15);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == boat.Object))).Returns(true);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == item1.Object))).Returns(true);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == item2.Object))).Returns(false);

            var list = new List<IEntity>(4) { item1.Object, item2.Object, boat.Object, beholder.Object };

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = ProtocolChanges.HighSeas;
            var expected = new MoveBoatHS(beholder.Object, boat.Object, d, speed, list, xOffset, yOffset).Compile();

            ns.SendMoveBoatHS(beholder.Object, boat.Object, d, speed, list, xOffset, yOffset);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestDisplayBoatHS()
        {
            var item1 = new Item((Serial)0x1000) { ItemID = 0x13B9, Location = new Point3D(11, 21, 16), Map = Map.Felucca };
            var item2 = new Item((Serial)0x2000) { ItemID = 0x13B9, Location = new Point3D(100, 200, 16), Map = Map.Felucca };

            var beholder = new Mock<Mobile>((Serial)0x100);
            beholder.Object.DefaultMobileInit();
            beholder.Object.Location = new Point3D(10, 20, 15);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == beholder.Object))).Returns(true);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == item1))).Returns(true);
            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == item2))).Returns(false);

            var boat = new Mock<BaseBoat>((Serial)0x3000);
            boat.Setup(b => b.GetMovingEntities()).Returns(() => new List<IEntity>{ item1, beholder.Object });
            boat.Setup(b => b.Location).Returns(new Point3D(10, 20, 15));
            boat.Object.Facing = Direction.Right;

            beholder.Setup(m => m.CanSee(It.Is<IEntity>(e => e == boat.Object))).Returns(true);

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = ProtocolChanges.HighSeas;

            var expected = new DisplayBoatHS(beholder.Object, boat.Object).Compile();

            ns.SendDisplayBoatHS(beholder.Object, boat.Object);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
