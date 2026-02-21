using System;
using Server.Factions;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.SkillHandlers
{
    public static class DetectHidden
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.DetectHidden].Callback = OnUse;
        }

        public static TimeSpan OnUse(Mobile src)
        {
            src.SendLocalizedMessage(500819); // Where will you search?
            src.Target = new InternalTarget();

            return TimeSpan.FromSeconds(30.0);
        }

        // Passive detection: called each time a stealther takes a step.
        // Scans nearby players with Detect Hidden skill and may reveal the stealther.
        public static void PassiveDetect(Mobile stealther)
        {
            var map = stealther.Map;
            if (map == null || !stealther.Hidden)
            {
                return;
            }

            foreach (var detector in map.GetMobilesInRange<PlayerMobile>(stealther.Location, 4))
            {
                if (detector == stealther || !detector.Alive)
                {
                    continue;
                }

                var detectSkill = detector.Skills.DetectHidden.Value;
                if (detectSkill <= 0)
                {
                    continue;
                }

                if (detector.AccessLevel < stealther.AccessLevel)
                {
                    continue;
                }

                var ss = detectSkill + Utility.Random(21) - 10;
                var ts = stealther.Skills.Hiding.Value + Utility.Random(21) - 10;

                if (ss >= ts)
                {
                    stealther.RevealingAction();
                    stealther.SendLocalizedMessage(500814); // You have been revealed!
                    return;
                }
            }
        }

        private class InternalTarget : Target
        {
            public InternalTarget() : base(12, true, TargetFlags.None)
            {
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                from.NextSkillTime = Core.TickCount;
            }

            protected override void OnTarget(Mobile src, object targ)
            {
                var foundAnyone = false;

                var p = targ switch
                {
                    Mobile mobile => mobile.Location,
                    Item item     => item.Location,
                    IPoint3D d    => new Point3D(d),
                    _             => src.Location
                };

                var srcSkill = src.Skills.DetectHidden.Value;
                var range = (int)(srcSkill / 10.0);

                if (!src.CheckSkill(SkillName.DetectHidden, 0.0, 100.0))
                {
                    range /= 2;
                }

                var house = BaseHouse.FindHouseAt(p, src.Map, 16);

                var inHouse = house?.IsFriend(src) == true;

                if (inHouse)
                {
                    range = 22;
                }

                if (range > 0)
                {
                    foreach (var trg in src.Map.GetMobilesInRange(p, range))
                    {
                        if (!trg.Hidden || src == trg)
                        {
                            continue;
                        }

                        var ss = srcSkill + Utility.Random(21) - 10;
                        var ts = trg.Skills.Hiding.Value + Utility.Random(21) - 10;

                        if (src.AccessLevel < trg.AccessLevel || ss < ts && (!inHouse || !house.IsInside(trg)))
                        {
                            continue;
                        }

                        if (trg is ShadowKnight && (trg.X != p.X || trg.Y != p.Y))
                        {
                            continue;
                        }

                        trg.RevealingAction();
                        trg.SendLocalizedMessage(500814); // You have been revealed!
                        foundAnyone = true;
                    }

                    if (Faction.Find(src) != null)
                    {
                        foreach (var trap in src.Map.GetItemsInRange<BaseFactionTrap>(p, range))
                        {
                            if (src.CheckTargetSkill(SkillName.DetectHidden, trap, 80.0, 100.0))
                            {
                                src.SendLocalizedMessage(
                                    1042712, // You reveal a trap placed by a faction:
                                    true,
                                    $" {(trap.Faction == null ? "" : trap.Faction.Definition.FriendlyName)}"
                                );

                                trap.Visible = true;
                                trap.BeginConceal();

                                foundAnyone = true;
                            }
                        }
                    }

                    // Reveal hidden dungeon traps temporarily
                    foreach (var trap in src.Map.GetItemsInRange<BaseTrap>(p, range))
                    {
                        if (trap is BaseFactionTrap || trap.Visible)
                        {
                            continue;
                        }

                        trap.Visible = true;
                        Timer.StartTimer(TimeSpan.FromSeconds(10.0), () =>
                        {
                            if (!trap.Deleted)
                            {
                                trap.Visible = false;
                            }
                        });

                        foundAnyone = true;
                    }

                    // Check for trapped containers and notify the detecting player privately
                    foreach (var container in src.Map.GetItemsInRange<TrappableContainer>(p, range))
                    {
                        if (container.TrapType == TrapType.None)
                        {
                            continue;
                        }

                        src.NetState.SendMessageLocalized(
                            container.Serial,
                            container.ItemID,
                            MessageType.Regular,
                            0x3B2,
                            3,
                            500813 // [trapped]
                        );

                        foundAnyone = true;
                    }
                }

                if (!foundAnyone)
                {
                    src.SendLocalizedMessage(500817); // You can see nothing hidden there.
                }

                const int TargeterCooldown = 30000; // 30s
                const int SkillCooldown = 10000;    // 10s

                // Calculate how much time has passed since the targeter was opened
                var ticksSinceTargeter = (int)(Core.TickCount - (src.NextSkillTime - TargeterCooldown));
                var remainingCooldown = Math.Max(0, SkillCooldown - ticksSinceTargeter);
                src.NextSkillTime = Core.TickCount + remainingCooldown;
            }
        }
    }
}
