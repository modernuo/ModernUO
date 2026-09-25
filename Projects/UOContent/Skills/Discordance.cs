using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class Discordance
    {
        private static readonly Dictionary<Mobile, DiscordanceInfo> m_Table = new();

        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Discordance].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.RevealingAction();

            BaseInstrument.PickInstrument(m, OnPickedInstrument);

            return TimeSpan.FromSeconds(1.0); // Cannot use another skill for 1 second
        }

        public static void OnPickedInstrument(Mobile from, BaseInstrument instrument)
        {
            from.RevealingAction();
            from.SendLocalizedMessage(1049541); // Choose the target for your song of discordance.
            from.Target = new DiscordanceTarget(from, instrument);
            from.NextSkillTime = Core.TickCount + 6000;
        }

        public static bool GetEffect(Mobile targ, ref int effect)
        {
            if (!m_Table.TryGetValue(targ, out var info))
            {
                return false;
            }

            effect = info.m_Effect;
            return true;
        }

        private static void ProcessDiscordance(DiscordanceInfo info)
        {
            var from = info.m_From;
            var targ = info.m_Creature;
            var ends = false;

            // According to uoherald bard must remain alive, visible, and
            // within range of the target or the effect ends in 15 seconds.
            if (!targ.Alive || targ.Deleted || !from.Alive || from.Hidden)
            {
                ends = true;
            }
            else
            {
                var range = (int)targ.GetDistanceToSqrt(from);
                var maxRange = BaseInstrument.GetBardRange(from, SkillName.Discordance);

                if (from.Map != targ.Map || range > maxRange)
                {
                    ends = true;
                }
            }

            if (ends && info.m_Ending && info.m_EndTime < Core.Now)
            {
                info._timerToken.Cancel();

                info.Clear();
                m_Table.Remove(targ);
            }
            else
            {
                if (ends && !info.m_Ending)
                {
                    info.m_Ending = true;
                    info.m_EndTime = Core.Now + TimeSpan.FromSeconds(15);
                }
                else if (!ends)
                {
                    info.m_Ending = false;
                    info.m_EndTime = Core.Now;
                }

                targ.FixedEffect(0x376A, 1, 32);
            }
        }

        private class DiscordanceInfo
        {
            public readonly Mobile m_Creature;
            public readonly int m_Effect;
            public readonly Mobile m_From;
            public bool m_Ending;
            public DateTime m_EndTime;
            public TimerExecutionToken _timerToken;

            public DiscordanceInfo(Mobile from, Mobile creature, int effect)
            {
                m_From = from;
                m_Creature = creature;
                m_EndTime = Core.Now;
                m_Ending = false;
                m_Effect = effect;
            }

            public void Clear()
            {
                m_Creature.RemoveResistanceMod("Discordance");
                m_Creature.RemoveStatMod("Discordance");
                m_Creature.RemoveSkillMod("Discordance");
            }
        }

        public class DiscordanceTarget : Target
        {
            private readonly BaseInstrument m_Instrument;

            public DiscordanceTarget(Mobile from, BaseInstrument inst) : base(
                BaseInstrument.GetBardRange(from, SkillName.Discordance),
                false,
                TargetFlags.None
            ) => m_Instrument = inst;

            protected override void OnTarget(Mobile from, object target)
            {
                from.RevealingAction();
                from.NextSkillTime = Core.TickCount + 1000;

                if (!m_Instrument.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(
                        1062488
                    ); // The instrument you are trying to play is no longer in your backpack!
                }
                else if (target is Mobile targ)
                {
                    if (targ == from || targ is BaseCreature bc &&
                        (bc.BardImmune || !from.CanBeHarmful(bc, false)) &&
                        bc.ControlMaster != from)
                    {
                        from.SendLocalizedMessage(1049535); // A song of discord would have no effect on that.
                    }
                    else if (m_Table.ContainsKey(targ)) // Already discorded
                    {
                        from.SendLocalizedMessage(1049537); // Your target is already in discord.
                    }
                    else if (!targ.Player)
                    {
                        var diff = m_Instrument.GetDifficultyFor(targ) - 10.0;
                        var music = from.Skills.Musicianship.Value;

                        if (music > 100.0)
                        {
                            diff -= (music - 100.0) * 0.5;
                        }

                        if (!BaseInstrument.CheckMusicianship(from))
                        {
                            from.SendLocalizedMessage(500612); // You play poorly, and there is no effect.
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }
                        else if (from.CheckTargetSkill(SkillName.Discordance, targ, diff - 25.0, diff + 25.0))
                        {
                            from.SendLocalizedMessage(1049539); // You play the song surpressing your targets strength
                            m_Instrument.PlayInstrumentWell(from);
                            m_Instrument.ConsumeUse(from);

                            int effect;
                            double scalar;

                            if (Core.AOS)
                            {
                                var discord = from.Skills.Discordance.Value;

                                if (discord > 100.0)
                                {
                                    effect = -20 + (int)((discord - 100.0) / -2.5);
                                }
                                else
                                {
                                    effect = (int)(discord / -5.0);
                                }

                                if (Core.SE && BaseInstrument.GetBaseDifficulty(targ) >= 160.0)
                                {
                                    effect /= 2;
                                }

                                scalar = effect * 0.01;

                                targ.AddResistanceMod(new ResistanceMod(ResistanceType.Physical, "Discordance", effect));
                                targ.AddResistanceMod(new ResistanceMod(ResistanceType.Fire, "Discordance", effect));
                                targ.AddResistanceMod(new ResistanceMod(ResistanceType.Cold, "Discordance", effect));
                                targ.AddResistanceMod(new ResistanceMod(ResistanceType.Poison, "Discordance", effect));
                                targ.AddResistanceMod(new ResistanceMod(ResistanceType.Energy, "Discordance", effect));
                            }
                            else
                            {
                                effect = (int)(from.Skills.Discordance.Value / -5.0);
                                scalar = effect * 0.01;

                                targ.AddStatMod(
                                    new StatMod(
                                        StatType.Str,
                                        "Discordance",
                                        (int)(targ.RawStr * scalar),
                                        TimeSpan.Zero
                                    )
                                );
                                targ.AddStatMod(
                                    new StatMod(
                                        StatType.Int,
                                        "Discordance",
                                        (int)(targ.RawInt * scalar),
                                        TimeSpan.Zero
                                    )
                                );
                                targ.AddStatMod(
                                    new StatMod(
                                        StatType.Dex,
                                        "Discordance",
                                        (int)(targ.RawDex * scalar),
                                        TimeSpan.Zero
                                    )
                                );
                            }

                            for (var i = 0; i < targ.Skills.Length; ++i)
                            {
                                var skill = targ.Skills[i];
                                if (skill.Value > 0)
                                {
                                    targ.AddSkillMod(new DefaultSkillMod(skill.SkillName, "Discordance", true, skill.Value * scalar));
                                }
                            }

                            var info = new DiscordanceInfo(from, targ, effect.Abs());
                            Timer.StartTimer(
                                TimeSpan.Zero,
                                TimeSpan.FromSeconds(1.25),
                                () => ProcessDiscordance(info),
                                out info._timerToken
                            );

                            m_Table[targ] = info;
                        }
                        else
                        {
                            from.SendLocalizedMessage(1049540); // You fail to disrupt your target
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }

                        from.NextSkillTime = Core.TickCount + 12000;
                    }
                    else
                    {
                        m_Instrument.PlayInstrumentBadly(from);
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1049535); // A song of discord would have no effect on that.
                }
            }
        }
    }
}
