using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class DamagePacketTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData(10, 10)]
        [InlineData(-5, 0)]
        [InlineData(1024, 0xFF)]
        public void TestDamagePacketOld(int inputAmount, byte expectedAmount)
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            Span<byte> data = new DamagePacketOld(m.Serial, inputAmount).Compile();

            Span<byte> expectedData = stackalloc byte[11];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xBF); // Packet ID
            expectedData.Write(ref pos, (ushort)11); // Length
            expectedData.Write(ref pos, (ushort)0x22); // Sub-packet
            expectedData.Write(ref pos, (byte)0x01); // Command
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, expectedAmount);

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(10, 10)]
        [InlineData(-5, 0)]
        [InlineData(1024, 1024)]
        [InlineData(100000, 0xFFFF)]
        public void TestDamage(int inputAmount, ushort expectedAmount)
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            Span<byte> data = new DamagePacket(m.Serial, inputAmount).Compile();

            Span<byte> expectedData = stackalloc byte[7];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0x0B); // Packet ID
            expectedData.Write(ref pos, m.Serial);
            expectedData.Write(ref pos, expectedAmount);

            AssertThat.Equal(data, expectedData);
        }
    }
}
