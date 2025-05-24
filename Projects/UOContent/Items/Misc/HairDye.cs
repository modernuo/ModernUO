using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class HairDye : Item
{
    [Constructible]
    public HairDye() : base(0xEFF) => Weight = 1.0;

    public override int LabelNumber => 1041060; // Hair Dye

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 1))
        {
            from.SendGump(new HairDyeGump(this));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 906, 1019045); // I can't reach that.
        }
    }
}

public class HairDyeGump : StaticGump<HairDyeGump>
{
    private static readonly (int HueStart, int HueCount)[] _entries =
    [
        (1602, 26),
        (1628, 27),
        (1502, 32),
        (1302, 32),
        (1402, 32),
        (1202, 24),
        (2402, 29),
        (2213, 6),
        (1102, 8),
        (1110, 8),
        (1118, 16),
        (1134, 16)
    ];

    private readonly HairDye _hairDye;

    public override bool Singleton => true;

    public HairDyeGump(HairDye dye) : base(50, 50) => _hairDye = dye;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(100, 10, 350, 355, 2600);
        builder.AddBackground(120, 54, 110, 270, 5100);

        builder.AddHtmlLocalized(70, 25, 400, 35, 1011013); // <center>Hair Color Selection Menu</center>

        builder.AddButton(149, 328, 4005, 4007, 1);
        builder.AddHtmlLocalized(185, 329, 250, 35, 1011014); // Dye my hair this color!

        ReadOnlySpan<(int HueStart, int HueCount)> entries = _entries;
        for (var i = 0; i < entries.Length; ++i)
        {
            var y = 59 + i * 22;
            builder.AddLabel(130, y, entries[i].HueStart - 1, "*****");
            builder.AddButton(207, y + 1, 5224, 5224, 0, GumpButtonType.Page, i + 1);
        }

        for (var i = 0; i < entries.Length; ++i)
        {
            var (hueStart, hueCount) = entries[i];

            builder.AddPage(i + 1);
            var switchId = i * 100;

            for (var j = 0; j < hueCount; ++j)
            {
                var page = Math.DivRem(j, 16, out var row);
                var x = 260 + page * 80;
                var y = 52 + row * 17;

                builder.AddRadio(x, y, 210, 211, false, switchId + j);
                builder.AddLabel(x + 18, y, hueStart + j - 1, "*****");
            }
        }
    }

    public override void OnResponse(NetState from, in RelayInfo info)
    {
        if (_hairDye.Deleted)
        {
            return;
        }

        var m = from.Mobile;
        var switches = info.Switches;

        if (!_hairDye.IsChildOf(m.Backpack))
        {
            m.SendLocalizedMessage(1042010); // You must have the object in your backpack to use it.
            return;
        }

        if (info.ButtonID == 0 || switches.Length <= 0)
        {
            m.SendLocalizedMessage(501200); // You decide not to dye your hair
            return;
        }

        if (m.HairItemID == 0 && m.FacialHairItemID == 0)
        {
            m.SendLocalizedMessage(502623); // You have no hair to dye and cannot use this
            return;
        }

        // To prevent this from being exploited, the hue is abstracted into an internal list
        var entryIndex = Math.DivRem(switches[0], 100, out var hueOffset);

        if (entryIndex < 0 || entryIndex >= _entries.Length)
        {
            return;
        }

        var e = _entries[entryIndex];

        if (hueOffset >= 0 && hueOffset < e.HueCount)
        {
            var hue = e.HueStart + hueOffset;

            m.HairHue = hue;
            m.FacialHairHue = hue;

            m.SendLocalizedMessage(501199); // You dye your hair
            _hairDye.Delete();
            m.PlaySound(0x4E);
        }
    }
}
