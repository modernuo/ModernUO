using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;

namespace Server.Engines.BulkOrders;

public class BOBGump : DynamicGump
{
    private const int LabelColor = 0x7FFF;

    private readonly PlayerMobile _from;
    private int _page;

    public BulkOrderBook Book { get; }
    public List<IBOBEntry> List { get; private set; }
    public override bool Singleton => true;

    public BOBGump(PlayerMobile from, BulkOrderBook book) : base(12, 24)
    {
        _from = from;
        Book = book;
    }

    public void ResetList() => List = null;

    protected override void BuildLayout(ref DynamicGumpBuilder builder)
    {
        if (List == null)
        {
            List = new List<IBOBEntry>(Book.Entries.Count);

            for (var i = 0; i < Book.Entries.Count; ++i)
            {
                var entry = Book.Entries[i];

                if (CheckFilter(entry))
                {
                    List.Add(entry);
                }
            }
        }

        var index = GetIndexForPage(_page);
        var count = GetCountForIndex(index);

        var tableIndex = 0;

        var canDrop = Book.IsChildOf(_from.Backpack);
        var pv = Book.RootParent as PlayerVendor;
        var canBuy = pv != null;
        var canPrice = canDrop || canBuy;

        if (canBuy)
        {
            var vi = pv.GetVendorItem(Book);

            canBuy = vi?.IsForSale == false;
        }

        var width = 600;

        if (!canPrice)
        {
            width = 516;
        }

        X = (624 - width) / 2;

        builder.AddPage();

        builder.AddBackground(10, 10, width, 439, 5054);
        builder.AddImageTiled(18, 20, width - 17, 420, 2624);

        if (canPrice)
        {
            builder.AddImageTiled(573, 64, 24, 352, 200);
            builder.AddImageTiled(493, 64, 78, 352, 1416);
        }

        if (canDrop)
        {
            builder.AddImageTiled(24, 64, 32, 352, 1416);
        }

        builder.AddImageTiled(58, 64, 36, 352, 200);
        builder.AddImageTiled(96, 64, 133, 352, 1416);
        builder.AddImageTiled(231, 64, 80, 352, 200);
        builder.AddImageTiled(313, 64, 100, 352, 1416);
        builder.AddImageTiled(415, 64, 76, 352, 200);

        for (var i = index; i < index + count && i >= 0 && i < List.Count; ++i)
        {
            var entry = List[i];

            if (!CheckFilter(entry))
            {
                continue;
            }

            builder.AddImageTiled(24, 94 + tableIndex * 32, canPrice ? 573 : 489, 2, 2624);
            tableIndex += entry is BOBLargeEntry largeEntry ? largeEntry.Entries.Length : 1;
        }

        builder.AddAlphaRegion(18, 20, width - 17, 420);
        builder.AddImage(5, 5, 10460);
        builder.AddImage(width - 15, 5, 10460);
        builder.AddImage(5, 424, 10460);
        builder.AddImage(width - 15, 424, 10460);

        builder.AddHtmlLocalized(canPrice ? 266 : 224, 32, 200, 32, 1062220, LabelColor); // Bulk Order Book
        builder.AddHtmlLocalized(63, 64, 200, 32, 1062213, LabelColor);                   // Type
        builder.AddHtmlLocalized(147, 64, 200, 32, 1062214, LabelColor);                  // Item
        builder.AddHtmlLocalized(246, 64, 200, 32, 1062215, LabelColor);                  // Quality
        builder.AddHtmlLocalized(336, 64, 200, 32, 1062216, LabelColor);                  // Material
        builder.AddHtmlLocalized(429, 64, 200, 32, 1062217, LabelColor);                  // Amount

        builder.AddButton(35, 32, 4005, 4007, 1);
        builder.AddHtmlLocalized(70, 32, 200, 32, 1062476, LabelColor); // Set Filter

        var f = _from.UseOwnFilter ? _from.BOBFilter : Book.Filter;

        if (f.IsDefault)
        {
            builder.AddHtmlLocalized(canPrice ? 470 : 386, 32, 120, 32, 1062475, 16927); // Using No Filter
        }
        else if (_from.UseOwnFilter)
        {
            builder.AddHtmlLocalized(canPrice ? 470 : 386, 32, 120, 32, 1062451, 16927); // Using Your Filter
        }
        else
        {
            builder.AddHtmlLocalized(canPrice ? 470 : 386, 32, 120, 32, 1062230, 16927); // Using Book Filter
        }

        builder.AddButton(375, 416, 4017, 4018, 0);
        builder.AddHtmlLocalized(410, 416, 120, 20, 1011441, LabelColor); // EXIT

        if (canDrop)
        {
            builder.AddHtmlLocalized(26, 64, 50, 32, 1062212, LabelColor); // Drop
        }

        if (canPrice)
        {
            builder.AddHtmlLocalized(516, 64, 200, 32, 1062218, LabelColor); // Price

            if (canBuy)
            {
                builder.AddHtmlLocalized(576, 64, 200, 32, 1062219, LabelColor); // Buy
            }
            else
            {
                builder.AddHtmlLocalized(576, 64, 200, 32, 1062227, LabelColor); // Set

                builder.AddButton(450, 416, 4005, 4007, 4);
                builder.AddHtml(485, 416, 120, 20, "<BASEFONT COLOR=#FFFFFF>Price all</BASEFONT>");
            }
        }

        tableIndex = 0;

        if (_page > 0)
        {
            builder.AddButton(75, 416, 4014, 4016, 2);
            builder.AddHtmlLocalized(110, 416, 150, 20, 1011067, LabelColor); // Previous page
        }

        if (GetIndexForPage(_page + 1) < List.Count)
        {
            builder.AddButton(225, 416, 4005, 4007, 3);
            builder.AddHtmlLocalized(260, 416, 150, 20, 1011066, LabelColor); // Next page
        }

        for (var i = index; i < index + count && i >= 0 && i < List.Count; ++i)
        {
            var entry = List[i];

            if (!CheckFilter(entry))
            {
                continue;
            }

            if (entry is BOBLargeEntry largeEntry)
            {
                var y = 96 + tableIndex * 32;

                if (canDrop)
                {
                    builder.AddButton(35, y + 2, 5602, 5606, 5 + i * 2);
                }

                if (canDrop || canBuy && entry.Price > 0)
                {
                    builder.AddButton(579, y + 2, 2117, 2118, 6 + i * 2);
                    builder.AddLabel(495, y, 1152, entry.Price.ToString());
                }

                builder.AddHtmlLocalized(61, y, 50, 32, 1062225, LabelColor); // Large

                for (var j = 0; j < largeEntry.Entries.Length; ++j)
                {
                    var sub = largeEntry.Entries[j];

                    builder.AddHtmlLocalized(103, y, 130, 32, sub.Number, LabelColor);

                    builder.AddHtmlLocalized(
                        235,
                        y,
                        80,
                        20,
                        entry.RequireExceptional
                            ? 1060636 // exceptional
                            : 1011542, // normal
                        LabelColor
                    );

                    var name = GetMaterialName(entry.Material, entry.DeedType, sub.ItemType);

                    name.AddHtmlText(
                        ref builder,
                        316,
                        y,
                        100,
                        20,
                        false,
                        false,
                        1152,
                        LabelColor
                    );

                    builder.AddLabel(421, y, 1152, $"{sub.AmountCur} / {entry.AmountMax}");

                    ++tableIndex;
                    y += 32;
                }
            }
            else
            {
                var smallEntry = (BOBSmallEntry)entry;

                var y = 96 + tableIndex++ * 32;

                if (canDrop)
                {
                    builder.AddButton(35, y + 2, 5602, 5606, 5 + i * 2);
                }

                if (canDrop || canBuy && smallEntry.Price > 0)
                {
                    builder.AddButton(579, y + 2, 2117, 2118, 6 + i * 2);
                    builder.AddLabel(495, y, 1152, $"{smallEntry.Price}");
                }

                builder.AddHtmlLocalized(61, y, 50, 32, 1062224, LabelColor); // Small

                builder.AddHtmlLocalized(103, y, 130, 32, smallEntry.Number, LabelColor);

                builder.AddHtmlLocalized(
                    235,
                    y,
                    80,
                    20,
                    smallEntry.RequireExceptional
                        ? 1060636  // exceptional
                        : 1011542, // normal
                    LabelColor
                );

                var name = GetMaterialName(smallEntry.Material, smallEntry.DeedType, smallEntry.ItemType);

                name.AddHtmlText(
                    ref builder,
                    316,
                    y,
                    100,
                    20,
                    false,
                    false,
                    1152,
                    LabelColor
                );

                builder.AddLabel(421, y, 1152, $"{smallEntry.AmountCur} / {smallEntry.AmountMax}");
            }
        }
    }

