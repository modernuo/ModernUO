using Server.Engines.Insurance;
using Server.Mobiles;
using Server.Network;

namespace Server.Gumps;

public class ItemInsuranceMenuGump : DynamicGump
{
    private readonly PlayerMobile _from;
    private readonly bool[] _insure;
    private readonly Item[] _items;
    private int _page;

    public override bool Singleton => true;

    public ItemInsuranceMenuGump(PlayerMobile from, Item[] items) : base(25, 50)
    {
        _from = from;
        _items = items;
        _insure = new bool[items.Length];

        for (var i = 0; i < items.Length; ++i)
        {
            _insure[i] = items[i].Insured;
        }
    }

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(0, 0, 520, 510, 0x13BE);
        builder.AddImageTiled(10, 10, 500, 30, 0xA40);
        builder.AddImageTiled(10, 50, 500, 355, 0xA40);
        builder.AddImageTiled(10, 415, 500, 80, 0xA40);
        builder.AddAlphaRegion(10, 10, 500, 485);

        builder.AddButton(15, 470, 0xFB1, 0xFB2, 0);
        builder.AddHtmlLocalized(50, 472, 80, 20, 1011012, 0x7FFF); // CANCEL

        if (_from.AutoRenewInsurance)
        {
            builder.AddButton(360, 10, 9723, 9724, 1);
        }
        else
        {
            builder.AddButton(360, 10, 9720, 9722, 1);
        }

        builder.AddHtmlLocalized(395, 14, 105, 20, 1114122, 0x7FFF); // AUTO REINSURE

        builder.AddButton(395, 470, 0xFA5, 0xFA6, 2);
        builder.AddHtmlLocalized(430, 472, 50, 20, 1006044, 0x7FFF); // OK

        builder.AddHtmlLocalized(10, 14, 150, 20, 1114121, 0x7FFF); // <CENTER>ITEM INSURANCE MENU</CENTER>

        builder.AddHtmlLocalized(45, 54, 70, 20, 1062214, 0x7FFF);  // Item
        builder.AddHtmlLocalized(250, 54, 70, 20, 1061038, 0x7FFF); // Cost
        builder.AddHtmlLocalized(400, 54, 70, 20, 1114311, 0x7FFF); // Insured

        var balance = Banker.GetBalance(_from);
        var cost = 0;

        for (var i = 0; i < _items.Length; ++i)
        {
            if (_insure[i])
            {
                cost += Insurance.GetInsuranceCost(_from, _items[i]);
            }
        }

        builder.AddHtmlLocalized(15, 420, 300, 20, 1114310, 0x7FFF); // GOLD AVAILABLE:
        builder.AddLabel(215, 420, 0x481, $"{balance}");
        builder.AddHtmlLocalized(15, 435, 300, 20, 1114123, 0x7FFF); // TOTAL COST OF INSURANCE:
        builder.AddLabel(215, 435, 0x481, $"{cost}");

        if (cost != 0)
        {
            builder.AddHtmlLocalized(15, 450, 300, 20, 1114125, 0x7FFF); // NUMBER OF DEATHS PAYABLE:
            builder.AddLabel(215, 450, 0x481, $"{balance / cost}");
        }

        for (int i = _page * 4, y = 72; i < (_page + 1) * 4 && i < _items.Length; ++i, y += 75)
        {
            var item = _items[i];
            var b = ItemBounds.Bounds[item.ItemID];

            builder.AddImageTiledButton(
                40,
                y,
                0x918,
                0x918,
                0,
                GumpButtonType.Page,
                0,
                item.ItemID,
                item.Hue,
                40 - b.Width / 2 - b.X,
                30 - b.Height / 2 - b.Y
            );
            builder.AddItemProperty(item.Serial);

            if (_insure[i])
            {
                builder.AddButton(400, y, 9723, 9724, 100 + i);
                builder.AddLabel(250, y, 0x481, $"{Insurance.GetInsuranceCost(_from, item)}");
            }
            else
            {
                builder.AddButton(400, y, 9720, 9722, 100 + i);
                builder.AddLabel(250, y, 0x66C, $"{Insurance.GetInsuranceCost(_from, item)}");
            }
        }

        if (_page >= 1)
        {
            builder.AddButton(15, 380, 0xFAE, 0xFAF, 3);
            builder.AddHtmlLocalized(50, 380, 450, 20, 1044044, 0x7FFF); // PREV PAGE
        }

        if ((_page + 1) * 4 < _items.Length)
        {
            builder.AddButton(400, 380, 0xFA5, 0xFA7, 4);
            builder.AddHtmlLocalized(435, 380, 70, 20, 1044045, 0x7FFF); // NEXT PAGE
        }
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (info.ButtonID == 0 || !_from.CheckAlive())
        {
            return;
        }

        switch (info.ButtonID)
        {
            case 1: // Auto Reinsure
                {
                    if (_from.AutoRenewInsurance)
                    {
                        _from.SendGump(new CancelRenewInventoryInsuranceGump(this));
                    }
                    else
                    {
                        Insurance.AutoRenewInventoryInsurance(_from);
                        _from.SendGump(this);
                    }

                    break;
                }
            case 2: // OK
                {
                    _from.SendGump(new ItemInsuranceMenuConfirmGump(this));

                    break;
                }
            case 3: // Prev
                {
                    if (_page >= 1)
                    {
                        _page--;
                        _from.SendGump(this);
                    }

                    break;
                }
            case 4: // Next
                {
                    if ((_page + 1) * 4 < _items.Length)
                    {
                        _page++;
                        _from.SendGump(this);
                    }

                    break;
                }
            default:
                {
                    var idx = info.ButtonID - 100;

                    if (idx >= 0 && idx < _items.Length)
                    {
                        _insure[idx] = !_insure[idx];
                    }

                    _from.SendGump(this);

                    break;
                }
        }
    }

    public void ToggleSelected()
    {
        var items = _items;
        var insure = _insure;
        for (var i = 0; i < items.Length; ++i)
        {
            var item = items[i];

            if (item.Insured != insure[i])
            {
                Insurance.ToggleItemInsurance(_from, item, false);
            }
        }
    }
}
