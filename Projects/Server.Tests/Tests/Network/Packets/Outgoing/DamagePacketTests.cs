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

        var expected = new DamagePacketOld(serial, inputAmount).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendDamage(serial, inputAmount);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Theory, InlineData(10), InlineData(-5), InlineData(1024), InlineData(100000)]
    public void TestDamage(int inputAmount)
    {
        var serial = (Serial)0x1024;

        var expected = new DamagePacket(serial, inputAmount).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.ProtocolChanges = ProtocolChanges.DamagePacket;

        ns.SendDamage(serial, inputAmount);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
