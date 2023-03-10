using System;
using Server.Items;
using Server.Misc;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class Begging
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Begging].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.RevealingAction();

            m.Target = new InternalTarget();
            m.RevealingAction();

            m.SendLocalizedMessage(500397); // To whom do you wish to grovel?

            return TimeSpan.FromHours(6.0);
        }

        private class InternalTarget : Target
        {
            private bool m_SetSkillTime = true;

            public InternalTarget() : base(12, false, TargetFlags.None)
            {
            }

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

                var number = -1;

                if (targeted is Mobile targ)
                {
                    if (targ.Player) // We can't beg from players
                    {
                        number = 500398; // Perhaps just asking would work better.
                    }
                    else if (!targ.Body.IsHuman) // Make sure the NPC is human
                    {
                        number = 500399; // There is little chance of getting money from that!
                    }
                    else if (!from.InRange(targ, 2))
                    {
                        if (!targ.Female)
                        {
                            number = 500401; // You are too far away to beg from him.
                        }
                        else
                        {
                            number = 500402; // You are too far away to beg from her.
                        }
                    }
                    // If we're on a mount, who would give us money? TODO: guessed it's removed since ML
                    else if (!Core.ML && from.Mounted)
                    {
                        number = 500404; // They seem unwilling to give you any money.
                    }
                    else
                    {
                        // Face each other
                        from.Direction = from.GetDirectionTo(targ);
                        targ.Direction = targ.GetDirectionTo(from);

                        from.Animate(32, 5, 1, true, false, 0); // Bow

                        new InternalTimer(from, targ).Start();

                        m_SetSkillTime = false;
                    }
                }
                else // Not a Mobile
                {
                    number = 500399; // There is little chance of getting money from that!
                }

                if (number != -1)
                {
                    from.SendLocalizedMessage(number);
                }
            }

            private class InternalTimer : Timer
            {
                private readonly Mobile m_From;
                private readonly Mobile m_Target;

                public InternalTimer(Mobile from, Mobile target) : base(TimeSpan.FromSeconds(2.0))
                {
                    m_From = from;
                    m_Target = target;
                }

                protected override void OnTick()
                {
                    var theirPack = m_Target.Backpack;

                    var badKarmaChance = 0.5 - (double)m_From.Karma / 8570;

                    if (theirPack == null)
                    {
                        m_From.SendLocalizedMessage(500404); // They seem unwilling to give you any money.
                    }
                    else if (m_From.Karma < 0 && badKarmaChance > Utility.RandomDouble())
                    {
                        // Thou dost not look trustworthy... no gold for thee today!
                        m_Target.PublicOverheadMessage(MessageType.Regular, m_Target.SpeechHue, 500406);
                    }
                    else if (m_From.CheckTargetSkill(SkillName.Begging, m_Target, 0.0, 100.0))
                    {
                        var toConsume = theirPack.GetAmount(typeof(Gold)) / 10;
                        var max = 10 + m_From.Fame / 2500;

                        if (max > 14)
                        {
                            max = 14;
                        }
                        else if (max < 10)
                        {
                            max = 10;
                        }

                        if (toConsume > max)
                        {
                            toConsume = max;
                        }

                        if (toConsume > 0)
                        {
                            var consumed = theirPack.ConsumeUpTo(typeof(Gold), toConsume);

                            if (consumed > 0)
                            {
                                // I feel sorry for thee...
                                m_Target.PublicOverheadMessage(MessageType.Regular, m_Target.SpeechHue, 500405);

                                var gold = new Gold(consumed);

                                m_From.AddToBackpack(gold);
                                m_From.PlaySound(gold.GetDropSound());

                                if (m_From.Karma > -3000)
                                {
                                    var toLose = m_From.Karma + 3000;

                                    if (toLose > 40)
                                    {
                                        toLose = 40;
                                    }

                                    Titles.AwardKarma(m_From, -toLose, true);
                                }
                            }
                            else
                            {
                                // I have not enough money to give thee any!
                                m_Target.PublicOverheadMessage(MessageType.Regular, m_Target.SpeechHue, 500407);
                            }
                        }
                        else
                        {
                            // I have not enough money to give thee any!
                            m_Target.PublicOverheadMessage(MessageType.Regular, m_Target.SpeechHue, 500407);
                        }
                    }
                    else
                    {
                        m_Target.SendLocalizedMessage(500404); // They seem unwilling to give you any money.
                    }

                    m_From.NextSkillTime = Core.TickCount + 10000;
                }
            }
        }
    }
}
