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

        PacketAssert.Sends(new Swing(attacker, defender), ns => ns.SendSwing(attacker, defender));
    }

    [Theory, InlineData(true), InlineData(false)]
    public void TestSetWarMode(bool warmode)
    {
        PacketAssert.Sends(new SetWarMode(warmode), ns => ns.SendSetWarMode(warmode));
    }

    [Fact]
    public void TestChangeCombatant()
    {
        var serial = (Serial)0x1024;

        PacketAssert.Sends(new ChangeCombatant(serial), ns => ns.SendChangeCombatant(serial));
    }
}
