using System;
using System.Buffers;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class MovementPacketTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void TestSpeedControl(byte speedControl)
        {
            var data = new SpeedControl(speedControl).Compile();

            Span<byte> expectedData = stackalloc byte[6];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xBF);   // Packet ID
            expectedData.Write(ref pos, (ushort)6);    // Length
            expectedData.Write(ref pos, (ushort)0x26); // Command
            expectedData.Write(ref pos, speedControl);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMovePlayer()
        {
            const Direction d = Direction.Left;
            var data = new MovePlayer(d).Compile();

            Span<byte> expectedData = stackalloc byte[2];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x97); // Packet ID
            expectedData.Write(ref pos, (byte)d);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMovementRej()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            const byte seq = 100;

            var data = new MovementRej(seq, m).Compile();

            Span<byte> expectedData = stackalloc byte[8];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x21); // Packet ID
            expectedData.Write(ref pos, seq);
            expectedData.Write(ref pos, (short)m.X);
            expectedData.Write(ref pos, (short)m.Y);
            expectedData.Write(ref pos, (byte)m.Direction);
            expectedData.Write(ref pos, (byte)m.Z);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestMovementAck()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            const byte seq = 100;
            var noto = Notoriety.Compute(m, m);

            var data = MovementAck.Instantiate(seq, m).Compile();

            Span<byte> expectedData = stackalloc byte[3];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x22); // Packet ID
            expectedData.Write(ref pos, seq);
            expectedData.Write(ref pos, (byte)noto);

            AssertThat.Equal(data, expectedData);
        }

        [Fact]
        public void TestNullFastwalkStack()
        {
            var data = new NullFastwalkStack().Compile();

            Span<byte> expectedData = stackalloc byte[29];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0xBF); // Packet ID
            expectedData.Write(ref pos, (ushort)29); // Length
            expectedData.Write(ref pos, (short)0x1); // Sub-packet

#if NO_LOCAL_INIT
            expectedData.Write(ref pos, 0); // Key 1
            expectedData.Write(ref pos, 0); // Key 2
            expectedData.Write(ref pos, 0); // Key 3
            expectedData.Write(ref pos, 0); // Key 4
            expectedData.Write(ref pos, 0); // Key 5
            expectedData.Write(ref pos, 0); // Key 6
#endif

            AssertThat.Equal(data, expectedData);
        }
    }
}
