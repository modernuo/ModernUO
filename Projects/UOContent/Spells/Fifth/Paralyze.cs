using System;
using Server.Mobiles;
using Server.Spells.Chivalry;
using Server.Targeting;

namespace Server.Spells.Fifth
{
    public class ParalyzeSpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Paralyze",
            "An Ex Por",
            218,
            9012,
            Reagent.Garlic,
            Reagent.MandrakeRoot,
            Reagent.SpidersSilk
        );

        public ParalyzeSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fifth;

        public void Target(Mobile m)
        {
            if (Core.AOS && (m.Frozen || m.Paralyzed || m.Spell?.IsCasting == true && m.Spell is not PaladinSpell))
            {
                Caster.SendLocalizedMessage(1061923); // The target is already frozen.
            }
            else if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                double duration;

                if (Core.AOS)
                {
                    var secs = (int)(GetDamageSkill(Caster) / 10 - GetResistSkill(m) / 10);

                    if (!Core.AOS)
                    {
                        secs += 2;
                    }

                    if (!m.Player)
                    {
                        secs *= 3;
                    }

                    duration = Math.Max(secs, 0);
                }
                else
                {
                    // Algorithm: ((20% of magery) + 7) seconds [- 50% if resisted]

                    duration = 7.0 + Caster.Skills.Magery.Value * 0.2;

                    if (CheckResisted(m))
                    {
                        duration *= 0.75;
                    }
                }

                if (m is PlagueBeastLord lord)
                {
                    lord.OnParalyzed(Caster);
                    duration = 120;
                }

                m.Paralyze(TimeSpan.FromSeconds(duration));

                m.PlaySound(0x204);
                m.FixedEffect(0x376A, 6, 1);

                HarmfulSpell(m);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }
    }
}
