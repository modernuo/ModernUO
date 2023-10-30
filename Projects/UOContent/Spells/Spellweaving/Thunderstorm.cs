using System;
using System.Collections.Generic;
using Server.Collections;

namespace Server.Spells.Spellweaving
{
    public class ThunderstormSpell : ArcanistSpell
    {
        private static readonly SpellInfo _info = new(
            "Thunderstorm",
            "Erelonia",
            -1
        );

        private static readonly Dictionary<Mobile, TimerExecutionToken> _table = new();

        public ThunderstormSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
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

                var duration = TimeSpan.FromSeconds(5 + FocusLevel);

                using var queue = PooledRefQueue<Mobile>.Create();
                foreach (var m in Caster.GetMobilesInRange(2 + FocusLevel))
                {
                    if (Caster == m || !SpellHelper.ValidIndirectTarget(Caster, m) || !Caster.CanBeHarmful(m, false) ||
                        !Caster.InLOS(m))
                    {
                        continue;
                    }

                    queue.Enqueue(m);
                }

                while (queue.Count > 0)
                {
                    var m = queue.Dequeue();
                    Caster.DoHarmful(m);

                    var oldSpell = m.Spell as Spell;

                    SpellHelper.Damage(this, m, m.Player && Caster.Player ? pvpDamage : pvmDamage, 0, 0, 0, 0, 100);

                    if (oldSpell == null || oldSpell == m.Spell || CheckResisted(m))
                    {
                        continue;
                    }

                    StopTimer(m);

                    Timer.StartTimer(duration, () => DoExpire(m), out var timerToken);
                    _table[m] = timerToken;

                    BuffInfo.AddBuff(
                        m,
                        new BuffInfo(BuffIcon.Thunderstorm, 1075800, duration, m, GetCastRecoveryMalus(m))
                    );
                }
            }

            FinishSequence();
        }

        public static int GetCastRecoveryMalus(Mobile m) => _table.ContainsKey(m) ? 6 : 0;

        private static void StopTimer(Mobile m)
        {
            if (_table.Remove(m, out var timerToken))
            {
                timerToken.Cancel();
            }
        }

        public static void DoExpire(Mobile m)
        {
            StopTimer(m);
            BuffInfo.RemoveBuff(m, BuffIcon.Thunderstorm);
        }
    }
}
