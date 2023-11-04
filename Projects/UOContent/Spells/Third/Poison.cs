using System;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Spells.Third
{
    public class PoisonSpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Poison",
            "In Nox",
            203,
            9051,
            Reagent.Nightshade
        );

        public PoisonSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Third;

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                m.Spell?.OnCasterHurt();

                m.Paralyzed = false;

                if (CheckResisted(m))
                {
                    m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                }
                else
                {
                    var total = Caster.Skills.Magery.Value;

                    if (Caster is PlayerMobile pm)
                    {
                        if (pm.DuelContext?.Started != true || pm.DuelContext.Finished ||
                            pm.DuelContext.Ruleset.GetOption("Skills", "Poisoning"))
                        {
                            total += pm.Skills.Poisoning.Value;
                        }
                    }
                    else
                    {
                        total += Caster.Skills.Poisoning.Value;
                    }

                    var dist = Caster.GetDistanceToSqrt(m);
                    int level;

                    if (Core.AOS && dist >= 3)
                    {
                        level = 0;
                    }
                    else
                    {
                        if (!Core.AOS && dist >= 3.0)
                        {
                            total -= (dist - 3.0) * 10.0;
                        }

                        if (Core.SA && dist >= 2.0)
                        {
                            total -= (dist - 2) * 31; // 240 -
                        }

                        level = total switch
                        {
                            > 200.0 when Core.SA && dist <= 2.0 => Utility.Random(10) == 0 ? 4 : 3,
                            > 199.8                            => Core.AOS || Utility.Random(10) == 0 ? 3 : 2,
                            > 170.2                             => 2,
                            > 130.2                             => 1,
                            _                                   => 0
                        };

                        if (Core.SA && dist > 2.0)
                        {
                            level -= (int)dist / 3;
                        }
                    }

                    m.ApplyPoison(Caster, Poison.GetPoison(Math.Max(level, 0)));
                }

                m.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);
                m.PlaySound(0x205);

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
