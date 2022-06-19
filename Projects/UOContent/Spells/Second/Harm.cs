using Server.Targeting;

namespace Server.Spells.Second
{
    public class HarmSpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Harm",
            "An Mani",
            212,
            Core.AOS ? 9001 : 9041,
            Reagent.Nightshade,
            Reagent.SpidersSilk
        );

        public HarmSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Second;

        public override bool DelayedDamage => false;

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                double damage;

                if (Core.AOS)
                {
                    damage = GetNewAosDamage(17, 1, 5, m);
                }
                else
                {
                    damage = Utility.Random(1, 15);

                    if (CheckResisted(m))
                    {
                        damage *= 0.75;

                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    damage *= GetDamageScalar(m);
                }

                if (!m.InRange(Caster, 2))
                {
                    damage *= 0.25; // 1/4 damage at > 2 tile range
                }
                else if (!m.InRange(Caster, 1))
                {
                    damage *= 0.50; // 1/2 damage at 2 tile range
                }

                if (Core.AOS)
                {
                    m.FixedParticles(0x374A, 10, 30, 5013, 1153, 2, EffectLayer.Waist);
                    m.PlaySound(0x0FC);
                }
                else
                {
                    m.FixedParticles(0x374A, 10, 15, 5013, EffectLayer.Waist);
                    m.PlaySound(0x1F1);
                }

                SpellHelper.Damage(this, m, damage, 0, 0, 100, 0, 0);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }

        public override double GetSlayerDamageScalar(Mobile target) => 1.0;
    }
}
