using System;
using Server.Targeting;

namespace Server.Spells.Seventh
{
    public class ManaVampireSpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Mana Vampire",
            "Ort Sanct",
            221,
            9032,
            Reagent.BlackPearl,
            Reagent.Bloodmoss,
            Reagent.MandrakeRoot,
            Reagent.SpidersSilk
        );

        public ManaVampireSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Seventh;

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                m.Spell?.OnCasterHurt();

                m.Paralyzed = false;

                var toDrain = 0;

                if (Core.AOS)
                {
                    toDrain = (int)(GetDamageSkill(Caster) - GetResistSkill(m));

                    if (!m.Player)
                    {
                        toDrain /= 2;
                    }
                }
                else
                {
                    if (CheckResisted(m))
                    {
                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }
                    else
                    {
                        toDrain = m.Mana;
                    }
                }

                // Will not drain more than the target has currently and will not put the caster over his maximum mana
                toDrain = Math.Clamp(toDrain, 0, Math.Min(m.Mana, Caster.ManaMax - Caster.Mana));

                if (toDrain > 0)
                {
                    m.Mana -= toDrain;
                    Caster.Mana += toDrain;
                }

                if (Core.AOS)
                {
                    m.FixedParticles(0x374A, 1, 15, 5054, 23, 7, EffectLayer.Head);
                    m.PlaySound(0x1F9);

                    Caster.FixedParticles(0x0000, 10, 5, 2054, EffectLayer.Head);
                }
                else
                {
                    m.FixedParticles(0x374A, 10, 15, 5054, EffectLayer.Head);
                    m.PlaySound(0x1F9);
                }

                HarmfulSpell(m);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }

        public override double GetResistPercent(Mobile target) => 98.0;
    }
}
