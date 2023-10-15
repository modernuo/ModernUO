using System.Collections.Generic;
using Server.ContextMenus;
using Server.Menus.ItemLists;
using Server.Menus.Questions;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    internal class ContextMenuItem : Item
    {
        private bool _requiresNewPacket;
        public ContextMenuItem(Serial serial, bool requiresNewPacket) : base(serial) =>
            _requiresNewPacket = requiresNewPacket;

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            list.Add(new ContextMenuEntry(3000001));
            list.Add(new ContextMenuEntry(3000002));
            list.Add(new ContextMenuEntry(3000003));
            if (_requiresNewPacket)
            {
                list.Add(new ContextMenuEntry(500000));
            }
        }
    }

    [Collection("Sequential Tests")]
    public class MenuPacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestDisplayItemListMenu()
        {
            var menu = new ItemListMenu(
                "Which item would you choose?",
                new[]
                {
                    new ItemListEntry("Item 1", 0x01),
                    new ItemListEntry("Item 2", 0x100),
                    new ItemListEntry("Item 3", 0x1000, 250)
                }
            );

            var expected = new DisplayItemListMenu(menu).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplayItemListMenu(menu);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }

        [Fact]
        public void TestDisplayQuestionMenu()
        {
            var menu = new QuestionMenu(
                "Which option would you choose?",
                new[]
                {
                    "Option 1",
                    "Option 2",
                    "Option 3"
                }
            );

            var expected = new DisplayQuestionMenu(menu).Compile();
            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplayQuestionMenu(menu);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
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
            var menu = new ContextMenu(m, item);

            var packet = newHaven && newPacket ? (Packet)new DisplayContextMenu(menu) : new DisplayContextMenuOld(menu);
            var expected = packet.Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            if (newHaven)
            {
                ns.ProtocolChanges |= ProtocolChanges.NewHaven;
            }

            ns.SendDisplayContextMenu(menu);

            var result = ns.SendPipe.Reader.AvailableToRead();
            AssertThat.Equal(result, expected);
        }
    }
}
