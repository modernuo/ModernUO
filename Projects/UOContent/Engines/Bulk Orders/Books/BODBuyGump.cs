using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.BulkOrders
{
    public class BODBuyGump : Gump
    {
        private readonly BulkOrderBook m_Book;
        private readonly IBOBEntry m_Entry;
        private readonly PlayerMobile m_From;
        private readonly int m_Page;
        private readonly int m_Price;

        public BODBuyGump(PlayerMobile from, BulkOrderBook book, IBOBEntry entry, int page, int price) : base(100, 200)
        {
            m_From = from;
            m_Book = book;
            m_Entry = entry;
            m_Price = price;
            m_Page = page;

            AddPage(0);

            AddBackground(100, 10, 300, 150, 5054);

            AddHtmlLocalized(125, 20, 250, 24, 1019070); // You have agreed to purchase:
            AddHtmlLocalized(125, 45, 250, 24, 1045151); // a bulk order deed

            AddHtmlLocalized(125, 70, 250, 24, 1019071); // for the amount of:
            AddLabel(125, 95, 0, price.ToString());

            AddButton(250, 130, 4005, 4007, 1);
            AddHtmlLocalized(282, 130, 100, 24, 1011012); // CANCEL

            AddButton(120, 130, 4005, 4007, 2);
            AddHtmlLocalized(152, 130, 100, 24, 1011036); // OKAY
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID != 2)
            {
                m_From.SendLocalizedMessage(503207); // Cancelled purchase.
                return;
            }

            if (m_Book.RootParent is not PlayerVendor pv)
            {
                m_From.SendLocalizedMessage(1062382); // The deed selected is not available.
                return;
            }

            if (!m_Book.Entries.Contains(m_Entry))
            {
                pv.SayTo(m_From, 1062382); // The deed selected is not available.
                return;
            }

            var price = 0;

            if (pv.GetVendorItem(m_Book)?.IsForSale == false)
            {
                price = m_Entry.Price;
            }

            if (price != m_Price)
            {
                pv.SayTo(
                    m_From,
                    "The price has been been changed. If you like, you may offer to purchase the item again."
                );
                return;
            }

            if (price == 0)
            {
                pv.SayTo(m_From, 1062382); // The deed selected is not available.
                return;
            }

            var item = m_Entry.Reconstruct();

            pv.Say(m_From.Name);

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
                pv.SayTo(m_From, 503204); // You do not have room in your backpack for this
                m_From.SendGump(new BOBGump(m_From, m_Book, m_Page));
                item.Delete();
            }
            else
            {
                if (pack.ConsumeTotal(typeof(Gold), price) || Banker.Withdraw(m_From, price))
                {
                    m_Book.RemoveEntry(m_Entry);
                    m_Book.InvalidateProperties();
                    pv.HoldGold += price;
                    m_From.AddToBackpack(item);

                    // The bulk order deed has been placed in your backpack.
                    m_From.SendLocalizedMessage(1045152);

                    if (m_Book.Entries.Count / 5 < m_Book.ItemCount)
                    {
                        m_Book.ItemCount--;
                        m_Book.InvalidateItems();
                    }

                    if (m_Book.Entries.Count > 0)
                    {
                        m_From.SendGump(new BOBGump(m_From, m_Book, m_Page));
                    }
                    else
                    {
                        m_From.SendLocalizedMessage(1062381); // The book is empty.
                    }
                }
                else
                {
                    pv.SayTo(m_From, 503205); // You cannot afford this item.
                    item.Delete();
                }
            }
        }
    }
}
