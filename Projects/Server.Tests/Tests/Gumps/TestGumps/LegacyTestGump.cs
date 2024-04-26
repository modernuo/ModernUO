using Server.Gumps;

namespace Server.Tests.Gumps;

public sealed class LegacyTestGump : Gump
{
    public LegacyTestGump(string petName) : base(50, 50)
    {
        Serial = (Serial)0x123;
        TypeID = 0x5345;

        AddPage(0);

        AddBackground(10, 10, 265, 140, 0x242C);

        AddItem(205, 40, 0x4);
        AddItem(227, 40, 0x5);

        AddItem(180, 78, 0xCAE);
        AddItem(195, 90, 0xCAD);
        AddItem(218, 95, 0xCB0);

        AddHtml(30, 30, 150, 75, "<div align=center>Wilt thou sanctify the resurrection of:</div>");
        AddHtml(30, 70, 150, 25, $"<CENTER>{petName}</CENTER>", true);

        AddButton(40, 105, 0x81A, 0x81B, 0x1);  // Okay
        AddButton(110, 105, 0x819, 0x818, 0x2); // Cancel
    }
}
