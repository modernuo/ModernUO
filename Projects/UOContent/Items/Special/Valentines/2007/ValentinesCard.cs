using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    public class ValentinesCard : Item
    {
        private static readonly string Unsigned = "___";
        private string m_From;

        private int m_LabelNumber;
        private string m_To;

        [Constructible]
        public ValentinesCard(int itemid)
            : base(itemid)
        {
            LootType = LootType.Blessed;
            Hue = Utility.RandomDouble() < .001 ? 0x47E : 0xE8;
            m_LabelNumber = Utility.Random(1077589, 5);
        }

        public ValentinesCard(Serial serial)
            : base(serial)
        {
        }

        public override string DefaultName => "a Valentine's card";

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string From
        {
            get => m_From;
            set => m_From = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string To
        {
            get => m_To;
            set => m_To = value;
        }

        /*
         * Five possible messages to be signed:
         *
         * To my one true love, ~1_target_player~. Signed: ~2_player~	1077589
         * You’ve pwnd my heart, ~1_target_player~. Signed: ~2_player~	1077590
         * Happy Valentine’s Day, ~1_target_player~. Signed: ~2_player~	1077591
         * Blackrock has driven me crazy... for ~1_target_player~! Signed: ~2_player~	1077592
         * You light my Candle of Love, ~1_target_player~! Signed: ~2_player~	1077593
         *
         */

        public override void AddNameProperty(IPropertyList list)
        {
            list.Add(m_LabelNumber, $"{m_To ?? Unsigned}\t{m_From ?? Unsigned}");
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(
                from,
                m_LabelNumber,
                $"{m_To ?? Unsigned}\t{m_From ?? Unsigned}"
            );
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_To == null)
            {
                if (IsChildOf(from))
                {
                    from.BeginTarget(10, false, TargetFlags.None, OnTarget);

                    from.SendLocalizedMessage(1077497); // To whom do you wish to give this card?
                }
                else
                {
                    from.SendLocalizedMessage(1080063); // This must be in your backpack to use it.
                }
            }
        }

        public virtual void OnTarget(Mobile from, object targeted)
        {
            if (!Deleted)
            {
                if (targeted is Mobile to)
                {
                    if (to is PlayerMobile)
                    {
                        if (to != from)
                        {
                            m_From = from.Name;
                            m_To = to.Name;
                            from.SendLocalizedMessage(
                                1077498
                            ); // You fill out the card. Hopefully the other person actually likes you...
                            InvalidateProperties();
                        }
                        else
                        {
                            from.SendLocalizedMessage(1077495); // You can't give yourself a card, silly!
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(1077496); // You can't possibly be THAT lonely!
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1077488); // That's not another player!
                }
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
            writer.Write(m_LabelNumber);
            writer.Write(m_From);
            writer.Write(m_To);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
            m_LabelNumber = reader.ReadInt();
            m_From = reader.ReadString();
            m_To = reader.ReadString();

            Utility.Intern(ref m_From);
            Utility.Intern(ref m_To);
        }
    }

    public class ValentinesCardSouth : ValentinesCard
    {
        [Constructible]
        public ValentinesCardSouth()
            : base(0x0EBD)
        {
        }

        public ValentinesCardSouth(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class ValentinesCardEast : ValentinesCard
    {
        [Constructible]
        public ValentinesCardEast()
            : base(0x0EBE)
        {
        }

        public ValentinesCardEast(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
