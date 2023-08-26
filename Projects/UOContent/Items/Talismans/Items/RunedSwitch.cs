using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class RunedSwitch : Item
{
    [Constructible]
    public RunedSwitch() : base(0x2F61) => Weight = 1.0;

    public RunedSwitch(Serial serial) : base(serial)
    {
    }

    public override int LabelNumber => 1072896; // runed switch

    public override void OnDoubleClick(Mobile from)
    {
        if (IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1075101); // Please select an item to recharge.
            from.Target = new InternalTarget(this);
        }
        else
        {
            from.SendLocalizedMessage(1060640); // The item must be in your backpack to use it.
        }
    }

    private class InternalTarget : Target
    {
        private readonly RunedSwitch _item;

        public InternalTarget(RunedSwitch item) : base(0, false, TargetFlags.None) => _item = item;

        protected override void OnTarget(Mobile from, object o)
        {
            if (_item?.Deleted != false)
            {
                return;
            }

            if (o is BaseTalisman talisman)
            {
                if (talisman.Charges == 0)
                {
                    talisman.Charges = talisman.MaxCharges;
                    _item.Delete();
                    from.SendLocalizedMessage(1075100); // The item has been recharged.
                }
                else
                {
                    // You cannot recharge that item until all of its current charges have been used.
                    from.SendLocalizedMessage(1075099);
                }
            }
            else
            {
                from.SendLocalizedMessage(1046439); // That is not a valid target.
            }
        }
    }
}
