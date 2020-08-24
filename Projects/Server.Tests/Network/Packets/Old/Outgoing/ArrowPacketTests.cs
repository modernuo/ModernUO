using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network.Packets
{
    public class ArrowPacketTests
    {
        [Fact]
        public void TestCancelArrow()
        {
            Span<byte> data = new CancelArrow().Compile();

            Span<byte> expectedData = stackalloc byte[6];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xBA); // Packet ID
            expectedData.Write(ref pos, (byte)0); // Command
            expectedData.Write(ref pos, 0xFFFFFFFF); // X, Y

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 10)]
        [InlineData(100000, 100000)]
        public void TestSetArrow(int x, int y)
        {
            Span<byte> data = new SetArrow(x, y).Compile();

            Span<byte> expectedData = stackalloc byte[6];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xBA); // Packet ID
            expectedData.Write(ref pos, (byte)0x01); // Command
            expectedData.Write(ref pos, (ushort)x);
            expectedData.Write(ref pos, (ushort)y);

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 10)]
        [InlineData(100000, 100000)]
        public void TestCancelArrowHS(int x, int y)
        {
            Serial serial = 0x01;
            Span<byte> data = new CancelArrowHS(x, y, serial).Compile();

            Span<byte> expectedData = stackalloc byte[10];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xBA); // Packet ID
#if NO_LOCAL_INIT
      expectedData.Write(ref pos, (byte)0); // Command
#else
            pos++;
#endif
            expectedData.Write(ref pos, (ushort)x);
            expectedData.Write(ref pos, (ushort)y);
            expectedData.Write(ref pos, serial);

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(100, 10)]
        [InlineData(100000, 100000)]
        public void TestSetArrowHS(int x, int y)
        {
            Serial serial = 0x01;
            Span<byte> data = new SetArrowHS(x, y, serial).Compile();

            Span<byte> expectedData = stackalloc byte[10];
            int pos = 0;

            expectedData.Write(ref pos, (byte)0xBA); // Packet ID
            expectedData.Write(ref pos, (byte)0x01); // Command
            expectedData.Write(ref pos, (ushort)x);
            expectedData.Write(ref pos, (ushort)y);
            expectedData.Write(ref pos, serial);

            AssertThat.Equal(data, expectedData);
        }
    }
}
