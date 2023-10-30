using System;
using System.Collections.Generic;
using Server.Collections;

namespace Server.Spells.Spellweaving
{
    public class EssenceOfWindSpell : ArcanistSpell
    {
        private static readonly SpellInfo _info = new("Essence of Wind", "Anathrae", -1);

        private static readonly Dictionary<Mobile, EssenceOfWindTimer> _table = new();

        public EssenceOfWindSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
        {
        }

        public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(3.0);

        public override double RequiredSkill => 52.0;
        public override int RequiredMana => 40;

        public override void OnCast()
        {
            if (CheckSequence())
            {
                Caster.PlaySound(0x5C6);

                var damage = 25 + FocusLevel;

                var skill = Caster.Skills.Spellweaving.Value;

                var duration = TimeSpan.FromSeconds((int)(skill / 24) + FocusLevel);

                var fcMalus = FocusLevel + 1;
                var ssiMalus = 2 * (FocusLevel + 1);

                using var queue = PooledRefQueue<Mobile>.Create();
                foreach (var m in Caster.GetMobilesInRange(5 + FocusLevel))
                {
                    if (Caster == m || !Caster.InLOS(m) || !SpellHelper.ValidIndirectTarget(Caster, m) ||
                        !Caster.CanBeHarmful(m, false))
                    {
                        continue;
                    }

                    queue.Enqueue(m);
                }

                while (queue.Count > 0)
                {
                    var m = queue.Dequeue();
                    Caster.DoHarmful(m);

                    SpellHelper.Damage(this, m, damage, 0, 0, 100, 0, 0);

                    if (CheckResisted(m))
                    {
                        continue;
                    }

                    var t = new EssenceOfWindTimer(m, fcMalus, ssiMalus, duration);
                    t.Start();

                    _table[m] = t;

                    BuffInfo.AddBuff(
                        m,
                        new BuffInfo(BuffIcon.EssenceOfWind, 1075802, duration, m, $"{fcMalus}\t{ssiMalus}")
                    );
                }
            }

            FinishSequence();
        }

        public static int GetFCMalus(Mobile m) => _table.TryGetValue(m, out var timer) ? timer._fcMalus : 0;

        public static int GetSSIMalus(Mobile m) => _table.TryGetValue(m, out var timer) ? timer._ssiMalus : 0;

        public static bool IsDebuffed(Mobile m) => _table.ContainsKey(m);

        public static void StopDebuffing(Mobile m)
        {
            if (_table.TryGetValue(m, out var timer))
            {
                timer.DoExpire();
            }
        }

        private class EssenceOfWindTimer : Timer
        {
            private Mobile _defender;
            internal int _fcMalus;
            internal int _ssiMalus;

            internal EssenceOfWindTimer(Mobile defender, int fcMalus, int ssiMalus, TimeSpan duration) : base(duration)
            {
                _defender = defender;
                _fcMalus = fcMalus;
                _ssiMalus = ssiMalus;
            }

            protected override void OnTick()
            {
                DoExpire();
            }

            internal void DoExpire()
            {
                Stop();
                _table.Remove(_defender);

                BuffInfo.RemoveBuff(_defender, BuffIcon.EssenceOfWind);
            }
        }
    }
}
