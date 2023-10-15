using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class LightPacketTests
    {
        [Fact]
        public void TestGlobalLightLevel()
        {
            const byte lightLevel = 5;
            var expected = new GlobalLightLevel(lightLevel).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendGlobalLightLevel(lightLevel);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestPersonalLightLevel()
        {
            Serial serial = (Serial)0x1024;
            byte lightLevel = 5;
            var expected = new PersonalLightLevel(serial, lightLevel).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendPersonalLightLevel(serial, lightLevel);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }
    }
}
