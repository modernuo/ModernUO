using System;
using System.Collections.Generic;

namespace Server.Spells.Spellweaving
{
    public class ThunderstormSpell : ArcanistSpell
    {
        private static readonly SpellInfo m_Info = new(
            "Thunderstorm",
            "Erelonia",
            -1
        );

        private static readonly Dictionary<Mobile, Timer> m_Table = new();

        public ThunderstormSpell(Mobile caster, Item scroll = null)
            : base(caster, scroll, m_Info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(1.5);

        public override double RequiredSkill => 10.0;
        public override int RequiredMana => 32;

        public override void OnCast()
        {
            if (CheckSequence())
            {
                Caster.PlaySound(0x5CE);

                var skill = Caster.Skills.Spellweaving.Value;

                var damage = Math.Max(11, 10 + (int)(skill / 24)) + FocusLevel;

                var sdiBonus = AosAttributes.GetValue(Caster, AosAttribute.SpellDamage);

                var pvmDamage = damage * (100 + sdiBonus) / 100;

                var pvpDamage = damage * (100 + Math.Min(sdiBonus, 15)) / 100;

                var range = 2 + FocusLevel;
                var duration = TimeSpan.FromSeconds(5 + FocusLevel);

                var eable = Caster.GetMobilesInRange(range);

                foreach (var m in eable)
                {
                    if (Caster == m || !SpellHelper.ValidIndirectTarget(Caster, m) || !Caster.CanBeHarmful(m, false) ||
                        !Caster.InLOS(m))
                    {
                        continue;
                    }

                    Caster.DoHarmful(m);

                    var oldSpell = m.Spell as Spell;

                    SpellHelper.Damage(this, m, m.Player && Caster.Player ? pvpDamage : pvmDamage, 0, 0, 0, 0, 100);

                    if (oldSpell == null || oldSpell == m.Spell || CheckResisted(m))
                    {
                        continue;
                    }

                    m_Table[m] = Timer.DelayCall(duration, DoExpire, m);

                    BuffInfo.AddBuff(
                        m,
                        new BuffInfo(BuffIcon.Thunderstorm, 1075800, duration, m, GetCastRecoveryMalus(m))
                    );
                }

                eable.Free();
            }

            FinishSequence();
        }

        public static int GetCastRecoveryMalus(Mobile m) => m_Table.ContainsKey(m) ? 6 : 0;

        public static void DoExpire(Mobile m)
        {
            if (!m_Table.Remove(m, out var t))
            {
                return;
            }

            t.Stop();

            BuffInfo.RemoveBuff(m, BuffIcon.Thunderstorm);
        }
    }
}
