using Server.Gumps;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class GumpPacketTests
{
    [Theory]
    [InlineData(100, 10)]
    public void TestCloseGump(int typeId, int buttonId)
    {
        var expected = new CloseGump(typeId, buttonId).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendCloseGump(typeId, buttonId);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestDisplaySignGump()
    {
        var gumpSerial = (Serial)0x1000;
        const int gumpId = 100;
        const string unknownString = "This is an unknown string";
        const string caption = "This is a caption";

        var expected = new DisplaySignGump(gumpSerial, gumpId, unknownString, caption).Compile();

        using var ns = PacketTestUtilities.CreateTestNetState();
        ns.SendDisplaySignGump(gumpSerial, gumpId, unknownString, caption);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestGumpPacketNameChange()
    {
        var gump = new NameChangeDeedGump();

        using var ns = PacketTestUtilities.CreateTestNetState();

        var expected = gump.Compile(ns).Compile();
        ns.SendGump(gump);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
    }

    [Fact]
    public void TestGumpPacketAdmin()
    {
        var m = new Mobile((Serial)0x1);
        m.DefaultMobileInit();
        m.RawName = "Test Mobile";
        m.AccessLevel = AccessLevel.Administrator;

        var gump = new AdminGump(m, AdminGumpPage.Clients);

        using var ns = PacketTestUtilities.CreateTestNetState();

        var expected = gump.Compile(ns).Compile();
        ns.SendGump(gump);

        var result = ns.SendBuffer.GetReadSpan();
        AssertThat.Equal(result, expected);
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
        AddHtml(10, 125, 250, 20, "Name Change Deed".Center(0xFFFFFF));

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

    public void AddButtonLabeled(int x, int y, int buttonID, string text)
    {
        AddButton(x, y - 1, 4005, 4007, buttonID);
        AddHtml(x + 35, y, 240, 20, text.Color(0xFFFFFF));
    }
}
