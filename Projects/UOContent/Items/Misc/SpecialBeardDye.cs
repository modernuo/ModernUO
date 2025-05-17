using System;
using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class SpecialBeardDye : Item
{
    [Constructible]
    public SpecialBeardDye() : base(0xE26)
    {
        Weight = 1.0;
        LootType = LootType.Newbied;
    }

    public override int LabelNumber => 1041087; // Special Beard Dye

    public override void OnDoubleClick(Mobile from)
    {
        if (from.InRange(GetWorldLocation(), 1))
        {
            from.SendGump(new SpecialBeardDyeGump(this));
        }
        else
        {
            from.LocalOverheadMessage(MessageType.Regular, 906, 1019045); // I can't reach that.
        }
    }
}

public class SpecialBeardDyeGump : StaticGump<SpecialBeardDyeGump>
{
    private static readonly (int HueStart, int HueCount)[] _entries =
    [
        (12, 10),
        (32, 5),
        (38, 8),
        (54, 3),
        (62, 10),
        (81, 2),
        (89, 2),
        (1153, 2)
    ];

    private readonly SpecialBeardDye _specialBeardDye;

    public override bool Singleton => true;

    public SpecialBeardDyeGump(SpecialBeardDye dye) : base(0, 0) => _specialBeardDye = dye;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(150, 60, 350, 358, 2600);
        builder.AddBackground(170, 104, 110, 270, 5100);

        builder.AddHtmlLocalized(230, 75, 200, 20, 1011013);  // <center>Hair Color Selection Menu</center>
        builder.AddHtmlLocalized(235, 380, 300, 20, 1013007); // Dye my beard this color!
        builder.AddButton(200, 380, 0xFA5, 0xFA7, 1); // DYE HAIR

        ReadOnlySpan<(int HueStart, int HueCount)> entries = _entries;
        for (var i = 0; i < entries.Length; ++i)
        {
            builder.AddLabel(180, 109 + i * 22, entries[i].HueStart - 1, "*****");
            builder.AddButton(257, 110 + i * 22, 5224, 5224, 0, GumpButtonType.Page, i + 1);
        }

        for (var i = 0; i < entries.Length; ++i)
        {
            var (hueStart, hueCount) = entries[i];

            builder.AddPage(i + 1);
            var switchId = i * 100;

            for (var j = 0; j < hueCount; ++j)
            {
                var page = Math.DivRem(j, 16, out var row);
                var x = 310 + page * 80;
                var y = 102 + row * 17;

                builder.AddRadio(x, y, 210, 211, false, switchId + j);
                builder.AddLabel(x + 18, y, hueStart + j - 1, "*****");
            }
        }
    }

    public override void OnResponse(NetState from, in RelayInfo info)
    {
        if (_specialBeardDye.Deleted)
        {
            return;
        }

        var m = from.Mobile;
        var switches = info.Switches;

        if (!_specialBeardDye.IsChildOf(m.Backpack))
        {
            m.SendLocalizedMessage(1042010); // You must have the object in your backpack to use it.
            return;
        }

        if (info.ButtonID == 0 || switches.Length <= 0)
        {
            m.SendLocalizedMessage(501200); // You decide not to dye your hair
            return;
        }

        if (m.FacialHairItemID == 0)
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

            m.FacialHairHue = hue;

            m.SendLocalizedMessage(501199); // You dye your hair
            _specialBeardDye.Delete();
            m.PlaySound(0x4E);
        }
    }
}
