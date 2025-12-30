using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.BulkOrders;

public class BODBuyGump : StaticGump<BODBuyGump>
{
    private BOBGump _gump;
    private readonly IBOBEntry _entry;
    private readonly int _price;

    public BODBuyGump(BOBGump gump, IBOBEntry entry, int price) : base(100, 200)
    {
        _gump = gump;
        _entry = entry;
        _price = price;
    }

    protected override void BuildLayout(ref StaticGumpBuilder builder)
    {
        builder.AddPage();

        builder.AddBackground(100, 10, 300, 150, 5054);

        builder.AddHtmlLocalized(125, 20, 250, 24, 1019070); // You have agreed to purchase:
        builder.AddHtmlLocalized(125, 45, 250, 24, 1045151); // a bulk order deed

        builder.AddHtmlLocalized(125, 70, 250, 24, 1019071); // for the amount of:
        builder.AddLabelPlaceholder(125, 95, 0, "price");

        builder.AddButton(250, 130, 4005, 4007, 1);
        builder.AddHtmlLocalized(282, 130, 100, 24, 1011012); // CANCEL

        builder.AddButton(120, 130, 4005, 4007, 2);
        builder.AddHtmlLocalized(152, 130, 100, 24, 1011036); // OKAY
    }

    protected override void BuildStrings(ref GumpStringsBuilder builder)
    {
        builder.SetStringSlot("price", $"{_price:N0}");
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        if (sender.Mobile is not PlayerMobile pm)
        {
            return;
        }

        if (info.ButtonID != 2)
        {
            pm.SendLocalizedMessage(503207); // Cancelled purchase.
            return;
        }

        var book = _gump.Book;

        if (book.RootParent is not PlayerVendor pv)
        {
            pm.SendLocalizedMessage(1062382); // The deed selected is not available.
            return;
        }

        if (!book.Entries.Contains(_entry))
        {
            pv.SayTo(pm, 1062382); // The deed selected is not available.
            return;
        }

        var price = 0;

        if (pv.GetVendorItem(book)?.IsForSale == false)
        {
            price = _entry.Price;
        }

        if (price != _price)
        {
            pv.SayTo(
                pm,
                "The price has been been changed. If you like, you may offer to purchase the item again."
            );
            return;
        }

        if (price == 0)
        {
            pv.SayTo(pm, 1062382); // The deed selected is not available.
            return;
        }

        var item = _entry.Reconstruct();

        pv.Say(pm.Name);

        var pack = pm.Backpack;

        if (pack?.CheckHold(
                pm,
                item,
                true,
                true,
                0,
                item.PileWeight + item.TotalWeight
            ) != true)
        {
            pv.SayTo(pm, 503204); // You do not have room in your backpack for this
            pm.SendGump(_gump);
            item.Delete();
        }
        else if (pack.ConsumeTotal(typeof(Gold), price) || Banker.Withdraw(pm, price))
        {
            book.RemoveEntry(_entry);
            pv.HoldGold += price;
            pm.AddToBackpack(item);

            // The bulk order deed has been placed in your backpack.
            pm.SendLocalizedMessage(1045152);

            if (book.Entries.Count / 5 < book.ItemCount)
            {
                book.ItemCount--;
                book.InvalidateItems();
            }

            if (book.Entries.Count > 0)
            {
                _gump.ResetList();
                pm.SendGump(_gump);
            }
            else
            {
                pm.SendLocalizedMessage(1062381); // The book is empty.
            }
        }
        else
        {
            pv.SayTo(pm, 503205); // You cannot afford this item.
            item.Delete();
        }
    }
}
