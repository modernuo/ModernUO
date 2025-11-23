using Server.Gumps;

namespace Server.Tests.Gumps;

public class DynamicTestGump : DynamicGump
{
    private readonly string _petName;

    public DynamicTestGump(string petName) : base(50, 50)
    {
        _petName = petName;
        Serial = (Serial)0x123;
        TypeID = 0x5345;
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(10, 10, 265, 140, 0x242C);

        builder.AddItem(205, 40, 0x4);
        builder.AddItem(227, 40, 0x5);

        builder.AddItem(180, 78, 0xCAE);
        builder.AddItem(195, 90, 0xCAD);
        builder.AddItem(218, 95, 0xCB0);

        builder.AddHtml(30, 30, 150, 75, "<div align=center>Wilt thou sanctify the resurrection of:</div>");
        builder.AddHtml(30, 70, 150, 25, _petName, align: TextAlignment.Center, background: true);

        builder.AddButton(40, 105, 0x81A, 0x81B, 0x1);  // Okay
        builder.AddButton(110, 105, 0x819, 0x818, 0x2); // Cancel
    }
}
