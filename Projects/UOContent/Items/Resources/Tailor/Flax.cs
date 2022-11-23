using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

[SerializationGenerator(0, false)]
public partial class Flax : Item
{
    [Constructible]
    public Flax(int amount = 1) : base(0x1A9C)
    {
        Stackable = true;
        Weight = 1.0;
        Amount = amount;
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
        private readonly Flax m_Flax;

        public PickWheelTarget(Flax flax) : base(3, false, TargetFlags.None) => m_Flax = flax;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Flax.Deleted)
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

            if (!m_Flax.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (wheel.Spinning)
            {
                from.SendLocalizedMessage(502656); // That spinning wheel is being used.
            }
            else
            {
                m_Flax.Consume();
                wheel.BeginSpin(m_Flax.OnSpun, from, m_Flax.Hue);
            }
        }
    }
}