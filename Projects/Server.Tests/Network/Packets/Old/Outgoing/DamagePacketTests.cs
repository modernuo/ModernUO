using System;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class DamagePacketTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData(10)]
        [InlineData(-5)]
        [InlineData(1024)]
        public void TestDamagePacketOld(int inputAmount)
        {
            Serial serial = 0x1024;

            var expected = new DamagePacketOld(serial, inputAmount).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDamage(serial, inputAmount);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(-5)]
        [InlineData(1024)]
        [InlineData(100000)]
        public void TestDamage(int inputAmount)
        {
            Serial serial = 0x1024;

            var expected = new DamagePacket(serial, inputAmount).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = ProtocolChanges.DamagePacket;

            ns.SendDamage(serial, inputAmount);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
