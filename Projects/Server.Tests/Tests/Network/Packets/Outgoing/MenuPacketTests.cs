using Server.Collections;
using Server.ContextMenus;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

internal class ContextMenuItem : Item
{
    private readonly bool _requiresNewPacket;
    public ContextMenuItem(Serial serial, bool requiresNewPacket) : base(serial) =>
        _requiresNewPacket = requiresNewPacket;

    public override void GetContextMenuEntries(Mobile from, ref PooledRefList<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, ref list);

        list.Add(new ContextMenuEntry(3000001));
        list.Add(new ContextMenuEntry(3000002));
        list.Add(new ContextMenuEntry(3000003));
        if (_requiresNewPacket)
        {
            list.Add(new ContextMenuEntry(500000));
        }
    }
}

[Collection("Sequential Server Tests")]
public class MenuPacketTests
{
    [Fact]
    public void TestDisplayItemListMenu()
    {
        var menu = new ItemListMenu(
            "Which item would you choose?",
            [
                new ItemListEntry("Item 1", 0x01),
                new ItemListEntry("Item 2", 0x100),
                new ItemListEntry("Item 3", 0x1000, 250)
            ]
        );

        PacketAssert.Sends(new DisplayItemListMenu(menu), ns => ns.SendDisplayItemListMenu(menu));
    }

    [Fact]
    public void TestDisplayQuestionMenu()
    {
        var menu = new QuestionMenu(
            "Which option would you choose?",
            [
                "Option 1",
                "Option 2",
                "Option 3"
            ]
        );

        PacketAssert.Sends(new DisplayQuestionMenu(menu), ns => ns.SendDisplayQuestionMenu(menu));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void TestDisplayContextMenu(bool newHaven, bool newPacket)
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();

        var item = new ContextMenuItem(World.NewItem, newPacket);
        var menu = ContextMenuSystem.CreateContextMenu(m, item);

        var expected = newHaven && newPacket ? (Packet)new DisplayContextMenu(menu) : new DisplayContextMenuOld(menu);

        PacketAssert.Sends(
            expected,
            ns => ns.SendDisplayContextMenu(menu),
            newHaven ? ProtocolChanges.NewHaven : ProtocolChanges.None
        );
    }
}
