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
    // Weapons take their layer from tiledata, which CI doesn't load; without an explicit layer the
    // equip is refused and the packets would only cover an empty item list.
    private static VikingSword EquipSword(Mobile m)
    {
        var sword = new VikingSword { Layer = Layer.OneHanded };
        var equipped = m.EquipItem(sword);
        if (!equipped)
        {
            sword.Delete();
        }

        Assert.True(equipped);
        return sword;
    }

    [Fact]
    public void TestCorpseEquipPacket()
    {
        var m = new Mobile(World.NewMobile);
        VikingSword weapon = null;
        Corpse c = null;

        try
        {
            m.DefaultMobileInit();
            weapon = EquipSword(m);
            c = new Corpse(m, m.Items);

            var expected = new CorpseEquip(m, c).Compile();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendCorpseEquip(m, c);

            var result = ns.SendBuffer.GetReadSpan();
            AssertThat.Equal(result, expected);
        }
        finally
        {
            c?.Delete();
            weapon?.Delete();
            m.Delete();
        }
    }

    [Theory]
    [InlineData(ProtocolChanges.None)]
    [InlineData(ProtocolChanges.ContainerGridLines)]
    public void TestCorpseContainerPacket(ProtocolChanges changes)
    {
        var m = new Mobile(World.NewMobile);
        VikingSword weapon = null;
        Corpse c = null;

        try
        {
            m.DefaultMobileInit();
            weapon = EquipSword(m);
            c = new Corpse(m, m.Items);

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;

            var expected = (ns.ContainerGridLines ? (Packet)new CorpseContent6017(m, c) : new CorpseContent(m, c)).Compile();

            ns.SendCorpseContent(m, c);

            var result = ns.SendBuffer.GetReadSpan();
            AssertThat.Equal(result, expected);
        }
        finally
        {
            c?.Delete();
            weapon?.Delete();
            m.Delete();
        }
    }
}
