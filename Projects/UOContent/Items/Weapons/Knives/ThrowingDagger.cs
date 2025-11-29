using System;
using ModernUO.Serialization;
using Server.Targeting;
using CalcMoves = Server.Movement.Movement;

namespace Server.Items;

[Flippable(0xF52, 0xF51)]
[SerializationGenerator(0, false)]
public partial class ThrowingDagger : Item
{
    [Constructible]
    public ThrowingDagger() : base(0xF52) => Layer = Layer.OneHanded;

    public override double DefaultWeight => 1.0;

    public override string DefaultName => "a throwing dagger";

    public override void OnDoubleClick(Mobile from)
    {
        if (from.Items.Contains(this))
        {
            var t = new InternalTarget(this);
            from.Target = t;
        }
        else
        {
            from.SendMessage("You must be holding that weapon to use it.");
        }
    }

    private class InternalTarget : Target
    {
        private readonly ThrowingDagger m_Dagger;

        public InternalTarget(ThrowingDagger dagger) : base(10, false, TargetFlags.Harmful) => m_Dagger = dagger;

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Dagger.Deleted)
            {
                return;
            }

            if (!from.Items.Contains(m_Dagger))
            {
                from.SendMessage("You must be holding that weapon to use it.");
            }
            else if (targeted is Mobile m && m != from && from.HarmfulCheck(m))
            {
                var to = from.GetDirectionTo(m);

                from.Direction = to;

                from.Animate(from.Mounted ? 26 : 9, 7, 1, true, false, 0);

                if (Utility.RandomDouble() >= Math.Sqrt(m.Dex / 100.0) * 0.8)
                {
                    from.MovingEffect(m, 0x1BFE, 7, 1, false, false, 0x481, 0);

                    AOS.Damage(m, from, Utility.Random(5, from.Str / 10), 100, 0, 0, 0, 0);

                    m_Dagger.MoveToWorld(m.Location, m.Map);
                }
                else
                {
                    var p = m.Location;
                    CalcMoves.Offset(to, ref p);

                    p.X += Utility.Random(-1, 3);
                    p.Y += Utility.Random(-1, 3);

                    m_Dagger.MoveToWorld(p, m.Map);

                    from.MovingEffect(m_Dagger, 0x1BFE, 7, 1, false, false, 0x481, 0);

                    from.SendMessage("You miss.");
                }
            }
        }
    }
}
