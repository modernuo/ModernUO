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
        ns.SendHairEquipUpdatePacket(m, (uint)m.HairSerial, m.HairItemID, m.HairHue, Layer.Hair);

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
        ns.SendRemoveHairPacket((uint)m.HairSerial);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void RemoveHairUsesEquippedSerial()
    {
        var m = new Mobile((Serial)0x1025u);
        m.DefaultMobileInit();
        m.HairItemID = 0x2000;

        var equippedSerial = m.HairSerial;
        Assert.NotEqual(Serial.Zero, equippedSerial);

        m.HairItemID = 0; // remove

        // Serial must survive removal so the client can remove the correct entity.
        Assert.Equal(equippedSerial, m.HairSerial);
    }

    [Fact]
    public void RemoveFacialHairUsesEquippedSerial()
    {
        var m = new Mobile((Serial)0x1026u);
        m.DefaultMobileInit();
        m.FacialHairItemID = 0x2040;

        var equippedSerial = m.FacialHairSerial;
        Assert.NotEqual(Serial.Zero, equippedSerial);

        m.FacialHairItemID = 0; // remove

        // Serial must survive removal so the client can remove the correct entity.
        Assert.Equal(equippedSerial, m.FacialHairSerial);
    }
}
