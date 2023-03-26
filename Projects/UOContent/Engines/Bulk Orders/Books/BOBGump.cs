using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;

namespace Server.Engines.BulkOrders
{
    public class BOBGump : Gump
    {
        private const int LabelColor = 0x7FFF;
        private readonly BulkOrderBook m_Book;
        private readonly PlayerMobile m_From;
        private readonly List<IBOBEntry> m_List;

        private int m_Page;

        public BOBGump(PlayerMobile from, BulkOrderBook book, int page = 0, List<IBOBEntry> list = null) : base(12, 24)
        {
            from.CloseGump<BOBGump>();
            from.CloseGump<BOBFilterGump>();

            m_From = from;
            m_Book = book;
            m_Page = page;

            if (list == null)
            {
                list = new List<IBOBEntry>(book.Entries.Count);

                for (var i = 0; i < book.Entries.Count; ++i)
                {
                    var entry = book.Entries[i];

                    if (CheckFilter(entry))
                    {
                        list.Add(entry);
                    }
                }
            }

            m_List = list;

            var index = GetIndexForPage(page);
            var count = GetCountForIndex(index);

            var tableIndex = 0;

            var pv = book.RootParent as PlayerVendor;

            var canDrop = book.IsChildOf(from.Backpack);
            var canBuy = pv != null;
            var canPrice = canDrop || canBuy;

            if (canBuy)
            {
                var vi = pv.GetVendorItem(book);

                canBuy = vi?.IsForSale == false;
            }

            var width = 600;

            if (!canPrice)
            {
                width = 516;
            }

            X = (624 - width) / 2;

            AddPage(0);

            AddBackground(10, 10, width, 439, 5054);
            AddImageTiled(18, 20, width - 17, 420, 2624);

            if (canPrice)
            {
                AddImageTiled(573, 64, 24, 352, 200);
                AddImageTiled(493, 64, 78, 352, 1416);
            }

            if (canDrop)
            {
                AddImageTiled(24, 64, 32, 352, 1416);
            }

            AddImageTiled(58, 64, 36, 352, 200);
            AddImageTiled(96, 64, 133, 352, 1416);
            AddImageTiled(231, 64, 80, 352, 200);
            AddImageTiled(313, 64, 100, 352, 1416);
            AddImageTiled(415, 64, 76, 352, 200);

            for (var i = index; i < index + count && i >= 0 && i < list.Count; ++i)
            {
                var entry = list[i];

                if (!CheckFilter(entry))
                {
                    continue;
                }

                AddImageTiled(24, 94 + tableIndex * 32, canPrice ? 573 : 489, 2, 2624);
                tableIndex += entry is BOBLargeEntry largeEntry ? largeEntry.Entries.Length : 1;
            }

            AddAlphaRegion(18, 20, width - 17, 420);
            AddImage(5, 5, 10460);
            AddImage(width - 15, 5, 10460);
            AddImage(5, 424, 10460);
            AddImage(width - 15, 424, 10460);

            AddHtmlLocalized(canPrice ? 266 : 224, 32, 200, 32, 1062220, LabelColor); // Bulk Order Book
            AddHtmlLocalized(63, 64, 200, 32, 1062213, LabelColor);                   // Type
            AddHtmlLocalized(147, 64, 200, 32, 1062214, LabelColor);                  // Item
            AddHtmlLocalized(246, 64, 200, 32, 1062215, LabelColor);                  // Quality
            AddHtmlLocalized(336, 64, 200, 32, 1062216, LabelColor);                  // Material
            AddHtmlLocalized(429, 64, 200, 32, 1062217, LabelColor);                  // Amount

            AddButton(35, 32, 4005, 4007, 1);
            AddHtmlLocalized(70, 32, 200, 32, 1062476, LabelColor); // Set Filter

            var f = from.UseOwnFilter ? from.BOBFilter : book.Filter;

            if (f.IsDefault)
            {
                AddHtmlLocalized(canPrice ? 470 : 386, 32, 120, 32, 1062475, 16927); // Using No Filter
            }
            else if (from.UseOwnFilter)
            {
                AddHtmlLocalized(canPrice ? 470 : 386, 32, 120, 32, 1062451, 16927); // Using Your Filter
            }
            else
            {
                AddHtmlLocalized(canPrice ? 470 : 386, 32, 120, 32, 1062230, 16927); // Using Book Filter
            }

            AddButton(375, 416, 4017, 4018, 0);
            AddHtmlLocalized(410, 416, 120, 20, 1011441, LabelColor); // EXIT

            if (canDrop)
            {
                AddHtmlLocalized(26, 64, 50, 32, 1062212, LabelColor); // Drop
            }

            if (canPrice)
            {
                AddHtmlLocalized(516, 64, 200, 32, 1062218, LabelColor); // Price

                if (canBuy)
                {
                    AddHtmlLocalized(576, 64, 200, 32, 1062219, LabelColor); // Buy
                }
                else
                {
                    AddHtmlLocalized(576, 64, 200, 32, 1062227, LabelColor); // Set

                    AddButton(450, 416, 4005, 4007, 4);
                    AddHtml(485, 416, 120, 20, "<BASEFONT COLOR=#FFFFFF>Price all</FONT>");
                }
            }

            tableIndex = 0;

            if (page > 0)
            {
                AddButton(75, 416, 4014, 4016, 2);
                AddHtmlLocalized(110, 416, 150, 20, 1011067, LabelColor); // Previous page
            }

            if (GetIndexForPage(page + 1) < list.Count)
            {
                AddButton(225, 416, 4005, 4007, 3);
                AddHtmlLocalized(260, 416, 150, 20, 1011066, LabelColor); // Next page
            }

            for (var i = index; i < index + count && i >= 0 && i < list.Count; ++i)
            {
                var entry = list[i];

                if (!CheckFilter(entry))
                {
                    continue;
                }

                if (entry is BOBLargeEntry largeEntry)
                {
                    var y = 96 + tableIndex * 32;

                    if (canDrop)
                    {
                        AddButton(35, y + 2, 5602, 5606, 5 + i * 2);
                    }

                    if (canDrop || canBuy && entry.Price > 0)
                    {
                        AddButton(579, y + 2, 2117, 2118, 6 + i * 2);
                        AddLabel(495, y, 1152, entry.Price.ToString());
                    }

                    AddHtmlLocalized(61, y, 50, 32, 1062225, LabelColor); // Large

                    for (var j = 0; j < largeEntry.Entries.Length; ++j)
                    {
                        var sub = largeEntry.Entries[j];

                        AddHtmlLocalized(103, y, 130, 32, sub.Number, LabelColor);

                        if (entry.RequireExceptional)
                        {
                            AddHtmlLocalized(235, y, 80, 20, 1060636, LabelColor); // exceptional
                        }
                        else
                        {
                            AddHtmlLocalized(235, y, 80, 20, 1011542, LabelColor); // normal
                        }

                        var name = GetMaterialName(entry.Material, entry.DeedType, sub.ItemType);

                        if (name.Number > 0)
                        {
                            AddHtmlLocalized(316, y, 100, 20, name, LabelColor);
                        }
                        else
                        {
                            AddLabel(316, y, 1152, name);
                        }

                        AddLabel(421, y, 1152, $"{sub.AmountCur} / {entry.AmountMax}");

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
                        AddButton(35, y + 2, 5602, 5606, 5 + i * 2);
                    }

                    if (canDrop || canBuy && smallEntry.Price > 0)
                    {
                        AddButton(579, y + 2, 2117, 2118, 6 + i * 2);
                        AddLabel(495, y, 1152, smallEntry.Price.ToString());
                    }

                    AddHtmlLocalized(61, y, 50, 32, 1062224, LabelColor); // Small

                    AddHtmlLocalized(103, y, 130, 32, smallEntry.Number, LabelColor);

                    if (smallEntry.RequireExceptional)
                    {
                        AddHtmlLocalized(235, y, 80, 20, 1060636, LabelColor); // exceptional
                    }
                    else
                    {
                        AddHtmlLocalized(235, y, 80, 20, 1011542, LabelColor); // normal
                    }

                    var name = GetMaterialName(smallEntry.Material, smallEntry.DeedType, smallEntry.ItemType);

                    if (name.Number > 0)
                    {
                        AddHtmlLocalized(316, y, 100, 20, name, LabelColor);
                    }
                    else
                    {
                        AddLabel(316, y, 1152, name);
                    }

                    AddLabel(421, y, 1152, $"{smallEntry.AmountCur} / {smallEntry.AmountMax}");
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
            var f = m_From.UseOwnFilter ? m_From.BOBFilter : m_Book.Filter;

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

            var list = m_List;

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

            var list = m_List;
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

        public TextDefinition GetMaterialName(BulkMaterialType mat, BODType type, Type itemType)
        {
            switch (type)
            {
                case BODType.Smith:
                    {
                        switch (mat)
                        {
                            case BulkMaterialType.None:       return 1062226;
                            case BulkMaterialType.DullCopper: return 1018332;
                            case BulkMaterialType.ShadowIron: return 1018333;
                            case BulkMaterialType.Copper:     return 1018334;
                            case BulkMaterialType.Bronze:     return 1018335;
                            case BulkMaterialType.Gold:       return 1018336;
                            case BulkMaterialType.Agapite:    return 1018337;
                            case BulkMaterialType.Verite:     return 1018338;
                            case BulkMaterialType.Valorite:   return 1018339;
                        }

                        break;
                    }
                case BODType.Tailor:
                    {
                        switch (mat)
                        {
                            case BulkMaterialType.None:
                                {
                                    if (itemType.IsSubclassOf(typeof(BaseArmor)) || itemType.IsSubclassOf(typeof(BaseShoes)))
                                    {
                                        return 1062235;
                                    }

                                    return 1044286;
                                }
                            case BulkMaterialType.Spined: return 1062236;
                            case BulkMaterialType.Horned: return 1062237;
                            case BulkMaterialType.Barbed: return 1062238;
                        }

                        break;
                    }
            }

            return "Invalid";
        }

        public override void OnResponse(NetState sender, RelayInfo info)
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
                        m_From.SendGump(new BOBFilterGump(m_From, m_Book));

                        break;
                    }
                case 2: // Previous page
                    {
                        if (m_Page > 0)
                        {
                            m_From.SendGump(new BOBGump(m_From, m_Book, m_Page - 1, m_List));
                        }

                        return;
                    }
                case 3: // Next page
                    {
                        if (GetIndexForPage(m_Page + 1) < m_List.Count)
                        {
                            m_From.SendGump(new BOBGump(m_From, m_Book, m_Page + 1, m_List));
                        }

                        break;
                    }
                case 4: // Price all
                    {
                        if (m_Book.IsChildOf(m_From.Backpack))
                        {
                            m_From.Prompt = new SetPricePrompt(m_Book, null, m_Page, m_List);
                            m_From.SendMessage("Type in a price for all deeds in the book:");
                        }

                        break;
                    }
                default:
                    {
                        index -= 5;

                        var type = index % 2;
                        index /= 2;

                        if (index < 0 || index >= m_List.Count)
                        {
                            break;
                        }

                        var bobEntry = m_List[index];

                        if (!m_Book.Entries.Contains(bobEntry))
                        {
                            m_From.SendLocalizedMessage(1062382); // The deed selected is not available.
                            break;
                        }

                        if (type == 0) // Drop
                        {
                            if (m_Book.IsChildOf(m_From.Backpack))
                            {
                                var item = bobEntry.Reconstruct();

                                var pack = m_From.Backpack;
                                if (pack?.CheckHold(
                                    m_From,
                                    item,
                                    true,
                                    true,
                                    0,
                                    item.PileWeight + item.TotalWeight
                                ) != true)
                                {
                                    m_From.SendLocalizedMessage(503204); // You do not have room in your backpack for this
                                    m_From.SendGump(new BOBGump(m_From, m_Book, m_Page));
                                }
                                else
                                {
                                    if (m_Book.IsChildOf(m_From.Backpack))
                                    {
                                        var sizeOfDroppedBod = bobEntry is BOBLargeEntry entry ? entry.Entries.Length : 1;

                                        m_From.AddToBackpack(item);

                                        // The bulk order deed has been placed in your backpack.
                                        m_From.SendLocalizedMessage(1045152);

                                        m_Book.Entries.Remove(bobEntry);
                                        m_Book.InvalidateProperties();

                                        if (m_Book.Entries.Count / 5 < m_Book.ItemCount)
                                        {
                                            m_Book.ItemCount--;
                                            m_Book.InvalidateItems();
                                        }

                                        if (m_Book.Entries.Count > 0)
                                        {
                                            m_Page = GetPageForIndex(index, sizeOfDroppedBod);
                                            m_From.SendGump(new BOBGump(m_From, m_Book, m_Page));
                                        }
                                        else
                                        {
                                            m_From.SendLocalizedMessage(1062381); // The book is empty.
                                        }
                                    }
                                }
                            }
                        }
                        else // Set Price | Buy
                        {
                            if (m_Book.IsChildOf(m_From.Backpack))
                            {
                                m_From.Prompt = new SetPricePrompt(m_Book, bobEntry, m_Page, m_List);
                                m_From.SendLocalizedMessage(1062383); // Type in a price for the deed:
                            }
                            else if (m_Book.RootParent is PlayerVendor pv)
                            {
                                var vi = pv.GetVendorItem(m_Book);

                                if (vi?.IsForSale != false)
                                {
                                    return;
                                }

                                var sizeOfDroppedBod = bobEntry is BOBLargeEntry largeEntry ? largeEntry.Entries.Length : 1;
                                var price = bobEntry.Price;

                                if (price == 0)
                                {
                                    m_From.SendLocalizedMessage(1062382); // The deed selected is not available.
                                }
                                else
                                {
                                    if (m_Book.Entries.Count > 0)
                                    {
                                        m_Page = GetPageForIndex(index, sizeOfDroppedBod);
                                        m_From.SendGump(new BODBuyGump(m_From, m_Book, bobEntry, m_Page, price));
                                    }
                                    else
                                    {
                                        m_From.SendLocalizedMessage(1062381); // The book is emptz
                                    }
                                }
                            }
                        }

                        break;
                    }
            }
        }

