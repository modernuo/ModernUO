using System;
using ModernUO.Serialization;
using Server.Factions;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.Seventh;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class DisguiseKit : Item
{
    public override int LabelNumber => 1041078; // a disguise kit

    [Constructible]
    public DisguiseKit() : base(0xE05)
    {
    }

    public override double DefaultWeight => 1.0;

    public bool ValidateUse(Mobile from)
    {
        var pm = from as PlayerMobile;

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001);
        }
        else if (pm?.NpcGuild != NpcGuild.ThievesGuild)
        {
            from.SendLocalizedMessage(501702);
        }
        else if (Stealing.SuspendOnMurder && pm.Kills > 0)  // could be perma-red: intentional
        {
            from.SendLocalizedMessage(501703);
        }
        else if (!from.CanBeginAction<IncognitoSpell>())
        {
            from.SendLocalizedMessage(501704);
        }
        else if (Sigil.ExistsOn(from))
        {
            from.SendLocalizedMessage(1010465); // You cannot disguise yourself while holding a sigil
        }
        else if (TransformationSpellHelper.UnderTransformation(from))
        {
            from.SendLocalizedMessage(1061634);
        }
        else if (from.BodyMod == 183 || from.BodyMod == 184)
        {
            from.SendLocalizedMessage(1040002);
        }
        else if (!from.CanBeginAction<PolymorphSpell>() || from.IsBodyMod)
        {
            from.SendLocalizedMessage(501705);
        }
        else
        {
            return true;
        }

        return false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (ValidateUse(from))
        {
            DisguiseGump.DisplayTo(from, this, true, false);
        }
    }
}

public class DisguiseGump : DynamicGump
{
    private static readonly DisguiseEntry[] _hairEntries =
    {
        new(8251, 50700, 0, 5, 1011052),  // Short
        new(8261, 60710, 0, 3, 1011047),  // Pageboy
        new(8252, 60708, 0, -5, 1011053), // Long
        new(8264, 60901, 0, 5, 1011048),  // Receding
        new(8253, 60702, 0, -5, 1011054), // Ponytail
        new(8265, 60707, 0, -5, 1011049), // 2-tails
        new(8260, 50703, 0, 5, 1011055),  // Mohawk
        new(8266, 60713, 0, 10, 1011050), // Topknot
        null,
        new(0, 0, 0, 0, 1011051) // None
    };

    private static readonly DisguiseEntry[] _beardEntries =
    {
        new(8269, 50906, 0, 0, 1011401),   // Vandyke
        new(8257, 50808, 0, -2, 1011062),  // Mustache
        new(8255, 50802, 0, 0, 1011060),   // Short beard
        new(8268, 50905, 0, -10, 1011061), // Long beard
        new(8267, 50904, 0, 0, 1011060),   // Short beard
        new(8254, 50801, 0, -10, 1011061), // Long beard
        null,
        new(0, 0, 0, 0, 1011051) // None
    };

    private readonly Mobile _from;
    private readonly DisguiseKit _kit;
    private readonly bool _startAtHair;
    private readonly bool _used;

    public override bool Singleton => true;

    private DisguiseGump(Mobile from, DisguiseKit kit, bool startAtHair, bool used) : base(50, 50)
    {
        _from = from;
        _kit = kit;
        _startAtHair = startAtHair;
        _used = used;
    }

    public static void DisplayTo(Mobile from, DisguiseKit kit, bool startAtHair, bool used)
    {
        if (from?.NetState == null || kit == null || kit.Deleted)
        {
            return;
        }

        from.SendGump(new DisguiseGump(from, kit, startAtHair, used));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage(0);

        builder.AddBackground(100, 10, 400, 385, 2600);

        // <center>THIEF DISGUISE KIT</center>
        builder.AddHtmlLocalized(100, 25, 400, 35, 1011045);

        builder.AddButton(140, 353, 4005, 4007, 0);
        builder.AddHtmlLocalized(172, 355, 90, 35, 1011036); // OKAY

        builder.AddButton(257, 353, 4005, 4007, 1);
        builder.AddHtmlLocalized(289, 355, 90, 35, 1011046); // APPLY

        if (_from.Female || _from.Body.IsFemale)
        {
            DrawEntries(ref builder, 0, 1, -1, _hairEntries, -1);
        }
        else if (_startAtHair)
        {
            DrawEntries(ref builder, 0, 1, 2, _hairEntries, 1011056);
            DrawEntries(ref builder, 1, 2, 1, _beardEntries, 1011059);
        }
        else
        {
            DrawEntries(ref builder, 1, 1, 2, _beardEntries, 1011059);
            DrawEntries(ref builder, 0, 2, 1, _hairEntries, 1011056);
        }
    }

    private static void DrawEntries(ref DynamicGumpBuilder builder, int index, int page, int nextPage, DisguiseEntry[] entries, int nextNumber)
    {
        builder.AddPage(page);

        if (nextPage != -1)
        {
            builder.AddButton(155, 320, 250 + index * 2, 251 + index * 2, 0, GumpButtonType.Page, nextPage);
            builder.AddHtmlLocalized(180, 320, 150, 35, nextNumber);
        }

        for (var i = 0; i < entries.Length; ++i)
        {
            var entry = entries[i];

            if (entry == null)
            {
                continue;
            }

            var x = i % 2 * 205;
            var y = i / 2 * 55;

            if (entry.GumpID != 0)
            {
                builder.AddBackground(220 + x, 60 + y, 50, 50, 2620);
                builder.AddImage(153 + x + entry.OffsetX, 15 + y + entry.OffsetY, entry.GumpID);
            }

            builder.AddHtmlLocalized(140 + x, 72 + y, 80, 35, entry.Number);
            builder.AddRadio(118 + x, 73 + y, 208, 209, false, i * 2 + index);
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0)
        {
            if (_used)
            {
                _from.SendLocalizedMessage(501706); // Disguises wear off after 2 hours.
            }
            else
            {
                _from.SendLocalizedMessage(501707); // You're looking good.
            }

            return;
        }

        var switches = info.Switches;

        if (switches.Length == 0)
        {
            return;
        }

        var switched = switches[0];
        var type = switched % 2;
        var index = switched / 2;

        var hair = type == 0;

        var entries = hair ? _hairEntries : _beardEntries;

        if (index >= 0 && index < entries.Length)
        {
            var entry = entries[index];

            if (entry == null)
            {
                return;
            }

            if (!_kit.ValidateUse(_from))
            {
                return;
            }

            if (!hair && (_from.Female || _from.Body.IsFemale))
            {
                return;
            }

            _from.NameMod = NameList.RandomName(_from.Female ? "female" : "male");

            if (_from is PlayerMobile pm)
            {
                if (hair)
                {
                    pm.SetHairMods(entry.ItemID, -2);
                }
                else
                {
                    pm.SetHairMods(-2, entry.ItemID);
                }
            }

            DisguiseGump.DisplayTo(_from, _kit, hair, true);

            DisguisePersistence.RemoveTimer(_from);

            DisguisePersistence.CreateTimer(_from, TimeSpan.FromHours(2.0));
            DisguisePersistence.StartTimer(_from);
        }
    }

    private class DisguiseEntry
    {
        public int GumpID { get; }
        public int ItemID { get; }
        public int Number { get; }
        public int OffsetX { get; }
        public int OffsetY { get; }

        public DisguiseEntry(int itemID, int gumpID, int ox, int oy, int name)
        {
            ItemID = itemID;
            GumpID = gumpID;
            OffsetX = ox;
            OffsetY = oy;
            Number = name;
        }
    }
}
