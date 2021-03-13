using System;
using Server.Targeting;

namespace Server.Items
{
    public class CupidsArrow : Item
    {
        private string m_From;
        private string m_To;

        [Constructible]
        public CupidsArrow()
            : base(0x4F7F) =>
            LootType = LootType.Blessed;

        public CupidsArrow(Serial serial)
            : base(serial)
        {
        }
        // TODO: Check messages

        public override int LabelNumber => 1152270; // Cupid's Arrow 2012

        [CommandProperty(AccessLevel.GameMaster)]
        public string From
        {
            get => m_From;
            set
            {
                m_From = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string To
        {
            get => m_To;
            set
            {
                m_To = value;
                InvalidateProperties();
            }
        }

        public bool IsSigned => m_From != null && m_To != null;

        public override void AddNameProperty(ObjectPropertyList list)
        {
            base.AddNameProperty(list);

            if (IsSigned)
            {
                list.Add(1152273, $"{m_From}\t{m_To}"); // ~1_val~ is madly in love with ~2_val~
            }
        }

        public static bool CheckSeason(Mobile from)
        {
            if (Core.Now.Month == 2)
            {
                return true;
            }

            from.SendLocalizedMessage(1152318); // You may not use this item out of season.
            return false;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (IsSigned)
            {
                LabelTo(from, 1152273, $"{m_From}\t{m_To}"); // ~1_val~ is madly in love with ~2_val~
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsSigned || !CheckSeason(from))
            {
                return;
            }

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1080063); // This must be in your backpack to use it.
                return;
            }

            from.BeginTarget(10, false, TargetFlags.None, OnTarget);
            from.SendMessage("Who do you wish to use this on?");
        }

        private void OnTarget(Mobile from, object targeted)
        {
            if (IsSigned || !IsChildOf(from.Backpack))
            {
                return;
            }

            if (targeted is Mobile m)
            {
                if (!m.Alive)
                {
                    from.SendLocalizedMessage(
                        1152269
                    ); // That target is dead and even Cupid's arrow won't make them love you.
                    return;
                }

                m_From = from.Name;
                m_To = m.Name;

                InvalidateProperties();

                from.SendMessage("You inscribe the arrow.");
            }
            else
            {
                from.SendMessage("That is not a person.");
            }
        }

        public override void Serialize(IGenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);

            writer.Write(m_From);
            writer.Write(m_To);
        }

        public override void Deserialize(IGenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();

            m_From = Utility.Intern(reader.ReadString());
            m_To = Utility.Intern(reader.ReadString());
        }
    }
}
