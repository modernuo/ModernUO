using System;
using Server.Multis;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests
{
    public class HousePacketTests
    {
        [Theory]
        [InlineData(0x1001u)]
        public void TestBeginHouseCustomization(uint serial)
        {
            var expected = new BeginHouseCustomization(serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendBeginHouseCustomization(serial);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(0x1001u)]
        public void TestEndHouseCustomization(uint serial)
        {
            var expected = new EndHouseCustomization(serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendEndHouseCustomization(serial);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(0x1001u, 0)]
        [InlineData(0x1001u, 100)]
        public void TestDesignStateGeneral(uint serial, int revision)
        {
            var expected = new DesignStateGeneral(serial, revision).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDesignStateGeneral(serial, revision);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
