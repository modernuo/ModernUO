using System;
using System.Collections.Generic;

namespace Server.Spells.Spellweaving
{
    public class EssenceOfWindSpell : ArcanistSpell
    {
        private static readonly SpellInfo m_Info = new("Essence of Wind", "Anathrae", -1);

        private static readonly Dictionary<Mobile, EssenceOfWindTimer> m_Table = new();

        public EssenceOfWindSpell(Mobile caster, Item scroll = null) : base(caster, scroll, m_Info)
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

                var range = 5 + FocusLevel;
                var damage = 25 + FocusLevel;

                var skill = Caster.Skills.Spellweaving.Value;

                var duration = TimeSpan.FromSeconds((int)(skill / 24) + FocusLevel);

                var fcMalus = FocusLevel + 1;
                var ssiMalus = 2 * (FocusLevel + 1);

                var eable = Caster.GetMobilesInRange(range);

                foreach (var m in eable)
                {
                    if (Caster == m || !Caster.InLOS(m) || !SpellHelper.ValidIndirectTarget(Caster, m) ||
                        !Caster.CanBeHarmful(m, false))
                    {
                        continue;
                    }

                    Caster.DoHarmful(m);

                    SpellHelper.Damage(this, m, damage, 0, 0, 100, 0, 0);

                    if (CheckResisted(m))
                    {
                        continue;
                    }

                    var t = new EssenceOfWindTimer(m, fcMalus, ssiMalus, duration);
                    t.Start();

                    m_Table[m] = t;

                    BuffInfo.AddBuff(
                        m,
                        new BuffInfo(
                            BuffIcon.EssenceOfWind,
                            1075802,
                            duration,
                            m,
                            $"{fcMalus.ToString()}\t{ssiMalus.ToString()}"
                        )
                    );
                }

                eable.Free();
            }

            FinishSequence();
        }

        public static int GetFCMalus(Mobile m) => m_Table.TryGetValue(m, out var timer) ? timer._fcMalus : 0;

        public static int GetSSIMalus(Mobile m) => m_Table.TryGetValue(m, out var timer) ? timer._ssiMalus : 0;

        public static bool IsDebuffed(Mobile m) => m_Table.ContainsKey(m);

        public static void StopDebuffing(Mobile m, bool message)
        {
            if (m_Table.TryGetValue(m, out var timer))
            {
                timer.DoExpire(message);
            }
        }

        private class EssenceOfWindTimer : Timer
        {
            private readonly Mobile _defender;
            internal readonly int _fcMalus;
            internal readonly int _ssiMalus;

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

            internal void DoExpire(bool message = true)
            {
                Stop();
                m_Table.Remove(_defender);

                BuffInfo.RemoveBuff(_defender, BuffIcon.EssenceOfWind);
            }
        }
    }
}
