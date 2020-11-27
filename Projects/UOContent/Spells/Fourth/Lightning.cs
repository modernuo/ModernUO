using Server.Targeting;

namespace Server.Spells.Fourth
{
    public class LightningSpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo m_Info = new(
            "Lightning",
            "Por Ort Grav",
            239,
            9021,
            Reagent.MandrakeRoot,
            Reagent.SulfurousAsh
        );

        public LightningSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fourth;

        public override bool DelayedDamage => false;

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
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                double damage;

                if (Core.AOS)
                {
                    damage = GetNewAosDamage(23, 1, 4, m);
                }
                else
                {
                    damage = Utility.Random(12, 9);

                    if (CheckResisted(m))
                    {
                        damage *= 0.75;

                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }

                    damage *= GetDamageScalar(m);
                }

                m.BoltEffect(0);

                SpellHelper.Damage(this, m, damage, 0, 0, 0, 0, 100);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }
    }
}
