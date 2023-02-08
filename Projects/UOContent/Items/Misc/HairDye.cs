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
            from.CloseGump<HairDyeGump>();
            from.SendGump(new HairDyeGump(this));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 906, 1019045); // I can't reach that.
        }
    }
}

public class HairDyeGump : Gump
{
    private static HairDyeEntry[] _entries =
    {
        new("*****", 1602, 26),
        new("*****", 1628, 27),
        new("*****", 1502, 32),
        new("*****", 1302, 32),
        new("*****", 1402, 32),
        new("*****", 1202, 24),
        new("*****", 2402, 29),
        new("*****", 2213, 6),
        new("*****", 1102, 8),
        new("*****", 1110, 8),
        new("*****", 1118, 16),
        new("*****", 1134, 16)
    };

    private HairDye _hairDye;

    public HairDyeGump(HairDye dye) : base(50, 50)
    {
        _hairDye = dye;

        AddPage(0);

        AddBackground(100, 10, 350, 355, 2600);
        AddBackground(120, 54, 110, 270, 5100);

        AddHtmlLocalized(70, 25, 400, 35, 1011013); // <center>Hair Color Selection Menu</center>

        AddButton(149, 328, 4005, 4007, 1);
        AddHtmlLocalized(185, 329, 250, 35, 1011014); // Dye my hair this color!

        for (var i = 0; i < _entries.Length; ++i)
        {
            AddLabel(130, 59 + i * 22, _entries[i].HueStart - 1, _entries[i].Name);
            AddButton(207, 60 + i * 22, 5224, 5224, 0, GumpButtonType.Page, i + 1);
        }

        for (var i = 0; i < _entries.Length; ++i)
        {
            var e = _entries[i];

            AddPage(i + 1);

            for (var j = 0; j < e.HueCount; ++j)
            {
                AddLabel(278 + j / 16 * 80, 52 + j % 16 * 17, e.HueStart + j - 1, "*****");
                AddRadio(260 + j / 16 * 80, 52 + j % 16 * 17, 210, 211, false, i * 100 + j);
            }
        }
    }

    public override void OnResponse(NetState from, RelayInfo info)
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

        if (info.ButtonID != 0 && switches.Length > 0)
        {
            if (m.HairItemID == 0 && m.FacialHairItemID == 0)
            {
                m.SendLocalizedMessage(502623); // You have no hair to dye and cannot use this
            }
            else
            {
                // To prevent this from being exploited, the hue is abstracted into an internal list
                var entryIndex = Math.DivRem(switches[0], 100, out var hueOffset);

                if (entryIndex >= 0 && entryIndex < _entries.Length)
                {
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
        }
        else
        {
            m.SendLocalizedMessage(501200); // You decide not to dye your hair
        }
    }

    private class HairDyeEntry
    {
        public HairDyeEntry(string name, int hueStart, int hueCount)
        {
            Name = name;
            HueStart = hueStart;
            HueCount = hueCount;
        }

        public string Name { get; }

        public int HueStart { get; }

        public int HueCount { get; }
    }
}