    public bool CheckFilter(IBOBEntry entry)
    {
        if (entry is BOBLargeEntry largeEntry)
        {
            return CheckFilter(
                entry.Material,
                entry.AmountMax,
                true,
                entry.RequireExceptional,
                entry.DeedType,
                largeEntry.Entries.Length > 0 ? largeEntry.Entries[0].ItemType : null
            );
        }

        if (entry is BOBSmallEntry smallEntry)
        {
            return CheckFilter(
                entry.Material,
                entry.AmountMax,
                false,
                entry.RequireExceptional,
                entry.DeedType,
                smallEntry.ItemType
            );
        }

        return false;
    }

    public bool CheckFilter(
        BulkMaterialType mat, int amountMax, bool isLarge, bool reqExc, BODType deedType,
        Type itemType
    )
    {
        var f = _from.UseOwnFilter ? _from.BOBFilter : Book.Filter;

        if (f.IsDefault)
        {
            return true;
        }

        if (f.Quality == 1 && reqExc)
        {
            return false;
        }

        if (f.Quality == 2 && !reqExc)
        {
            return false;
        }

        if (f.Quantity == 1 && amountMax != 10)
        {
            return false;
        }

        if (f.Quantity == 2 && amountMax != 15)
        {
            return false;
        }

        if (f.Quantity == 3 && amountMax != 20)
        {
            return false;
        }

        if (f.Type == 1 && isLarge)
        {
            return false;
        }

        if (f.Type == 2 && !isLarge)
        {
            return false;
        }

        return f.Material switch
        {
            1  => deedType == BODType.Smith,
            2  => deedType == BODType.Tailor,
            3  => mat == BulkMaterialType.None && BGTClassifier.Classify(deedType, itemType) == BulkGenericType.Iron,
            4  => mat == BulkMaterialType.DullCopper,
            5  => mat == BulkMaterialType.ShadowIron,
            6  => mat == BulkMaterialType.Copper,
            7  => mat == BulkMaterialType.Bronze,
            8  => mat == BulkMaterialType.Gold,
            9  => mat == BulkMaterialType.Agapite,
            10 => mat == BulkMaterialType.Verite,
            11 => mat == BulkMaterialType.Valorite,
            12 => mat == BulkMaterialType.None && BGTClassifier.Classify(deedType, itemType) == BulkGenericType.Cloth,
            13 => mat == BulkMaterialType.None && BGTClassifier.Classify(deedType, itemType) == BulkGenericType.Leather,
            14 => mat == BulkMaterialType.Spined,
            15 => mat == BulkMaterialType.Horned,
            16 => mat == BulkMaterialType.Barbed,
            _  => true
        };
    }

