using System.Collections.Generic;
using ModernUO.Serialization;
using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Prompts;

namespace Server.Engines.BulkOrders;

[SerializationGenerator(3, false)]
public partial class BulkOrderBook : Item, ISecurable
{
    [SerializableField(0)]
    private int _itemCount;

    [InvalidateProperties]
    [SerializableField(1)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private SecureLevel _level;

    [InvalidateProperties]
    [SerializableField(2)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private string _bookName;

    [SerializableField(3)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private BOBFilter _filter;

    [SerializableField(4)]
    [SerializedCommandProperty(AccessLevel.GameMaster)]
    private List<IBOBEntry> _entries;

    [Constructible]
    public BulkOrderBook() : base(0x2259)
    {
        Weight = 1.0;
        LootType = LootType.Blessed;

        _entries = new List<IBOBEntry>();
        _filter = new BOBFilter();

        _level = SecureLevel.CoOwners;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
        }
        else if (Entries.Count == 0)
        {
            from.SendLocalizedMessage(1062381); // The book is empty.
        }
        else if (from is PlayerMobile mobile)
        {
            mobile.SendGump(new BOBGump(mobile, this));
        }
    }

    public override void OnDoubleClickSecureTrade(Mobile from)
    {
        if (!from.InRange(GetWorldLocation(), 2))
        {
            from.SendLocalizedMessage(500446); // That is too far away.
        }
        else if (Entries.Count == 0)
        {
            from.SendLocalizedMessage(1062381); // The book is empty.
        }
        else
        {
            from.SendGump(new BOBGump((PlayerMobile)from, this));

            var trade = GetSecureTradeCont()?.Trade;

            if (trade?.From.Mobile == from)
            {
                trade.To.Mobile.SendGump(new BOBGump((PlayerMobile)trade.To.Mobile, this));
            }
            else if (trade?.To.Mobile == from)
            {
                trade.From.Mobile.SendGump(new BOBGump((PlayerMobile)trade.From.Mobile, this));
            }
        }
    }

    public override bool OnDragDrop(Mobile from, Item dropped)
    {
        if (dropped is BaseBOD)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1062385); // You must have the book in your backpack to add deeds to it.
                return false;
            }

            if (!from.Backpack.CheckHold(from, dropped, true, true))
            {
                return false;
            }

            if (Entries.Count < 500)
            {
                IBOBEntry entry = dropped is LargeBOD largeBod ? new BOBLargeEntry(largeBod) : new BOBSmallEntry((SmallBOD)dropped);
                AddEntry(entry);

                if (Entries.Count / 5 > ItemCount)
                {
                    ItemCount++;
                    InvalidateItems();
                }

                from.SendSound(0x42, GetWorldLocation());
                from.SendLocalizedMessage(1062386); // Deed added to book.

                if (from is PlayerMobile pm)
                {
                    pm.SendGump(new BOBGump(pm, this));
                }

                dropped.Delete();

                return true;
            }

            from.SendLocalizedMessage(1062387); // The book is full of deeds.
            return false;
        }

        from.SendLocalizedMessage(1062388); // That is not a bulk order deed.
        return false;
    }

    public override int GetTotal(TotalType type)
    {
        var total = base.GetTotal(type);

        if (type == TotalType.Items)
        {
            total = ItemCount;
        }

        return total;
    }

    public void AddEntry(IBOBEntry entry)
    {
        this.Add(Entries, entry);
        InvalidateProperties();
    }

    public void RemoveEntry(IBOBEntry entry)
    {
        this.Remove(Entries, entry);
        InvalidateProperties();
    }

    public void InvalidateItems()
    {
        if (RootParent is Mobile m)
        {
            m.UpdateTotals();
            InvalidateContainers(Parent);
        }
    }

    public void InvalidateContainers(IEntity parent)
    {
        if (parent is Container c)
        {
            c.InvalidateProperties();
            InvalidateContainers(c.Parent);
        }
    }

    private void Deserialize(IGenericReader reader, int version)
    {
        _itemCount = reader.ReadInt();
        _level = (SecureLevel)reader.ReadInt();

        _bookName = reader.ReadString();

        _filter = new BOBFilter();
        _filter.Deserialize(reader);

        var count = reader.ReadEncodedInt();

        Entries = new List<IBOBEntry>(count);

        for (var i = 0; i < count; ++i)
        {
            var v = reader.ReadEncodedInt();

            switch (v)
            {
                case 0:
                    {
                        var largeEntry = new BOBLargeEntry(BOBEntries.NewBOBEntry);
                        largeEntry.Deserialize(reader);
                        AddEntry(largeEntry);
                        break;
                    }
                case 1:
                    {
                        var smallEntry = new BOBSmallEntry(BOBEntries.NewBOBEntry);
                        smallEntry.Deserialize(reader);
                        AddEntry(smallEntry);
                        break;
                    }
            }
        }
    }

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        list.Add(1062344, Entries.Count); // Deeds in book: ~1_val~

        if (!string.IsNullOrEmpty(_bookName))
        {
            list.Add(1062481, _bookName); // Book Name: ~1_val~
        }
    }

    public override void OnSingleClick(Mobile from)
    {
        base.OnSingleClick(from);

        LabelTo(from, 1062344, Entries.Count.ToString()); // Deeds in book: ~1_val~

        if (!string.IsNullOrEmpty(_bookName))
        {
            LabelTo(from, 1062481, _bookName);
        }
    }

    public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
    {
        base.GetContextMenuEntries(from, list);

        if (from.CheckAlive() && IsChildOf(from.Backpack))
        {
            list.Add(new NameBookEntry(from, this));
        }

        SetSecureLevelEntry.AddTo(from, this, list);
    }

    private class NameBookEntry : ContextMenuEntry
    {
        private readonly BulkOrderBook m_Book;
        private readonly Mobile m_From;

        public NameBookEntry(Mobile from, BulkOrderBook book) : base(6216)
        {
            m_From = from;
            m_Book = book;
        }

        public override void OnClick()
        {
            if (m_From.CheckAlive() && m_Book.IsChildOf(m_From.Backpack))
            {
                m_From.Prompt = new NameBookPrompt(m_Book);
                m_From.SendLocalizedMessage(1062479); // Type in the new name of the book:
            }
        }
    }

    private class NameBookPrompt : Prompt
    {
        private readonly BulkOrderBook m_Book;

        public NameBookPrompt(BulkOrderBook book) => m_Book = book;

        public override void OnResponse(Mobile from, string text)
        {
            if (text.Length > 40)
            {
                text = text[..40];
            }

            if (from.CheckAlive() && m_Book.IsChildOf(from.Backpack))
            {
                m_Book.BookName = Utility.FixHtml(text.Trim());

                from.SendLocalizedMessage(1062480); // The bulk order book's name has been changed.
            }
        }

        public override void OnCancel(Mobile from)
        {
        }
    }
}
