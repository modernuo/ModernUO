using ModernUO.Serialization;
using Server.Gumps;
using Server.Network;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class NewPlayerTicket : Item
{
    [SerializableField(0)]
    [SerializedCommandProperty(AccessLevel.Owner)]
    private Mobile _owner;

    [Constructible]
    public NewPlayerTicket() : base(0x14EF)
    {
        LootType = LootType.Blessed;
    }

    public override double DefaultWeight => 1.0;

    public override int LabelNumber => 1062094; // a young player ticket

    public override bool DisplayLootType => false;

    public override void GetProperties(IPropertyList list)
    {
        base.GetProperties(list);

        // This is half a prize ticket! Double-click this ticket and target any other ticket marked NEW PLAYER and get a prize! This ticket will only work for YOU, so don't give it away!
        list.Add(1041492);
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (from != Owner)
        {
            from.SendLocalizedMessage(501926); // This isn't your ticket! Shame on you! You have to use YOUR ticket.
        }
        else if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else
        {
            from.SendLocalizedMessage(501927); // Target any other ticket marked NEW PLAYER to win a prize.
            from.Target = new InternalTarget(this);
        }
    }

    private class InternalTarget : Target
    {
        private readonly NewPlayerTicket m_Ticket;

        public InternalTarget(NewPlayerTicket ticket) : base(2, false, TargetFlags.None) => m_Ticket = ticket;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted == m_Ticket)
            {
                from.SendLocalizedMessage(501928); // You can't target the same ticket!
            }
            else if (targeted is NewPlayerTicket theirTicket)
            {
                var them = theirTicket.Owner;

                if (them?.Deleted != false)
                {
                    from.SendLocalizedMessage(501930); // That is not a valid ticket.
                }
                else
                {
                    NewPlayerTicketGump.DisplayTo(from, m_Ticket);
                    NewPlayerTicketGump.DisplayTo(them, theirTicket);
                }
            }
            else if ((targeted as Item)?.ItemID == 0x14F0)
            {
                from.SendLocalizedMessage(501931); // You need to find another ticket marked NEW PLAYER.
            }
            else
            {
                from.SendLocalizedMessage(501929); // You will need to select a ticket.
            }
        }
    }

    private class NewPlayerTicketGump : StaticGump<NewPlayerTicketGump>
    {
        private readonly NewPlayerTicket _ticket;

        public override bool Singleton => true;

        private NewPlayerTicketGump(NewPlayerTicket ticket) : base(50, 50) => _ticket = ticket;

        public static void DisplayTo(Mobile from, NewPlayerTicket ticket)
        {
            if (from?.NetState == null || ticket?.Deleted != false)
            {
                return;
            }

            from.SendGump(new NewPlayerTicketGump(ticket));
        }

        protected override void BuildLayout(ref StaticGumpBuilder builder)
        {
            builder.AddBackground(0, 0, 400, 385, 0xA28);

            // Choose the gift you prefer. WARNING: if you cancel, and your partner does not, you will need to find another matching ticket!
            builder.AddHtmlLocalized(30, 45, 340, 70, 1013011, true, true);

            builder.AddButton(46, 128, 0xFA5, 0xFA7, 1);
            builder.AddHtmlLocalized(80, 130, 320, 35, 1013012); // A sextant

            builder.AddButton(46, 163, 0xFA5, 0xFA7, 2);
            builder.AddHtmlLocalized(80, 165, 320, 35, 1013013); // A coupon for a single hair restyling

            builder.AddButton(46, 198, 0xFA5, 0xFA7, 3);
            builder.AddHtmlLocalized(80, 200, 320, 35, 1013014); // A spellbook with all 1st - 4th spells.

            builder.AddButton(46, 233, 0xFA5, 0xFA7, 4);
            builder.AddHtmlLocalized(80, 235, 320, 35, 1013015); // A wand of fireworks

            builder.AddButton(46, 268, 0xFA5, 0xFA7, 5);
            builder.AddHtmlLocalized(80, 270, 320, 35, 1013016); // A spyglass

            builder.AddButton(46, 303, 0xFA5, 0xFA7, 6);
            builder.AddHtmlLocalized(80, 305, 320, 35, 1013017); // Dyes and a dye tub

            builder.AddButton(120, 340, 0xFA5, 0xFA7, 0);
            builder.AddHtmlLocalized(154, 342, 100, 35, 1011012); // CANCEL
        }

        public override void OnResponse(NetState sender, in RelayInfo info)
        {
            if (_ticket.Deleted)
            {
                return;
            }

            var number = 0;

            Item item = null;
            Item item2 = null;

            switch (info.ButtonID)
            {
                case 1:
                    {
                        item = new Sextant();
                        number = 1010494;
                        break; // A sextant has been placed in your backpack.
                    }
                case 2:
                    {
                        item = new HairRestylingDeed();
                        number = 501933;
                        break; // A coupon for a free hair restyling has been placed in your backpack.
                    }
                case 3:
                    {
                        item = new Spellbook(0xFFFFFFFF);
                        number = 1010495;
                        break; // A spellbook with all 1st to 4th circle spells has been placed in your backpack.
                    }
                case 4:
                    {
                        item = new FireworksWand();
                        number = 501935;
                        break; // A wand of fireworks has been placed in your backpack.
                    }
                case 5:
                    {
                        item = new Spyglass();
                        number = 501936;
                        break; // A spyglass has been placed in your backpack.
                    }
                case 6:
                    {
                        item = new DyeTub();
                        item2 = new Dyes();
                        number = 501937;
                        break; // The dyes and dye tub have been placed in your backpack.
                    }
            }

            if (item != null)
            {
                var from = sender.Mobile;
                _ticket.Delete();

                from.SendLocalizedMessage(number);
                from.AddToBackpack(item);

                if (item2 != null)
                {
                    from.AddToBackpack(item2);
                }
            }
        }
    }
}
