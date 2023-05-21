using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Scales : Item
{
    [Constructible]
    public Scales() : base(0x1852) => Weight = 4.0;

    public override void OnDoubleClick(Mobile from)
    {
        from.SendLocalizedMessage(502431); // What would you like to weigh?
        from.Target = new InternalTarget(this);
    }

    private class InternalTarget : Target
    {
        private Scales _scales;

        public InternalTarget(Scales item) : base(1, false, TargetFlags.None) => _scales = item;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (targeted == _scales)
            {
                from.SendMessage("It cannot weigh itself.");
                return;
            }

            if (targeted is Mobile m)
            {
                from.SendLocalizedMessage(502432); // That is too heavy for these scales!
            }

            if (targeted is not Item { Movable: true } item)
            {
                from.SendMessage("You cannot weigh that.");
                return;
            }

            var root = item.RootParent;

            if (root != null && root != from || item.Parent == from)
            {
                from.SendMessage("You decide that item's current location is too awkward to get an accurate result.");
                return;
            }

            var amount = item.Amount > 1 ? "one" : "that";
            var weight = item.Weight;

            if (weight <= 0.0)
            {
                from.SendMessage($"You place {amount} item on the scale. It is lighter than a feather.");
            }
            else
            {
                from.SendMessage($"You place {amount} item on the scale. It weighs {weight} stones.");
            }
        }
    }
}
