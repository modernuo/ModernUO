using ModernUO.Serialization;
using Server.Engines.MLQuests.Gumps;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RaceChangeToken : Item, IRaceChanger
{
    [Constructible]
    public RaceChangeToken() : base(0x2AAA)
    {
        LootType = LootType.Blessed;
        Light = LightType.Circle300;
    }

    public override double DefaultWeight => 5.0;

    public override int LabelNumber => 1070997; // A promotional token

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.AddLocalized(1070998, 1113656); // Use this to redeem<br>Your ~1_PROMO~ : race change
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
        }
        else if (from is PlayerMobile pm)
        {
            RaceChangeTokenGump.DisplayTo(pm, this);
        }
    }

    public override void OnRemoved(IEntity parent)
    {
        Mobile m = null;

        if (parent is Item item)
        {
            m = item.RootParent as Mobile;
        }
        else if (parent is Mobile mobile)
        {
            m = mobile;
        }

        m?.CloseGump<RaceChangeTokenGump>();
    }

    public bool CheckComplete(PlayerMobile pm)
    {
        if (Deleted)
        {
            return false;
        }

        if (!IsChildOf(pm.Backpack))
        {
            pm.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            return false;
        }

        return true;
    }

    public void ConsumeNeeded(PlayerMobile pm) => Consume();

    public void OnCancel(PlayerMobile pm)
    {
    }

    public void OnSuccess(PlayerMobile pm, Race targetRace)
    {
        pm.SendLocalizedMessage(1111914); // You have successfully changed your race.
    }
}

public class RaceChangeTokenGump : DynamicGump
{
    public enum GumpMode
    {
        Confirm,
        Select
    }

    private readonly PlayerMobile _from;
    private readonly RaceChangeToken _token;
    private readonly GumpMode _mode;

    public override bool Singleton => true;

    public RaceChangeTokenGump(PlayerMobile from, RaceChangeToken token, GumpMode mode = GumpMode.Confirm) : base(50, 50)
    {
        _from = from;
        _token = token;
        _mode = mode;
    }

    public static void DisplayTo(PlayerMobile from, RaceChangeToken token)
    {
        if (from?.NetState == null || token?.Deleted != false)
        {
            return;
        }

        from.SendGump(new RaceChangeTokenGump(from, token));
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();
        builder.AddBackground(0, 0, 291, 159, 9200);
        builder.AddImageTiled(5, 5, 281, 20, 2702);
        builder.AddImageTiled(5, 30, 281, 100, 2702);

        builder.AddHtmlLocalized(8, 5, 279, 20, 1113641, 0x7FFF); // Change your character's race.

        builder.AddButton(5, 132, 0xFB1, 0xFB3, 0);
        builder.AddHtmlLocalized(40, 132, 100, 20, 1011012, 0x7FFF); // CANCEL

        if (_mode == GumpMode.Confirm)
        {
            builder.AddHtmlLocalized(8, 30, 279, 124, 1113642, 0x7FFF); // Click OK to change your character's race. This change is permanent.

            builder.AddButton(126, 132, 0xFB7, 0xFB9, 1);
            builder.AddHtmlLocalized(160, 132, 120, 20, 1113643, 0x7FFF); // CHANGE RACE
        }
        else
        {
            builder.AddHtmlLocalized(8, 30, 279, 40, 1113657, 0x7FFF);

            var y = 80;

            if (_from.Race != Race.Human)
            {
                builder.AddButton(8, y, 0xFB7, 0xFB9, 2);
                builder.AddHtmlLocalized(100, y, 150, 20, 1111906, 0x7FFF); // Make me a human!
                y += 25;
            }

            if (_from.Race != Race.Elf &&
                (_from.NetState == null || _from.NetState.SupportsExpansion(Expansion.ML)))
            {
                builder.AddButton(8, y, 0xFB7, 0xFB9, 3);
                builder.AddHtmlLocalized(100, y, 150, 20, 1111910, 0x7FFF); // Make me an elf!
                y += 25;
            }

            if (_from.Race != Race.Gargoyle &&
                (_from.NetState == null || _from.NetState.SupportsExpansion(Expansion.SA)))
            {
                builder.AddButton(8, y, 0xFB7, 0xFB9, 4);
                builder.AddHtmlLocalized(100, y, 150, 20, 1111900, 0x7FFF); // Make me a gargoyle!
                y += 25;
            }
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (_token?.Deleted != false || sender.Mobile != _from)
        {
            return;
        }

        if (!_token.IsChildOf(_from.Backpack))
        {
            _from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            return;
        }

        switch (info.ButtonID)
        {
            case 0:
                {
                    break;
                }
            case 1 when _mode == GumpMode.Confirm:
                {
                    _from.SendGump(new RaceChangeTokenGump(_from, _token, GumpMode.Select));
                    break;
                }
            case 2:
            case 3:
            case 4:
                {
                    var race = info.ButtonID switch
                    {
                        2 => Race.Human,
                        3 => Race.Elf,
                        _ => Race.Gargoyle
                    };

                    RaceChangeConfirmGump.TryOffer(_from, _token, race);
                    break;
                }
        }
    }
}