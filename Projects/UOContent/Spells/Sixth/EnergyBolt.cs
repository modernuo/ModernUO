using Server.Targeting;

namespace Server.Spells.Sixth
{
    public class EnergyBoltSpell : MagerySpell, ITargetingSpell<Mobile>
    {
        private static readonly SpellInfo _info = new(
            "Energy Bolt",
            "Corp Por",
            230,
            9022,
            Reagent.BlackPearl,
            Reagent.Nightshade
        );

        public EnergyBoltSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Sixth;

        public override bool DelayedDamage => true;

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                var source = Caster;

                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, ref source, ref m);

                double damage;

                if (Core.AOS)
                {
                    damage = GetNewAosDamage(40, 1, 5, m);
                }
                else
                {
                    damage = Utility.Random(24, 18);

                    if (CheckResisted(m))
                    {
                        damage *= 0.75;

                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    // Scale damage based on evalint and resist
                    damage *= GetDamageScalar(m);
                }

                source.MovingParticles(m, 0x379F, 7, 0, false, true, 0, 0, 3043, 4043, 0x211, EffectLayer.RightHand, 0);
                source.PlaySound(0x20A);

                SpellHelper.Damage(this, m, damage, 0, 0, 0, 0, 100);
            }
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTarget<Mobile>(this, TargetFlags.Harmful);
        }
    }
}
