using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class CombatPacketTests
    {
        [Fact]
        public void TestSwing()
        {
            Serial attacker = 0x1000;
            Serial defender = 0x2000;

            var expected = new Swing(attacker, defender).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSwing(attacker, defender);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSetWarMode(bool warmode)
        {
            var expected = new SetWarMode(warmode).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSetWarMode(warmode);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestChangeCombatant()
        {
            Serial serial = 0x1024;

            var expected = new ChangeCombatant(serial).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendChangeCombatant(serial);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
