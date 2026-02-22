using System;
using System.Collections.Generic;
using Server.Engines.PartySystem;
using Server.Factions;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Systems.FeatureFlags;
using Server.Targeting;
using Core = Server.Core;

namespace Server.SkillHandlers
{
    public static class DetectHidden
    {
        // Debounce tracking: (stealther Serial, detector Serial) -> last detection time
        private static readonly Dictionary<(uint, uint), DateTime> PassiveDetectDebounce =
            new();

        private const int PassiveDetectDebounceMs = 3000; // 3 seconds
        private const int DebounceCleanupThresholdMs = 10000; // Clean up entries older than 10 seconds

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

        // Clean up old debounce entries to prevent memory bloat and stale entries
        private static void CleanupDebounceCache()
        {
            var now = Core.Now;
            var entriesToRemove = new List<(uint, uint)>();

            foreach (var entry in PassiveDetectDebounce)
            {
                if ((now - entry.Value).TotalMilliseconds > DebounceCleanupThresholdMs)
                {
                    entriesToRemove.Add(entry.Key);
                }
            }

            foreach (var key in entriesToRemove)
            {
                PassiveDetectDebounce.Remove(key);
            }
        }

        // For testing: clear the debounce cache to prevent cross-test contamination
        public static void ClearDebounceCache()
        {
            PassiveDetectDebounce.Clear();
        }

        // Passive detection: called each time a stealther takes a step.
        // Scans nearby players with Detect Hidden skill and may reveal the stealther.
        // NOTE: OSI uncertain - The exact chance calculation and distance dropoff is unknown.
        // We use a ±10 variance on both skills, matching active detection mechanics.
        public static void PassiveDetect(Mobile stealther)
        {
            if (!ContentFeatureFlags.PassiveDetectHidden)
            {
                return;
            }

            var map = stealther.Map;
            if (map == null || !stealther.Hidden || map != Map.Felucca)
            {
                return;
            }

            // Periodically clean up old debounce entries
            CleanupDebounceCache();

            foreach (var detector in map.GetMobilesInRange<PlayerMobile>(stealther.Location, 4))
            {
                TryDetectStealther(detector, stealther);
            }
        }

        // Check if a detector can passively detect a stealther.
        // Returns true if detection was successful (and the stealther was revealed).
        public static bool TryDetectStealther(Mobile detector, Mobile stealther)
        {
            if (!ContentFeatureFlags.PassiveDetectHidden || stealther == detector || !stealther.Hidden ||
                !detector.Alive)
            {
                return false;
            }

            var detectSkill = detector.Skills.DetectHidden.Value;
            if (detectSkill <= 0)
            {
                return false;
            }

            if (detector.AccessLevel < stealther.AccessLevel)
            {
                return false;
            }

            // Check if the detector can harm the stealther (excludes blessed, dead, etc)
            if (!detector.CanBeHarmful(stealther, false))
            {
                return false;
            }

            // Exclude party members
            var stealtherParty = Party.Get(stealther);
            var detectorParty = Party.Get(detector);
            if (stealtherParty != null && stealtherParty == detectorParty)
            {
                return false;
            }

            // Exclude guild members and allies
            if (stealther is PlayerMobile pm1 && detector is PlayerMobile pm2)
            {
                var guild1 = pm1.Guild as Guild;
                var guild2 = pm2.Guild as Guild;

                if (guild1 != null && guild2 != null)
                {
                    if (guild1 == guild2 || guild1.IsAlly(guild2))
                    {
                        return false;
                    }
                }
            }

            // Check debounce: prevent constant detection
            var key = ((uint)stealther.Serial, (uint)detector.Serial);
            if (PassiveDetectDebounce.TryGetValue(key, out var lastDetect))
            {
                if ((Core.Now - lastDetect).TotalMilliseconds < PassiveDetectDebounceMs)
                {
                    return false;
                }
            }

            var ss = detectSkill + Utility.Random(21) - 10;
            var ts = stealther.Skills.Hiding.Value + Utility.Random(21) - 10;

            if (ss >= ts)
            {
                stealther.RevealingAction();
                stealther.SendLocalizedMessage(500814); // You have been revealed!
                PassiveDetectDebounce[key] = Core.Now;
                return true;
            }

            return false;
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

                    // Check for traps and trapped containers in a single loop
                    foreach (var item in src.Map.GetItemsInRange(p, range))
                    {
                        if (item is BaseFactionTrap factionTrap)
                        {
                            if (Faction.Find(src) != null &&
                                src.CheckTargetSkill(SkillName.DetectHidden, factionTrap, 80.0, 100.0))
                            {
                                src.SendLocalizedMessage(
                                    1042712, // You reveal a trap placed by a faction:
                                    true,
                                    $" {(factionTrap.Faction == null ? "" : factionTrap.Faction.Definition.FriendlyName)}"
                                );

                                factionTrap.Visible = true;
                                factionTrap.BeginConceal();

                                foundAnyone = true;
                            }
                        }
                        else if (item is BaseTrap trap && !trap.Visible)
                        {
                            // High Seas (Publish 79): Requires 75 Detect Hidden to detect dungeon traps
                            if (Core.HS && srcSkill < 75.0)
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
                        else if (item is TrappableContainer container && container.TrapType != TrapType.None)
                        {
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
