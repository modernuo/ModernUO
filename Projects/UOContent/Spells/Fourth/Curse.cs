using System.Collections.Generic;
using Server.Targeting;

namespace Server.Spells.Fourth
{
    public class CurseSpell : MagerySpell, ISpellTargetingMobile
    {
        private static readonly SpellInfo _info = new(
            "Curse",
            "Des Sanct",
            227,
            9031,
            Reagent.Nightshade,
            Reagent.Garlic,
            Reagent.SulfurousAsh
        );

        private static readonly HashSet<Mobile> m_UnderEffect = new();

        public CurseSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override SpellCircle Circle => SpellCircle.Fourth;

        public void Target(Mobile m)
        {
            if (CheckHSequence(m))
            {
                SpellHelper.Turn(Caster, m);

                SpellHelper.CheckReflect((int)Circle, Caster, ref m);

                SpellHelper.AddStatCurse(Caster, m, StatType.Str);
                SpellHelper.DisableSkillCheck = true;
                SpellHelper.AddStatCurse(Caster, m, StatType.Dex);
                SpellHelper.AddStatCurse(Caster, m, StatType.Int);
                SpellHelper.DisableSkillCheck = false;

                // On OSI you CAN curse yourself and get this effect.
                if (Caster.Player && m.Player /*&& Caster != m */ && !UnderEffect(m))
                {
                    var duration = SpellHelper.GetDuration(Caster, m);
                    m_UnderEffect.Add(m);
                    Timer.StartTimer(duration, () => RemoveEffect(m));
                    m.UpdateResistances();
                }

                m.Spell?.OnCasterHurt();

                m.Paralyzed = false;

                m.FixedParticles(0x374A, 10, 15, 5028, EffectLayer.Waist);
                m.PlaySound(0x1E1);

                var percentage = (int)(SpellHelper.GetOffsetScalar(Caster, m, true) * 100);
                var length = SpellHelper.GetDuration(Caster, m);

                var args = $"{percentage}\t{percentage}\t{percentage}\t{10}\t{10}\t{10}\t{10}";

                BuffInfo.AddBuff(m, new BuffInfo(BuffIcon.Curse, 1075835, 1075836, length, m, args));

                HarmfulSpell(m);
            }

            FinishSequence();
        }

        public override void OnCast()
        {
            Caster.Target = new SpellTargetMobile(this, TargetFlags.Harmful, Core.ML ? 10 : 12);
        }

        public static void RemoveEffect(Mobile m)
        {
            m_UnderEffect.Remove(m);

            m.UpdateResistances();
        }

        public static bool UnderEffect(Mobile m) => m_UnderEffect.Contains(m);
    }
}
