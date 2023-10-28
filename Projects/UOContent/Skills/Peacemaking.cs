using System;
using Server.Engines.ConPVP;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class Peacemaking
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Peacemaking].Callback = OnUse;
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
            from.SendLocalizedMessage(1049525); // Whom do you wish to calm?
            from.Target = new InternalTarget(from, instrument);
            from.NextSkillTime = Core.TickCount + 21600000;
        }

        private class InternalTarget : Target
        {
            private readonly BaseInstrument m_Instrument;
            private bool m_SetSkillTime = true;

            public InternalTarget(Mobile from, BaseInstrument instrument) : base(
                BaseInstrument.GetBardRange(from, SkillName.Peacemaking),
                false,
                TargetFlags.None
            ) =>
                m_Instrument = instrument;

            protected override void OnTargetFinish(Mobile from)
            {
                if (m_SetSkillTime)
                {
                    from.NextSkillTime = Core.TickCount;
                }
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                from.RevealingAction();

                if (targeted is not Mobile targ)
                {
                    from.SendLocalizedMessage(1049528); // You cannot calm that!
                }
                else if (from.Region.IsPartOf<SafeZone>())
                {
                    from.SendMessage("You may not peacemake in this area.");
                }
                else if (targ.Region.IsPartOf<SafeZone>())
                {
                    from.SendMessage("You may not peacemake there.");
                }
                else if (!m_Instrument.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(
                        1062488
                    ); // The instrument you are trying to play is no longer in your backpack!
                }
                else
                {
                    m_SetSkillTime = false;
                    from.NextSkillTime = Core.TickCount + 10000;

                    if (targeted == from)
                    {
                        // Standard mode : reset combatants for everyone in the area

                        if (!BaseInstrument.CheckMusicianship(from))
                        {
                            from.SendLocalizedMessage(500612); // You play poorly, and there is no effect.
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }
                        else if (!from.CheckSkill(SkillName.Peacemaking, 0.0, 120.0))
                        {
                            from.SendLocalizedMessage(500613); // You attempt to calm everyone, but fail.
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }
                        else
                        {
                            from.NextSkillTime = Core.TickCount + 5000;
                            m_Instrument.PlayInstrumentWell(from);
                            m_Instrument.ConsumeUse(from);

                            var map = from.Map;

                            if (map != null)
                            {
                                var range = BaseInstrument.GetBardRange(from, SkillName.Peacemaking);

                                var calmed = false;

                                foreach (var m in from.GetMobilesInRange(range))
                                {
                                    var bc = m as BaseCreature;
                                    if (bc?.Uncalmable == true || bc?.AreaPeaceImmune == true || m == from ||
                                        !from.CanBeHarmful(m, false))
                                    {
                                        continue;
                                    }

                                    calmed = true;

                                    // You hear lovely music, and forget to continue battling!
                                    m.SendLocalizedMessage(500616);
                                    m.Combatant = null;
                                    m.Warmode = false;

                                    if (bc?.BardPacified == false)
                                    {
                                        bc.Pacify(from, Core.Now + TimeSpan.FromSeconds(1.0));
                                    }
                                }

                                if (!calmed)
                                {
                                    // You play hypnotic music, but there is nothing in range for you to calm.
                                    from.SendLocalizedMessage(1049648);
                                }
                                else
                                {
                                    from.SendLocalizedMessage(500615); // You play your hypnotic music, stopping the battle.
                                }
                            }
                        }
                    }
                    else
                    {
                        // Target mode : pacify a single target for a longer duration
                        var bc = targ as BaseCreature;

                        if (!from.CanBeHarmful(targ, false))
                        {
                            from.SendLocalizedMessage(1049528);
                            m_SetSkillTime = true;
                        }
                        else if (bc?.Uncalmable == true)
                        {
                            from.SendLocalizedMessage(1049526); // You have no chance of calming that creature.
                            m_SetSkillTime = true;
                        }
                        else if (bc?.BardPacified == true)
                        {
                            from.SendLocalizedMessage(1049527); // That creature is already being calmed.
                            m_SetSkillTime = true;
                        }
                        else if (!BaseInstrument.CheckMusicianship(from))
                        {
                            from.SendLocalizedMessage(500612); // You play poorly, and there is no effect.
                            from.NextSkillTime = Core.TickCount + 5000;
                            m_Instrument.PlayInstrumentBadly(from);
                            m_Instrument.ConsumeUse(from);
                        }
                        else
                        {
                            var diff = m_Instrument.GetDifficultyFor(targ) - 10.0;
                            var music = from.Skills.Musicianship.Value;

                            if (music > 100.0)
                            {
                                diff -= (music - 100.0) * 0.5;
                            }

                            if (!from.CheckTargetSkill(SkillName.Peacemaking, targ, diff - 25.0, diff + 25.0))
                            {
                                from.SendLocalizedMessage(1049531); // You attempt to calm your target, but fail.
                                m_Instrument.PlayInstrumentBadly(from);
                                m_Instrument.ConsumeUse(from);
                            }
                            else
                            {
                                m_Instrument.PlayInstrumentWell(from);
                                m_Instrument.ConsumeUse(from);

                                from.NextSkillTime = Core.TickCount + 5000;
                                targ.Combatant = null;
                                targ.Warmode = false;

                                if (bc != null)
                                {
                                    from.SendLocalizedMessage(1049532); // You play hypnotic music, calming your target.

                                    var seconds = 100 - diff / 1.5;

                                    if (seconds > 120)
                                    {
                                        seconds = 120;
                                    }
                                    else if (seconds < 10)
                                    {
                                        seconds = 10;
                                    }

                                    bc.Pacify(from, Core.Now + TimeSpan.FromSeconds(seconds));
                                }
                                else
                                {
                                    from.SendLocalizedMessage(1049532); // You play hypnotic music, calming your target.

                                    // You hear lovely music, and forget to continue battling!
                                    targ.SendLocalizedMessage(500616);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
