using System;
using Server.Targeting;

namespace Server.Spells.Mysticism
{
    public class EagleStrikeSpell : MysticSpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Eagle Strike",
            "Kal Por Xen",
            -1,
            9002,
            Reagent.Bloodmoss,
            Reagent.Bone,
            Reagent.SpidersSilk,
            Reagent.MandrakeRoot
        );

        public EagleStrikeSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Third;

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                if (Core.SA && HasDelayedDamageContext(m))
                {
                    DoHurtFizzle();
                    return;
                }

                var source = Caster;

                if (SpellHelper.CheckReflect(2, ref source, ref m))
                {
                    Timer.StartTimer(TimeSpan.FromSeconds(0.5), () =>
                    {
                        /* Conjures a magical eagle that assaults the Target with its talons, dealing energy damage. */
                        source.MovingEffect(m, 0x407A, 8, 1, false, true, 0, 0);
                        source.PlaySound(0x2EE);
                    });
                }

                Caster.MovingParticles(m, 0x407A, 7, 0, false, true, 0, 0, 0xBBE, 0xFA6, 0xFFFF, 0);
                Caster.PlaySound(0x2EE);

                Timer.StartTimer(TimeSpan.FromSeconds(1.0), () => Damage(m));
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful);
        }

        private void Damage(Mobile to)
        {
            if (to == null)
            {
                return;
            }

            double damage = GetNewAosDamage(19, 1, 5, to);

            SpellHelper.Damage(this, to, damage, 0, 0, 0, 0, 100);

            to.PlaySound(0x64D);
        }
    }
}
