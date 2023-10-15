using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests
{
    public class VirtualHairPacketTests: IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestSendVirtualHairUpdate()
        {
            var m = new Mobile((Serial)0x1024u);
            m.DefaultMobileInit();
            m.HairHue = 0x1000;
            m.HairItemID = 0x2000;

            var expected = new HairEquipUpdate(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendHairEquipUpdatePacket(m, HairInfo.FakeSerial(m.Serial), m.HairItemID, m.HairHue, Layer.Hair);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestSendRemoveVirtualHair()
        {
            var m = new Mobile((Serial)0x1024u);
            m.DefaultMobileInit();

            var expected = new RemoveHair(m).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendRemoveHairPacket(HairInfo.FakeSerial(m.Serial));

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }
    }
}