    public int GetIndexForPage(int page)
    {
        var index = 0;

        while (page-- > 0)
        {
            index += GetCountForIndex(index);
        }

        return index;
    }

    public int GetCountForIndex(int index)
    {
        var slots = 0;
        var count = 0;

        var list = List;

        for (var i = index; i >= 0 && i < list.Count; ++i)
        {
            var entry = list[i];

            if (CheckFilter(entry))
            {
                var add = entry is BOBLargeEntry largeEntry ? largeEntry.Entries.Length : 1;

                if (slots + add > 10)
                {
                    break;
                }

                slots += add;
            }

            ++count;
        }

        return count;
    }

    public int GetPageForIndex(int index, int sizeDropped)
    {
        if (index <= 0)
        {
            return 0;
        }

        var count = 0;
        var page = 0;
        int i;

        var list = List;
        for (i = 0; i < index && i < list.Count; i++)
        {
            var entry = list[i];
            if (!CheckFilter(entry))
            {
                continue;
            }

            var add = entry is BOBLargeEntry largeEntry ? largeEntry.Entries.Length : 1;
            count += add;
            if (count > 10)
            {
                page++;
                count = add;
            }
        }

        /* now we are on the page of the bod preceding the dropped one.
         * next step: checking whether we have to remain where we are.
         * The counter i needs to be incremented as the bod to this very moment
         * has not yet been removed from m_List */
        i++;

        /* if, for instance, a big bod of size 6 has been removed, smaller bods
         * might fall back into this page. Depending on their sizes, the page needs
         * to be adjusted accordingly. This is done now.
         */
        if (count + sizeDropped > 10)
        {
            while (i < list.Count && count <= 10)
            {
                var entry = list[i];
                if (CheckFilter(entry))
                {
                    count += entry is BOBLargeEntry largeEntry ? largeEntry.Entries.Length : 1;
                }

                i++;
            }

            if (count > 10)
            {
                page++;
            }
        }

        return page;
    }

