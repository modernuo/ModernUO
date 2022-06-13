using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items
{
    [SerializationGenerator(0, false)]
    public partial class Wool : Item, IDyable
    {
        [Constructible]
        public Wool(int amount = 1) : base(0xDF8)
        {
            Stackable = true;
            Weight = 4.0;
            Amount = amount;
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
            {
                return false;
            }

            Hue = sender.DyedHue;

            return true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(502655); // What spinning wheel do you wish to spin this on?
                from.Target = new PickWheelTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public virtual void OnSpun(ISpinningWheel wheel, Mobile from, int hue)
        {
            Item item = new DarkYarn(3);
            item.Hue = hue;

            from.AddToBackpack(item);
            from.SendLocalizedMessage(1010576); // You put the balls of yarn in your backpack.
        }

        private class PickWheelTarget : Target
        {
            private readonly Wool m_Wool;

            public PickWheelTarget(Wool wool) : base(3, false, TargetFlags.None) => m_Wool = wool;

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Wool.Deleted)
                {
                    return;
                }

                var wheel = targeted as ISpinningWheel;

                if (wheel == null && targeted is AddonComponent component)
                {
                    wheel = component.Addon as ISpinningWheel;
                }

                if (wheel is Item)
                {
                    if (!m_Wool.IsChildOf(from.Backpack))
                    {
                        from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                    }
                    else if (wheel.Spinning)
                    {
                        from.SendLocalizedMessage(502656); // That spinning wheel is being used.
                    }
                    else
                    {
                        m_Wool.Consume();
                        wheel.BeginSpin(m_Wool.OnSpun, from, m_Wool.Hue);
                    }
                }
                else
                {
                    from.SendLocalizedMessage(502658); // Use that on a spinning wheel.
                }
            }
        }
    }

    [SerializationGenerator(0, false)]
    public partial class TaintedWool : Wool
    {
        [Constructible]
        public TaintedWool(int amount = 1) : base(0x101F)
        {
            Stackable = true;
            Weight = 4.0;
            Amount = amount;
        }

        public override void OnSpun(ISpinningWheel wheel, Mobile from, int hue)
        {
            Item item = new DarkYarn();
            item.Hue = hue;

            from.AddToBackpack(item);
            from.SendLocalizedMessage(1010574); // You put a ball of yarn in your backpack.
        }
    }
}
