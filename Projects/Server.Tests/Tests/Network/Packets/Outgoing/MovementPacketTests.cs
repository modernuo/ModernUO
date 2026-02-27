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
        var expected = new SpeedControl(speedControl).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendSpeedControl((SpeedControlSetting)speedControl);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestMovePlayer()
    {
        const Direction d = Direction.Left;
        var expected = new MovePlayer(d).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendMovePlayer(d);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestMovementRej()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        const byte seq = 100;

        var expected = new MovementRej(seq, m).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendMovementRej(seq, m);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestMovementAck()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        const byte seq = 100;

        var expected = MovementAck.Instantiate(seq, m).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendMovementAck(seq, m);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
