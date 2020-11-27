using Server.Targeting;

namespace Server.Spells.First
{
    public class MagicArrowSpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo m_Info = new(
            "Magic Arrow",
            "In Por Ylem",
            212,
            9041,
            Reagent.SulfurousAsh
        );

        public MagicArrowSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.First;

        public override bool DelayedDamageStacking => !Core.AOS;

        public override bool DelayedDamage => true;

        public void Target(Mobile m)
        {
            if (m == null)
            {
                return;
            }

            if (!Caster.CanSee(m))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (CheckHSequence(m))
            {
                var source = Caster;

                SpellHelper.Turn(source, m);

                SpellHelper.CheckReflect((int)Circle, ref source, ref m);

                double damage;

                if (Core.AOS)
                {
                    damage = GetNewAosDamage(10, 1, 4, m);
                }
                else
                {
                    damage = Utility.Random(4, 4);

                    if (CheckResisted(m))
                    {
                        damage *= 0.75;

                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    damage *= GetDamageScalar(m);
                }

                source.MovingParticles(m, 0x36E4, 5, 0, false, false, 3006, 0, 0);
                source.PlaySound(0x1E5);

                SpellHelper.Damage(this, m, damage, 0, 100, 0, 0, 0);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }
    }
}
