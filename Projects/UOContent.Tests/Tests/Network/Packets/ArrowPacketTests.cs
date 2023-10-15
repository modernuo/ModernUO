using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class ArrowPacketTests
    {
        [Fact]
        public void TestCancelArrow()
        {
            var expected = new CancelArrow().Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendCancelArrow(0, 0, Serial.Zero);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 10)]
        [InlineData(100000, 100000)]
        public void TestSetArrow(int x, int y)
        {
            var expected = new SetArrow(x, y).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSetArrow(x, y, Serial.Zero);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 10)]
        [InlineData(100000, 100000)]
        public void TestCancelArrowHS(int x, int y)
        {
            Serial serial = (Serial)0x1024;

            var expected = new CancelArrowHS(x, y, serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = ProtocolChanges.HighSeas;
            ns.SendCancelArrow(x, y, serial);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 10)]
        [InlineData(100000, 100000)]
        public void TestSetArrowHS(int x, int y)
        {
            Serial serial = (Serial)0x1024;

            var expected = new SetArrowHS(x, y, serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = ProtocolChanges.HighSeas;
            ns.SendSetArrow(x, y, serial);

        var result = ns.SendPipe.Reader.AvailableToRead();
        AssertThat.Equal(result, expected);
        }
    }
}
