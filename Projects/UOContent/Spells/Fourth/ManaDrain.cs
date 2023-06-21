using System;
using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Fourth
{
    public class ManaDrainSpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Mana Drain",
            "Ort Rel",
            215,
            9031,
            Reagent.BlackPearl,
            Reagent.MandrakeRoot,
            Reagent.SpidersSilk
        );

        private static readonly HashSet<Mobile> _table = new();

        public ManaDrainSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fourth;

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                m.Spell?.OnCasterHurt();

                m.Paralyzed = false;

                if (Core.AOS)
                {
                    var toDrain = Math.Clamp(40 + (int)(GetDamageSkill(Caster) - GetResistSkill(m)), 0, m.Mana);

                    if (_table.Contains(m))
                    {
                        toDrain = 0;
                    }

                    m.FixedParticles(0x3789, 10, 25, 5032, EffectLayer.Head);
                    m.PlaySound(0x1F8);

                    if (toDrain > 0)
                    {
                        m.Mana -= toDrain;

                        _table.Add(m);
                        Timer.StartTimer(TimeSpan.FromSeconds(5.0), () => AosDelay_Callback(m, toDrain));
                    }
                }
                else
                {
                    if (CheckResisted(m))
                    {
                        m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                    }
                    else if (m.Mana > 0)
                    {
                        m.Mana -= Utility.Random(1, Math.Min(m.Mana, 100));
                    }

                    m.FixedParticles(0x374A, 10, 15, 5032, EffectLayer.Head);
                    m.PlaySound(0x1F8);
                }

                HarmfulSpell(m);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }

        private void AosDelay_Callback(Mobile m, int mana)
        {
            if (m.Alive && !m.IsDeadBondedPet)
            {
                m.Mana += mana;

                m.FixedEffect(0x3779, 10, 25);
                m.PlaySound(0x28E);
            }

            _table.Remove(m);
        }

        public override double GetResistPercent(Mobile target) => 99.0;
    }
}