    public static TextDefinition GetMaterialName(BulkMaterialType mat, BODType type, Type itemType) =>
        type switch
        {
            BODType.Smith => mat switch
            {
                BulkMaterialType.None       => 1062226,
                BulkMaterialType.DullCopper => 1018332,
                BulkMaterialType.ShadowIron => 1018333,
                BulkMaterialType.Copper     => 1018334,
                BulkMaterialType.Bronze     => 1018335,
                BulkMaterialType.Gold       => 1018336,
                BulkMaterialType.Agapite    => 1018337,
                BulkMaterialType.Verite     => 1018338,
                BulkMaterialType.Valorite   => 1018339,
                _                           => 1062226
            },
            BODType.Tailor => mat switch
            {
                BulkMaterialType.Spined => 1062236,
                BulkMaterialType.Horned => 1062237,
                BulkMaterialType.Barbed => 1062238,
                _ when itemType.IsSubclassOf(typeof(BaseArmor)) ||
                       itemType.IsSubclassOf(typeof(BaseShoes)) => 1062235,
                _                       => 1044286
            },
            _ => TextDefinition.Empty
        };

    public override void SendTo(NetState ns)
    {
        ns.CloseGump<BOBFilterGump>();

        base.SendTo(ns);
    }

    public override void OnResponse(NetState sender, in RelayInfo info)
    {
        var index = info.ButtonID;

        switch (index)
        {
            case 0: // EXIT
                {
                    break;
                }
            case 1: // Set Filter
                {
                    _from.SendGump(new BOBFilterGump(_from, Book));

                    break;
                }
            case 2: // Previous page
                {
                    if (_page > 0)
                    {
                        _page--;
                        _from.SendGump(this);
                    }

                    return;
                }
            case 3: // Next page
                {
                    if (GetIndexForPage(_page + 1) < List.Count)
                    {
                        _page++;
                        _from.SendGump(this);
                    }

                    break;
                }
            case 4: // Price all
                {
                    if (Book.IsChildOf(_from.Backpack))
                    {
                        _from.Prompt = new SetPricePrompt(this, null);
                        _from.SendMessage("Type in a price for all deeds in the book:");
                    }

                    break;
                }
            default:
                {
                    index -= 5;

                    var type = index % 2;
                    index /= 2;

                    if (index < 0 || index >= List.Count)
                    {
                        break;
                    }

                    var bobEntry = List[index];

                    if (!Book.Entries.Contains(bobEntry))
                    {
                        _from.SendLocalizedMessage(1062382); // The deed selected is not available.
                        break;
                    }

                    if (Book.IsChildOf(_from.Backpack))
                    {
                        if (type == 0) // Drop
                        {
                            var item = bobEntry.Reconstruct();

                            var pack = _from.Backpack;
                            if (pack?.CheckHold(
                                    _from,
                                    item,
                                    true,
                                    true,
                                    0,
                                    item.PileWeight + item.TotalWeight
                                ) != true)
                            {
                                _from.SendLocalizedMessage(503204); // You do not have room in your backpack for this
                                ResetList();
                                _from.SendGump(this);
                            }
                            else
                            {
                                var sizeOfDroppedBod = bobEntry is BOBLargeEntry entry ? entry.Entries.Length : 1;

                                _from.AddToBackpack(item);

                                // The bulk order deed has been placed in your backpack.
                                _from.SendLocalizedMessage(1045152);

                                Book.RemoveEntry(bobEntry);

                                if (Book.Entries.Count / 5 < Book.ItemCount)
                                {
                                    Book.ItemCount--;
                                    Book.InvalidateItems();
                                }

                                if (Book.Entries.Count > 0)
                                {
                                    _page = GetPageForIndex(index, sizeOfDroppedBod);
                                    ResetList();
                                    _from.SendGump(this);
                                }
                                else
                                {
                                    _from.SendLocalizedMessage(1062381); // The book is empty.
                                }
                            }
                        }
                        else // Set Price | Buy
                        {
                            _from.Prompt = new SetPricePrompt(this, bobEntry);
                            _from.SendLocalizedMessage(1062383); // Type in a price for the deed:
                        }
                    }
                    else if (Book.RootParent is PlayerVendor pv)
                    {
                        var vi = pv.GetVendorItem(Book);

                        if (vi?.IsForSale != false)
                        {
                            return;
                        }

                        var sizeOfDroppedBod = bobEntry is BOBLargeEntry largeEntry ? largeEntry.Entries.Length : 1;
                        var price = bobEntry.Price;

                        if (price == 0)
                        {
                            _from.SendLocalizedMessage(1062382); // The deed selected is not available.
                        }
                        else if (Book.Entries.Count > 0)
                        {
                            _page = GetPageForIndex(index, sizeOfDroppedBod);
                            _from.SendGump(new BODBuyGump(this, bobEntry, price));
                        }
                        else
                        {
                            _from.SendLocalizedMessage(1062381); // The book is empty
                        }
                    }

                    break;
                }
        }
    }

    private class SetPricePrompt : Prompt
    {
        private readonly BOBGump _gump;
        private readonly IBOBEntry _entry;

        public SetPricePrompt(BOBGump gump, IBOBEntry entry)
        {
            _gump = gump;
            _entry = entry;
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (_entry != null && !_gump.Book.Entries.Contains(_entry))
            {
                from.SendLocalizedMessage(1062382); // The deed selected is not available.
                return;
            }

            var price = Utility.ToInt32(text);

            if (price is < 0 or > 250000000)
            {
                from.SendLocalizedMessage(1062390); // The price you requested is outrageous!
                return;
            }

            if (_entry == null)
            {
                for (var i = 0; i < _gump.List.Count; ++i)
                {
                    var entry = _gump.List[i];

                    if (!_gump.Book.Entries.Contains(entry))
                    {
                        continue;
                    }

                    entry.Price = price;
                }
            }
            else
            {
                _entry.Price = price;
            }

            from.SendLocalizedMessage(1062384); // Deed price set.
            from.SendGump(_gump);
        }
    }
}
