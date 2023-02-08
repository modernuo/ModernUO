using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpecialHairDye : Item
{
    [Constructible]
    public SpecialHairDye() : base(0xE26)
    {
        Weight = 1.0;
        LootType = LootType.Newbied;
    }

    public override int LabelNumber => 1074402;

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 1))
        {
            from.CloseGump<SpecialHairDyeGump>();
            from.SendGump(new SpecialHairDyeGump(this));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 906, 1019045); // I can't reach that.
        }
    }
}

public class SpecialHairDyeGump : Gump
{
    private static readonly SpecialHairDyeEntry[] _entries =
    {
        new("*****", 12, 10),
        new("*****", 32, 5),
        new("*****", 38, 8),
        new("*****", 54, 3),
        new("*****", 62, 10),
        new("*****", 81, 2),
        new("*****", 89, 2),
        new("*****", 1153, 2)
    };

    private SpecialHairDye _specialHairDye;

    public SpecialHairDyeGump(SpecialHairDye dye) : base(0, 0)
    {
        _specialHairDye = dye;

        AddPage(0);
        AddBackground(150, 60, 350, 358, 2600);
        AddBackground(170, 104, 110, 270, 5100);
        AddHtmlLocalized(230, 75, 200, 20, 1011013);  // Hair Color Selection Menu
        AddHtmlLocalized(235, 380, 300, 20, 1011014); // Dye my hair this color!
        AddButton(200, 380, 0xFA5, 0xFA7, 1);         // DYE HAIR

        for (var i = 0; i < _entries.Length; ++i)
        {
            AddLabel(180, 109 + i * 22, _entries[i].HueStart - 1, _entries[i].Name);
            AddButton(257, 110 + i * 22, 5224, 5224, 0, GumpButtonType.Page, i + 1);
        }

        for (var i = 0; i < _entries.Length; ++i)
        {
            var e = _entries[i];

            AddPage(i + 1);

            for (var j = 0; j < e.HueCount; ++j)
            {
                AddLabel(328 + j / 16 * 80, 102 + j % 16 * 17, e.HueStart + j - 1, "*****");
                AddRadio(310 + j / 16 * 80, 102 + j % 16 * 17, 210, 211, false, i * 100 + j);
            }
        }
    }

    public override void OnResponse(NetState from, RelayInfo info)
    {
        if (_specialHairDye.Deleted)
        {
            return;
        }

        var m = from.Mobile;
        var switches = info.Switches;

        if (!_specialHairDye.IsChildOf(m.Backpack))
        {
            m.SendLocalizedMessage(1042010); // You must have the objecti n your backpack to use it.
            return;
        }

        if (info.ButtonID != 0 && switches.Length > 0)
        {
            if (m.HairItemID == 0)
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
                        _specialHairDye.Delete();

                        var hue = e.HueStart + hueOffset;

                        m.HairHue = hue;

                        m.SendLocalizedMessage(501199); // You dye your hair
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

    private class SpecialHairDyeEntry
    {
        public SpecialHairDyeEntry(string name, int hueStart, int hueCount)
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
