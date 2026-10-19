using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class MovementPacketTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TestSpeedControl(byte speedControl)
    {
        PacketAssert.Sends(
            new SpeedControl(speedControl),
            ns => ns.SendSpeedControl((SpeedControlSetting)speedControl)
        );
    }

    [Fact]
    public void TestMovePlayer()
    {
        const Direction d = Direction.Left;

        PacketAssert.Sends(new MovePlayer(d), ns => ns.SendMovePlayer(d));
    }

    [Fact]
    public void TestMovementRej()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        const byte seq = 100;

        PacketAssert.Sends(new MovementRej(seq, m), ns => ns.SendMovementRej(seq, m));
    }

    [Fact]
    public void TestMovementAck()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        const byte seq = 100;

        PacketAssert.Sends(MovementAck.Instantiate(seq, m), ns => ns.SendMovementAck(seq, m));
    }
}
