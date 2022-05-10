using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class CupidsArrow : Item
    {
        [InternString]
        [InvalidateProperties]
        [SerializableField(0)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private string _from;

        [InternString]
        [InvalidateProperties]
        [SerializableField(1)]
        [SerializableFieldAttr("[CommandProperty(AccessLevel.GameMaster)]")]
        private string _to;

        [Constructible]
        public CupidsArrow() : base(0x4F7F) => LootType = LootType.Blessed;

        // TODO: Check messages

        public override int LabelNumber => 1152270; // Cupid's Arrow 2012

        public bool IsSigned => _from != null && _to != null;

        public override void AddNameProperty(ObjectPropertyList list)
        {
            base.AddNameProperty(list);

            if (IsSigned)
            {
                list.Add(1152273, $"{_from}\t{_to}"); // ~1_val~ is madly in love with ~2_val~
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
                LabelTo(from, 1152273, $"{_from}\t{_to}"); // ~1_val~ is madly in love with ~2_val~
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

                From = from.Name;
                To = m.Name;

                InvalidateProperties();

                from.SendMessage("You inscribe the arrow.");
            }
            else
            {
                from.SendMessage("That is not a person.");
            }
        }
    }
}
