using System;
using Server.Items;
using Server.Network;

namespace Server.Gumps;

public class TithingGump : StaticGump<TithingGump>
{
    // TODO: What's the maximum?
    private const int MaxTitheAmount = 100000;
    private readonly Mobile _from;
    private int _offer;

    public TithingGump(Mobile from) : base(160, 40) => _from = from;

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddImage(30, 30, 102);

        // May your wealth bring blessings to those in need, if tithed upon this most sacred site.
        builder.AddHtmlLocalized(95, 100, 120, 100, 1060198, 0);

        builder.AddHtmlLocalized(57, 274, 50, 20, 3000311); // Gold:
        // AddLabel(57, 274, 0, "Gold:");
        builder.AddLabelPlaceholder(87, 274, 53, "goldOffer");

        builder.AddHtmlLocalized(137, 274, 50, 20, 1079251); // Tithe:
        // AddLabel(137, 274, 0, "Tithe:");
        builder.AddLabelPlaceholder(172, 274, 53, "titheOffer");

        builder.AddButton(105, 230, 5220, 5220, 2);
        builder.AddButton(113, 230, 5222, 5222, 2);
        builder.AddLabel(108, 228, 0, "<");
        builder.AddLabel(112, 228, 0, "<");

        builder.AddButton(127, 230, 5223, 5223, 1);
        builder.AddLabel(131, 228, 0, "<");

        builder.AddButton(147, 230, 5224, 5224, 3);
        builder.AddLabel(153, 228, 0, ">");

        builder.AddButton(168, 230, 5220, 5220, 4);
        builder.AddButton(176, 230, 5222, 5222, 4);
        builder.AddLabel(172, 228, 0, ">");
        builder.AddLabel(176, 228, 0, ">");

        builder.AddButton(217, 272, 4023, 4024, 5);
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        var totalGold = _from.TotalGold;

        // Just in case
        _offer = Math.Clamp(_offer, 0, totalGold);

        builder.SetStringSlot("goldOffer", $"{totalGold - _offer:N0}");
        builder.SetStringSlot("titheOffer", $"{_offer:N0}");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        switch (info.ButtonID)
        {
            case 0:
                {
                    // You have decided to tithe no gold to the shrine.
                    _from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1060193);
                    break;
                }
            case 1:
            case 2:
            case 3:
            case 4:
                {
                    _offer = info.ButtonID switch
                    {
                        1 => _offer - 100,
                        2 => 0,
                        3 => _offer + 100,
                        4 => _from.TotalGold,
                        _ => 0
                    };

                    _from.SendGump(this);
                    break;
                }
            case 5:
                {
                    var totalGold = _from.TotalGold;

                    _offer = Math.Clamp(_offer, 0, totalGold);

                    if (_from.TithingPoints + _offer > MaxTitheAmount)
                    {
                        _offer = MaxTitheAmount - _from.TithingPoints;
                    }

                    if (_offer <= 0)
                    {
                        // You have decided to tithe no gold to the shrine.
                        _from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1060193);
                        break;
                    }

                    var pack = _from.Backpack;

                    // TODO: At some point this was changed on OSI to only work from bank/account funds
                    if (pack?.ConsumeTotal(typeof(Gold), _offer) == true)
                    {
                        // You tithe gold to the shrine as a sign of devotion.
                        _from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1060195);
                        _from.TithingPoints += _offer;

                        _from.PlaySound(0x243);
                        _from.PlaySound(0x2E6);
                    }
                    else
                    {
                        // You do not have enough gold to tithe that amount!
                        _from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1060194);
                    }

                    break;
                }
        }
    }
}
