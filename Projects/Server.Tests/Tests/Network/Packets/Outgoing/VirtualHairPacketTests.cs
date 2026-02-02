using Server.Network;
using Server.Tests.Network;
using Xunit;

namespace Server.Tests;

[Collection("Sequential Server Tests")]
public class VirtualHairPacketTests
{
    [Fact]
    public void TestSendVirtualHairUpdate()
    {
        var m = new Mobile((Serial)0x1024u);
        m.DefaultMobileInit();
        m.HairHue = 0x1000;
        m.HairItemID = 0x2000;

        var expected = new HairEquipUpdate(m).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendHairEquipUpdatePacket(m, (uint)m.Hair.VirtualSerial, m.Hair.ItemId, m.Hair.Hue, Layer.Hair);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestSendRemoveVirtualHair()
    {
        var m = new Mobile((Serial)0x1024u);
        m.DefaultMobileInit();

        var expected = new RemoveHair(m).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendRemoveHairPacket((uint) m.Hair.VirtualSerial);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
