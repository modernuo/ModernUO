using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class DamagePacketTests
{
    [Theory, InlineData(10), InlineData(-5), InlineData(1024)]
    public void TestDamagePacketOld(int inputAmount)
    {
        var serial = (Serial)0x1024;

        PacketAssert.Sends(new DamagePacketOld(serial, inputAmount), ns => ns.SendDamage(serial, inputAmount));
    }

    [Theory, InlineData(10), InlineData(-5), InlineData(1024), InlineData(100000)]
    public void TestDamage(int inputAmount)
    {
        var serial = (Serial)0x1024;

        PacketAssert.Sends(
            new DamagePacket(serial, inputAmount),
            ns => ns.SendDamage(serial, inputAmount),
            ProtocolChanges.DamagePacket
        );
    }
}
