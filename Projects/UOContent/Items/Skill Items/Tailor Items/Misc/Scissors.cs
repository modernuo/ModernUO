using ModernUO.Serialization;
using Server.Targeting;

namespace Server.Items;

public interface IScissorable
{
    bool Scissor(Mobile from, Scissors scissors);
}

[Flippable(0xf9f, 0xf9e)]
[SerializationGenerator(0, false)]
public partial class Scissors : Item
{
    [Constructible]
    public Scissors() : base(0xF9F)
    {
    }

    public override double DefaultWeight => 1.0;

    public override void OnDoubleClick(Mobile from)
    {
        from.SendLocalizedMessage(502434); // What should I use these scissors on?

        from.Target = new InternalTarget(this);
    }

    public static bool CanScissor(Mobile from, IScissorable obj)
    {
        if (obj is Item item && item.Nontransferable)
        {
            from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
            return false;
        }

        // TODO: Move other general checks from the different implementations here

        return true;
    }

    private class InternalTarget : Target
    {
        private readonly Scissors m_Item;

        public InternalTarget(Scissors item) : base(2, false, TargetFlags.None) => m_Item = item;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Item.Deleted)
            {
                return;
            }

            if (Core.AOS && targeted == from)
            {
                // "That doesn't seem like the smartest thing to do." / "That was an encounter you don't wish to repeat." / "Ha! You missed!"
                from.SendLocalizedMessage(1062845 + Utility.Random(3));
            }
            else if (Core.SE && Utility.RandomDouble() < 0.80 && (from.Direction & Direction.Running) != 0 &&
                     Core.TickCount - from.LastMoveTime < from.ComputeMovementSpeed(from.Direction))
            {
                // Didn't your parents ever tell you not to run with scissors in your hand?!
                from.SendLocalizedMessage(1063305);
            }
            else if (targeted is Item item and IScissorable scissorable && (targeted is PlagueBeastInnard or PlagueBeastMutationCore || item.Movable))
            {
                if (CanScissor(from, scissorable) && scissorable.Scissor(from, m_Item))
                {
                    from.PlaySound(0x248);
                }
            }
            else
            {
                from.SendLocalizedMessage(502440); // Scissors can not be used on that to produce anything.
            }
        }

        protected override void OnNonlocalTarget(Mobile from, object targeted)
        {
            if (targeted is not PlagueBeastInnard and not PlagueBeastMutationCore)
            {
                base.OnNonlocalTarget(from, targeted);
                return;
            }

            var scissorable = (IScissorable)targeted;

            if (CanScissor(from, scissorable) && scissorable.Scissor(from, m_Item))
            {
                from.PlaySound(0x248);
            }
        }
    }
}
