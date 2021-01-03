using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
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

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendCloseGump(typeId, buttonId);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestDisplaySignGump()
        {
            Serial gumpSerial = 0x1000;
            var gumpId = 100;
            var unknownString = "This is an unknown string";
            var caption = "This is a caption";

            var data = new DisplaySignGump(gumpSerial, gumpId, unknownString, caption).Compile();

            Span<byte> expectedData = stackalloc byte[15 + unknownString.Length + caption.Length];
            var pos = 0;

            expectedData.Write(ref pos, (byte)0x8B);
            expectedData.Write(ref pos, (ushort)expectedData.Length);
            expectedData.Write(ref pos, gumpSerial);
            expectedData.Write(ref pos, (ushort)gumpId);
            expectedData.Write(ref pos, (ushort)(unknownString.Length + 1));
            expectedData.WriteAsciiNull(ref pos, unknownString);
            expectedData.Write(ref pos, (ushort)(caption.Length + 1));
            expectedData.WriteAsciiNull(ref pos, caption);

            AssertThat.Equal(data, expectedData);
        }

        [Theory]
        [InlineData(ProtocolChanges.None)]
        [InlineData(ProtocolChanges.Unpack)]
        public void TestGumpPacketSimple(ProtocolChanges changes)
        {
            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;
            var gump = new ResurrectGump(2);

            var expected = gump.Compile(ns).Compile();

            ns.SendDisplayGump(gump);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Theory]
        [InlineData(ProtocolChanges.None)]
        [InlineData(ProtocolChanges.Unpack)]
        public void TestGumpPacketNameChange(ProtocolChanges changes)
        {
            var gump = new NameChangeDeedGump();

            using var ns = PacketTestUtilities.CreateTestNetState();
            ns.ProtocolChanges = changes;

            var expected = gump.Compile(ns).Compile();

            ns.SendDisplayGump(gump);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }

    public class ResurrectGump : Gump
    {
        public ResurrectGump(int msg) : base(100, 0)
        {
            AddPage(0);

            AddBackground(0, 0, 400, 350, 2600);

            AddHtmlLocalized(0, 20, 400, 35, 1011022); // <center>Resurrection</center>

            /* It is possible for you to be resurrected here by this healer. Do you wish to try?<br>
             * CONTINUE - You chose to try to come back to life now.<br>
             * CANCEL - You prefer to remain a ghost for now.
             */
            AddHtmlLocalized(50, 55, 300, 140, 1011023 + msg, true, true);

            AddButton(200, 227, 4005, 4007, 0);
            AddHtmlLocalized(235, 230, 110, 35, 1011012); // CANCEL

            AddButton(65, 227, 4005, 4007, 1);
            AddHtmlLocalized(100, 230, 110, 35, 1011011); // CONTINUE
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

        public string Center(string text) => $"<CENTER>{text}</CENTER>";

        public string Color(string text, int color) => $"<BASEFONT COLOR=#{color:X6}>{text}</BASEFONT>";

        public void AddButtonLabeled(int x, int y, int buttonID, string text)
        {
            AddButton(x, y - 1, 4005, 4007, buttonID);
            AddHtml(x + 35, y, 240, 20, Color(text, 0xFFFFFF));
        }
    }
}
