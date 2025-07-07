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

            return TimeSpan.FromSeconds(30.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget() : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                from.NextSkillTime = Core.TickCount;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is not Mobile targ)
                {
                    from.SendLocalizedMessage(500399); // There is little chance of getting money from that!
                }
                else if (targ.Player) // We can't beg from players
                {
                    from.SendLocalizedMessage(500398); // Perhaps just asking would work better.
                }
                else if (!targ.Body.IsHuman) // Make sure the NPC is human
                {
                    from.SendLocalizedMessage(500399); // There is little chance of getting money from that!
                }
                else if (!from.InRange(targ, 2))
                {
                    if (!targ.Female)
                    {
                        from.SendLocalizedMessage(500401); // You are too far away to beg from him.
                    }
                    else
                    {
                        from.SendLocalizedMessage(500402); // You are too far away to beg from her.
                    }
                }
                // If we're on a mount, who would give us money? TODO: guessed it's removed since ML
                else if (!Core.ML && from.Mounted)
                {
                    from.SendLocalizedMessage(500404); // They seem unwilling to give you any money.
                }
                else
                {
                    from.RevealingAction();

                    // Face each other
                    from.Direction = from.GetDirectionTo(targ);
                    targ.Direction = targ.GetDirectionTo(from);

                    from.Animate(32, 5, 1, true, false, 0); // Bow

                    new InternalTimer(from, targ).Start();
                    return;
                }

                from.NextSkillTime = Core.TickCount;
            }

            private class InternalTimer : Timer
            {
                private readonly Mobile _from;
                private readonly Mobile _target;

                public InternalTimer(Mobile from, Mobile target) : base(TimeSpan.FromSeconds(2.0))
                {
                    _from = from;
                    _target = target;
                }

                protected override void OnTick()
                {
                    var theirPack = _target.Backpack;

                    var badKarmaChance = 0.5 - (double)_from.Karma / 8570;

                    if (theirPack == null)
                    {
                        _from.SendLocalizedMessage(500404); // They seem unwilling to give you any money.
                    }
                    else if (_from.Karma < 0 && badKarmaChance > Utility.RandomDouble())
                    {
                        // Thou dost not look trustworthy... no gold for thee today!
                        _target.PublicOverheadMessage(MessageType.Regular, _target.SpeechHue, 500406);
                    }
                    else if (_from.CheckTargetSkill(SkillName.Begging, _target, 0.0, 100.0))
                    {
                        var toConsume = theirPack.GetAmount(typeof(Gold)) / 10;
                        var max = Math.Clamp(10 + _from.Fame / 2500, 10, 14);

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
                                _target.PublicOverheadMessage(MessageType.Regular, _target.SpeechHue, 500405);

                                var gold = new Gold(consumed);

                                _from.AddToBackpack(gold);
                                _from.PlaySound(gold.GetDropSound());

                                if (_from.Karma > -3000)
                                {
                                    var toLose = _from.Karma + 3000;

                                    if (toLose > 40)
                                    {
                                        toLose = 40;
                                    }

                                    Titles.AwardKarma(_from, -toLose, true);
                                }
                            }
                            else
                            {
                                // I have not enough money to give thee any!
                                _target.PublicOverheadMessage(MessageType.Regular, _target.SpeechHue, 500407);
                            }
                        }
                        else
                        {
                            // I have not enough money to give thee any!
                            _target.PublicOverheadMessage(MessageType.Regular, _target.SpeechHue, 500407);
                        }
                    }
                    else
                    {
                        _target.SendLocalizedMessage(500404); // They seem unwilling to give you any money.
                    }

                    const int TargeterCooldown = 30000; // 30s
                    const int SkillCooldown = 10000;    // 10s

                    // Calculate how much time has passed since the targeter was opened
                    int ticksSinceTargeter = (int)(Core.TickCount - (_from.NextSkillTime - TargeterCooldown));
                    int remainingCooldown = Math.Max(0, SkillCooldown - ticksSinceTargeter);
                    _from.NextSkillTime = Core.TickCount + remainingCooldown;
                }
            }
        }
    }
}
