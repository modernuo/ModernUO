using System;
using System.Collections.Generic;

namespace Server.Spells.Bushido
{
    public class HonorableExecution : SamuraiMove
    {
        private static readonly Dictionary<Mobile, HonorableExecutionTimer> _table = new();

        public override int BaseMana => 0;
        public override double RequiredSkill => 25.0;

        public override TextDefinition AbilityMessage =>
            new(1063122); // You better kill your enemy with your next hit or you'll be rather sorry...

        public override double GetDamageScalar(Mobile attacker, Mobile defender) =>
            // TODO: 20 -> Perfection
            1.0 + attacker.Skills.Bushido.Value * 20 / 10000;

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker) || !CheckMana(attacker, true))
            {
                return;
            }

            ClearCurrentMove(attacker);
            RemovePenalty(attacker);

            HonorableExecutionTimer timer;

            if (!defender.Alive)
            {
                attacker.FixedParticles(0x373A, 1, 17, 0x7E2, EffectLayer.Waist);

                var bushido = attacker.Skills.Bushido.Value;
                bushido *= bushido;

                attacker.Hits += 20 + (int)(bushido / 480.0);

                var swingBonus = Math.Max(1, (int)(bushido / 720.0));

                timer = new HonorableExecutionTimer(attacker, swingBonus);
            }
            else
            {
                var mods = new List<object>
                {
                    new ResistanceMod(ResistanceType.Physical, "PhysicalResistHonorableExecution", -40),
                    new ResistanceMod(ResistanceType.Fire, "FireResistHonorableExecution", -40),
                    new ResistanceMod(ResistanceType.Cold, "ColdResistHonorableExecution", -40),
                    new ResistanceMod(ResistanceType.Poison, "PoisonResistHonorableExecution", -40),
                    new ResistanceMod(ResistanceType.Energy, "EnergyResistHonorableExecution", -40)
                };

                var resSpells = attacker.Skills.MagicResist.Value;

                if (resSpells > 0.0)
                {
                    mods.Add(new DefaultSkillMod(SkillName.MagicResist, "MagicResistHonorableExecution", true, -resSpells));
                }

                timer = new HonorableExecutionTimer(attacker, mods);
            }

            _table[attacker] = timer;
            timer.Start();

            attacker.Delta(MobileDelta.WeaponDamage);
            CheckGain(attacker);
        }

        public static int GetSwingBonus(Mobile target) => _table.TryGetValue(target, out var info) ? info.m_SwingBonus : 0;

        public static bool IsUnderPenalty(Mobile target) => _table.TryGetValue(target, out var info) && info.m_Penalty;

        public static void RemovePenalty(Mobile target)
        {
            if (_table.Remove(target, out var timer))
            {
                timer.Clear();
            }
        }

        private class HonorableExecutionTimer : Timer
        {
            public readonly Mobile m_Mobile;
            public readonly List<object> m_Mods;
            public readonly bool m_Penalty;
            public readonly int m_SwingBonus;

            public HonorableExecutionTimer(Mobile from, List<object> mods) : this(TimeSpan.FromSeconds(7.0), from, 0, mods, mods != null)
            {
            }

            public HonorableExecutionTimer(Mobile from, int swingBonus) : this(TimeSpan.FromSeconds(20.0), from, swingBonus)
            {
            }

            public HonorableExecutionTimer(
                TimeSpan duration, Mobile from, int swingBonus, List<object> mods = null, bool penalty = false
            ) : base(duration)
            {
                m_Mobile = from;
                m_SwingBonus = swingBonus;
                m_Mods = mods;
                m_Penalty = penalty;

                Apply();
            }

            protected override void OnTick()
            {
                m_Mobile?.Delta(MobileDelta.WeaponDamage);
                RemovePenalty(m_Mobile);
            }

            public void Apply()
            {
                if (m_Mods == null)
                {
                    return;
                }

                for (var i = 0; i < m_Mods.Count; ++i)
                {
                    var mod = m_Mods[i];

                    if (mod is ResistanceMod resistanceMod)
                    {
                        m_Mobile.AddResistanceMod(resistanceMod);
                    }
                    else if (mod is SkillMod skillMod)
                    {
                        m_Mobile.AddSkillMod(skillMod);
                    }
                }
            }

            public void Clear()
            {
                Stop();

                if (m_Mods == null)
                {
                    return;
                }

                for (var i = 0; i < m_Mods.Count; ++i)
                {
                    var mod = m_Mods[i];

                    if (mod is ResistanceMod resistanceMod)
                    {
                        m_Mobile?.RemoveResistanceMod(resistanceMod);
                    }
                    else if (mod is SkillMod skillMod)
                    {
                        m_Mobile?.RemoveSkillMod(skillMod);
                    }
                }
            }
        }
    }
}
