using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Cotton : Item, IDyable
{
    [Constructible]
    public Cotton(int amount = 1) : base(0xDF9)
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
        Item item = new SpoolOfThread(6);
        item.Hue = hue;

        from.AddToBackpack(item);
        from.SendLocalizedMessage(1010577); // You put the spools of thread in your backpack.
    }

    private class PickWheelTarget : Target
    {
        private readonly Cotton m_Cotton;

        public PickWheelTarget(Cotton cotton) : base(3, false, TargetFlags.None) => m_Cotton = cotton;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Cotton.Deleted)
            {
                return;
            }

            var wheel = targeted as ISpinningWheel;

            if (wheel == null && targeted is AddonComponent component)
            {
                wheel = component.Addon as ISpinningWheel;
            }

            if (wheel is not Item)
            {
                from.SendLocalizedMessage(502658); // Use that on a spinning wheel.
                return;
            }

            if (!m_Cotton.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (wheel.Spinning)
            {
                from.SendLocalizedMessage(502656); // That spinning wheel is being used.
            }
            else
            {
                m_Cotton.Consume();
                wheel.BeginSpin(m_Cotton.OnSpun, from, m_Cotton.Hue);
            }
        }
    }
}