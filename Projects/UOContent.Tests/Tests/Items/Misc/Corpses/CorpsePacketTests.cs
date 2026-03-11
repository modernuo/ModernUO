using Server;
using Server.Items;
using Server.Network;
using Server.Tests;
using Server.Tests.Network;
using Xunit;

namespace UOContent.Tests;

[Collection("Sequential UOContent Tests")]
public class CorpsePacketTests
{
    [Fact]
    public void TestCorpseEquipPacket()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var weapon = new VikingSword();
        m.EquipItem(weapon);

        var c = new Corpse(m, m.Items);

        var expected = new CorpseEquip(m, c).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendCorpseEquip(m, c);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Theory]
    [InlineData(ProtocolChanges.None)]
    [InlineData(ProtocolChanges.ContainerGridLines)]
    public void TestCorpseContainerPacket(ProtocolChanges changes)
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var weapon = new VikingSword();
        m.EquipItem(weapon);

        var c = new Corpse(m, m.Items);

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.ProtocolChanges = changes;

        var expected = (ns.ContainerGridLines ? (Packet)new CorpseContent6017(m, c) : new CorpseContent(m, c)).Compile();

        ns.SendCorpseContent(m, c);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }
}
