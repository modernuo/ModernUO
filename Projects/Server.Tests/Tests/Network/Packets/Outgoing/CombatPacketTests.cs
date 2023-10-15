using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class CombatPacketTests
    {
        [Fact]
        public void TestSwing()
        {
            Serial attacker = (Serial)0x1024;
            Serial defender = (Serial)0x2048;

            var expected = new Swing(attacker, defender).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSwing(attacker, defender);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Theory, InlineData(true), InlineData(false)]
        public void TestSetWarMode(bool warmode)
        {
            var expected = new SetWarMode(warmode).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSetWarMode(warmode);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestChangeCombatant()
        {
            Serial serial = (Serial)0x1024;

            var expected = new ChangeCombatant(serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendChangeCombatant(serial);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }
    }
}