        private class SetPricePrompt : Prompt
        {
            private readonly BulkOrderBook m_Book;
            private readonly IBOBEntry m_Entry;
            private readonly List<IBOBEntry> m_List;
            private readonly int m_Page;

            public SetPricePrompt(BulkOrderBook book, IBOBEntry entry, int page, List<IBOBEntry> list)
            {
                m_Book = book;
                m_Entry = entry;
                m_Page = page;
                m_List = list;
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (m_Entry != null && !m_Book.Entries.Contains(m_Entry))
                {
                    from.SendLocalizedMessage(1062382); // The deed selected is not available.
                    return;
                }

                var price = Utility.ToInt32(text);

                if (price is < 0 or > 250000000)
                {
                    from.SendLocalizedMessage(1062390); // The price you requested is outrageous!
                }
                else if (m_Entry == null)
                {
                    for (var i = 0; i < m_List.Count; ++i)
                    {
                        var entry = m_List[i];

                        if (!m_Book.Entries.Contains(entry))
                        {
                            continue;
                        }

                        entry.Price = price;
                    }

                    // Deed price set.
                    from.SendLocalizedMessage(1062384);

                    if (from is PlayerMobile mobile)
                    {
                        mobile.SendGump(new BOBGump(mobile, m_Book, m_Page, m_List));
                    }
                }
                else
                {
                    m_Entry.Price = price;
                    from.SendLocalizedMessage(1062384); // Deed price set.
                    if (from is PlayerMobile mobile)
                    {
                        mobile.SendGump(new BOBGump(mobile, m_Book, m_Page, m_List));
                    }
                }
            }
        }
    }
}
