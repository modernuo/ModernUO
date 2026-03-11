using System;
using System.Collections.Generic;
using Server.Collections;
using Server.Engines.PartySystem;
using Server.Factions;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Systems.FeatureFlags;
using Server.Targeting;

namespace Server.SkillHandlers;

public static class DetectHidden
{
    // Debounce tracking: (stealther, detector) -> last detection time
    private static readonly Dictionary<(Mobile, Mobile), long> PassiveDetectDebounce = [];

    private const int PassiveDetectDebounceMs = 3000;     // 3 seconds
    private const int DebounceCleanupIntervalMs = 10000;  // Run cleanup every 10 seconds
    private const int DebounceExpiryMs = 10000;           // Remove entries older than 10 seconds
    private static long _lastCleanupTime;

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

    // Clean up old debounce entries to prevent memory bloat
    private static void CleanupDebounceCache(long now)
    {
        using var entriesToRemove = PooledRefQueue<(Mobile, Mobile)>.Create();

        foreach (var entry in PassiveDetectDebounce)
        {
            if (now - entry.Value > DebounceExpiryMs)
            {
                entriesToRemove.Enqueue(entry.Key);
            }
        }

        while (entriesToRemove.Count > 0)
        {
            PassiveDetectDebounce.Remove(entriesToRemove.Dequeue());
        }
    }

    // For testing: clear the debounce cache to prevent cross-test contamination
    internal static void ClearDebounceCache()
    {
        PassiveDetectDebounce.Clear();
    }

    // Passive detection: check if a detector can passively detect a stealther.
    // Called via OnMovement when either party moves within range.
    // Returns true if detection was successful (and the stealther was revealed).
    // NOTE: OSI uncertain - The exact chance calculation and distance dropoff is unknown.
    // We use a ±10 variance on both skills, matching active detection mechanics.
    public static bool TryDetectStealther(Mobile detector, Mobile stealther)
    {
        if (!ContentFeatureFlags.PassiveDetectHidden || stealther == detector || !stealther.Hidden || !detector.Alive)
        {
            return false;
        }

        // Felucca PvP only
        if (stealther.Map != Map.Felucca)
        {
            return false;
        }

        var detectSkill = detector.Skills.DetectHidden.Value;
        if (detectSkill <= 0)
        {
            return false;
        }

        var now = Core.TickCount;

        // Debounce: skip pairs already checked recently (cheap dictionary lookup before expensive checks)
        var key = (stealther, detector);
        if (PassiveDetectDebounce.TryGetValue(key, out var lastDetect) && now - lastDetect < PassiveDetectDebounceMs)
        {
            return false;
        }

        // Periodic cleanup (amortized, not every call)
        if (now - _lastCleanupTime > DebounceCleanupIntervalMs)
        {
            _lastCleanupTime = now;
            CleanupDebounceCache(now);
        }

        // Excludes blessed, dead, bonded pets, and region-based PvP rules
        if (stealther.AccessLevel > AccessLevel.Player || !detector.CanBeHarmful(stealther, false))
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
        if (stealther.Guild is Guild sg && detector.Guild is Guild dg && (sg == dg || sg.IsAlly(dg)))
        {
            return false;
        }

        var ss = detectSkill + Utility.Random(21) - 10;
        var ts = stealther.Skills.Hiding.Value + Utility.Random(21) - 10;

        if (ss >= ts)
        {
            stealther.RevealingAction();
            stealther.SendLocalizedMessage(500814); // You have been revealed!
            PassiveDetectDebounce[key] = now;
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
            var srcSkill = src.Skills.DetectHidden.Value;
            var range = (int)(srcSkill / 10.0);

            if (targ is TrappableContainer container && container.TrapType != TrapType.None)
            {
                // Direct container targeting: show [trapped] if within detection range and skill check passes
                if (src.InRange(container.GetWorldLocation(), range) &&
                    src.CheckSkill(SkillName.DetectHidden, 0.0, 100.0))
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
            else if (targ is not Item && (targ is not Mobile m || m == src))
            {
                // Area scan only when targeting self or the ground
                var p = targ switch
                {
                    Mobile mobile => mobile.Location,
                    IPoint3D d    => new Point3D(d),
                    _             => src.Location
                };

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
                        else if (item is BaseTrap { Visible: false } trap)
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
                    }
                }
            }

            if (!foundAnyone)
            {
                src.SendLocalizedMessage(500817); // You can see nothing hidden there.
            }

            // Calculate how much time has passed since the targeter was opened
            var ticksSinceTargeter = (int)(Core.TickCount - (src.NextSkillTime - 30000));
            var remainingCooldown = Math.Max(0, 10000 - ticksSinceTargeter);
            src.NextSkillTime = Core.TickCount + remainingCooldown;
        }
    }
}
