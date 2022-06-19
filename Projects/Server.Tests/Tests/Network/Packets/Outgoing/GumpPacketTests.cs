using System;
using Server.Gumps;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    public class GumpPacketTests : IClassFixture<ServerFixture>
    {
        [Theory]
        [InlineData(100, 10)]
        public void TestCloseGump(int typeId, int buttonId)
        {
            var expected = new CloseGump(typeId, buttonId).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendCloseGump(typeId, buttonId);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestDisplaySignGump()
        {
            Serial gumpSerial = (Serial)0x1000;
            const int gumpId = 100;
            const string unknownString = "This is an unknown string";
            const string caption = "This is a caption";

            var expected = new DisplaySignGump(gumpSerial, gumpId, unknownString, caption).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendDisplaySignGump(gumpSerial, gumpId, unknownString, caption);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.None)]
        [InlineData(ProtocolChanges.Unpack)]
        public void TestGumpPacketNameChange(ProtocolChanges changes)
        {
            var gump = new NameChangeDeedGump();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;

            var expected = gump.Compile(ns).Compile();

            ns.SendDisplayGump(gump, out _, out _);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.None)]
        [InlineData(ProtocolChanges.Unpack)]
        public void TestGumpPacketAdmin(ProtocolChanges changes)
        {
            var m = new Mobile((Serial)0x1);
            m.DefaultMobileInit();
            m.RawName = "Test Mobile";
            m.AccessLevel = AccessLevel.Administrator;

            var gump = new AdminGump(m, AdminGumpPage.Clients);

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;

            var expected = gump.Compile(ns).Compile();

            ns.SendDisplayGump(gump, out _, out _);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }

    public class NameChangeDeedGump : Gump
    {
        public NameChangeDeedGump() : base(50, 50)
        {
            Closable = false;
            Draggable = false;
            Resizable = false;

            AddPage(0);

            AddBlackAlpha(10, 120, 250, 85);
            AddHtml(10, 125, 250, 20, Color(Center("Name Change Deed"), 0xFFFFFF));

            AddLabel(73, 15, 1152, "");
            AddLabel(20, 150, 0x480, "New Name:");
            AddTextField(100, 150, 150, 20, 0);

            AddButtonLabeled(75, 180, 1, "Submit");
        }

        public void AddBlackAlpha(int x, int y, int width, int height)
        {
            AddImageTiled(x, y, width, height, 2624);
            AddAlphaRegion(x, y, width, height);
        }

        public void AddTextField(int x, int y, int width, int height, int index)
        {
            AddBackground(x - 2, y - 2, width + 4, height + 4, 0x2486);
            AddTextEntry(x + 2, y + 2, width - 4, height - 4, 0, index, "");
        }

        public static string Center(string text) => $"<CENTER>{text}</CENTER>";

        public static string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

        public void AddButtonLabeled(int x, int y, int buttonID, string text)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID);
            AddHtml(x + 35, y, 240, 20, Color(text, 0xFFFFFF));
        }
    }
}
