using Server.Targeting;
using System;

namespace Server.Spells.Mysticism
{
    public class BombardSpell : MysticSpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Bombard", "Corp Por Ylem",
            230,
            9022,
            Reagent.Bloodmoss,
            Reagent.Garlic,
            Reagent.SulfurousAsh,
            Reagent.DragonsBlood
        );

        public BombardSpell(Mobile caster, Item scroll) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.75);
        public override double RequiredSkill => 58.0;
        public override int RequiredMana => 20;
        public override bool DelayedDamage => true;

        public override Type[] DelayedDamageSpellFamilyStacking => AOSNoDelayedDamageStackingSelf;

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful);
        }

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                if (HasDelayedDamageContext(m))
                {
                    DoHurtFizzle();
                    return;
                }

                var source = Caster;

                if (SpellHelper.CheckReflect(6, ref source, ref m))
                {
                    Timer.StartTimer(TimeSpan.FromSeconds(0.5), () =>
                    {
                        source.MovingEffect(m, 0x1363, 12, 1, false, true, 0, 0);
                        source.PlaySound(0x64B);
                    });
                }

                Caster.MovingEffect(m, 0x1363, 12, 1, false, true, 0, 0);
                Caster.PlaySound(0x64B);

                SpellHelper.Damage(this, m, GetNewAosDamage(40, 1, 5, m), 100, 0, 0, 0, 0);

                Timer.DelayCall(TimeSpan.FromSeconds(1.2), () =>
                {
                    if (!CheckResisted(m))
                    {
                        int secs = Math.Max(0, (int)(GetDamageSkill(Caster) / 10 - GetResistSkill(m) / 10));

                        m.Paralyze(TimeSpan.FromSeconds(secs));
                        // TODO: Knockback
                    }
                });
            }

            FinishSequence();
        }
    }
}
