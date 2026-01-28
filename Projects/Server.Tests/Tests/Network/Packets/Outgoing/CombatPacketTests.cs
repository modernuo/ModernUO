using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class CombatPacketTests
{
    [Fact]
    public void TestSwing()
    {
        var attacker = (Serial)0x1024;
        var defender = (Serial)0x2048;

        var expected = new Swing(attacker, defender).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendSwing(attacker, defender);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Theory, InlineData(true), InlineData(false)]
    public void TestSetWarMode(bool warmode)
    {
        var expected = new SetWarMode(warmode).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendSetWarMode(warmode);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestChangeCombatant()
    {
        var serial = (Serial)0x1024;

        var expected = new ChangeCombatant(serial).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendChangeCombatant(serial);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
