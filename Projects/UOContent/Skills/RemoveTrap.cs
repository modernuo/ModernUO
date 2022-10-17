using System;
using Server.Factions;
using Server.Items;
using Server.Network;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class RemoveTrap
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.RemoveTrap].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            if (m.Skills.Lockpicking.Value < 50)
            {
                m.SendLocalizedMessage(502366); // You do not know enough about locks.  Become better at picking locks.
            }
            else if (m.Skills.DetectHidden.Value < 50)
            {
                m.SendLocalizedMessage(502367); // You are not perceptive enough.  Become better at detect hidden.
            }
            else
            {
                m.Target = new InternalTarget();
                m.SendLocalizedMessage(502368); // Which trap will you attempt to disarm?
            }

            return TimeSpan.FromSeconds(10.0); // 10 second delay before being able to re-use a skill
        }

        private class InternalTarget : Target
        {
            public InternalTarget() : base(2, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    from.SendLocalizedMessage(502816); // You feel that such an action would be inappropriate
                }
                else if (targeted is TrappableContainer targ)
                {
                    if (targ.TrapType == TrapType.None)
                    {
                        from.SendLocalizedMessage(502373); // That doesn't appear to be trapped
                        return;
                    }

                    if (targ.RootParent == from)
                    {
                        from.Direction = from.GetDirectionTo(targ);
                    }

                    from.PlaySound(0x241);

                    if (from.CheckTargetSkill(SkillName.RemoveTrap, targ, targ.TrapPower, targ.TrapPower + 30))
                    {
                        targ.TrapPower = 0;
                        targ.TrapLevel = 0;
                        targ.TrapType = TrapType.None;
                        from.SendLocalizedMessage(502377); // You successfully render the trap harmless
                    }
                    else
                    {
                        from.SendLocalizedMessage(502372); // You fail to disarm the trap... but you don't set it off
                    }
                }
                else if (targeted is BaseFactionTrap trap)
                {
                    var faction = Faction.Find(from);
                    var kit = from.Backpack?.FindItemByType<FactionTrapRemovalKit>();

                    var isOwner = trap.Placer == from || trap.Faction?.IsCommander(from) == true;

                    if (faction == null)
                    {
                        // You may not disarm faction traps unless you are in an opposing faction
                        from.SendLocalizedMessage(1010538);
                    }
                    else if (trap.Faction != null && faction == trap.Faction && !isOwner)
                    {
                        from.SendLocalizedMessage(1010537); // You may not disarm traps set by your own faction!
                    }
                    else if (!isOwner && kit == null)
                    {
                        // You must have a trap removal kit at the base level of your pack to disarm a faction trap.
                        from.SendLocalizedMessage(1042530);
                    }
                    else
                    {
                        if (Core.ML && isOwner || from.CheckTargetSkill(SkillName.RemoveTrap, trap, 80.0, 100.0) &&
                            from.CheckTargetSkill(SkillName.Tinkering, trap, 80.0, 100.0))
                        {
                            from.PrivateOverheadMessage(
                                MessageType.Regular,
                                trap.MessageHue,
                                trap.DisarmMessage,
                                from.NetState
                            );

                            if (!isOwner)
                            {
                                var silver = faction.AwardSilver(from, trap.SilverFromDisarm);

                                if (silver > 0)
                                {
                                    // You have been granted faction silver for removing the enemy trap :
                                    from.SendLocalizedMessage(1008113, true, silver.ToString("N0"));
                                }
                            }

                            trap.Delete();
                        }
                        else
                        {
                            from.SendLocalizedMessage(502372); // You fail to disarm the trap... but you don't set it off
                        }

                        if (!isOwner)
                        {
                            kit.ConsumeCharge(from);
                        }
                    }
                }
                else
                {
                    from.SendLocalizedMessage(502373); // That doesn't appear to be trapped
                }
            }
        }
    }
}
