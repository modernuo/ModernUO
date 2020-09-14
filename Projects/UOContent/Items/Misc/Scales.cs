using Server.Targeting;

namespace Server.Items
{
    public class Scales : Item
    {
        [Constructible]
        public Scales() : base(0x1852) => Weight = 4.0;

        public Scales(Serial serial) : base(serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            from.SendLocalizedMessage(502431); // What would you like to weigh?
            from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private readonly Scales m_Item;

            public InternalTarget(Scales item) : base(1, false, TargetFlags.None) => m_Item = item;

            protected override void OnTarget(Mobile from, object targeted)
            {
                string message;

                if (targeted == m_Item)
                {
                    message = "It cannot weight itself.";
                }
                else if (targeted is Item item)
                {
                    var root = item.RootParent;

                    if (root != null && root != from || item.Parent == from)
                    {
                        message = "You decide that item's current location is too awkward to get an accurate result.";
                    }
                    else if (item.Movable)
                    {
                        message = item.Amount > 1
                            ? "You place one item on the scale. "
                            : "You place that item on the scale. ";

                        var weight = item.Weight;

                        if (weight <= 0.0)
                        {
                            message += "It is lighter than a feather.";
                        }
                        else
                        {
                            message += $"It weighs {weight} stones.";
                        }
                    }
                    else
                    {
                        message = "You cannot weigh that object.";
                    }
                }
                else
                {
                    message = "You cannot weigh that object.";
                }

                from.SendMessage(message);
            }
        }
    }
}
