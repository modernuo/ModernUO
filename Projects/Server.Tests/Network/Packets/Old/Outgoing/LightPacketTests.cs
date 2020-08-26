using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class LightPacketTests
    {
        [Fact]
        public void TestGlobalLightLevel()
        {
            byte lightLevel = 5;
            var data = new GlobalLightLevel(lightLevel).Compile();

            Span<byte> expectedData = stackalloc byte[2];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x4F); // Packet ID
            expectedData.Write(ref pos, lightLevel);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestPersonalLightLevel()
        {
            Serial serial = 0x1;
            byte lightLevel = 5;
            var data = new PersonalLightLevel(serial, lightLevel).Compile();

            Span<byte> expectedData = stackalloc byte[6];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x4E); // Packet ID
            expectedData.Write(ref pos, serial);
            expectedData.Write(ref pos, lightLevel);

            AssertThat.Equal(data, expectedData);
        }
    }
}
