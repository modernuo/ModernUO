using System;
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
            var expected = new SpeedControl(speedControl).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendSpeedControl((SpeedControlSetting)speedControl);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMovePlayer()
        {
            const Direction d = Direction.Left;
            var expected = new MovePlayer(d).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMovePlayer(d);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMovementRej()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            const byte seq = 100;

            var expected = new MovementRej(seq, m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMovementRej(seq, m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestMovementAck()
        {
            var m = new Mobile(0x1);
            m.DefaultMobileInit();

            const byte seq = 100;

            var expected = MovementAck.Instantiate(seq, m).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendMovementAck(seq, m);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestNullFastwalkStack()
        {
            var expected = new NullFastwalkStack().Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendInitialFastwalkStack(new uint[]{ 0, 0, 0, 0, 0, 0 });

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
