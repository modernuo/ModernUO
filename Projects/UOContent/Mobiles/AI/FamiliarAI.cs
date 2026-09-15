/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: FamiliarAI.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program. If not, see <http://www.gnu.org/licenses/>.  *
 ************************************************************************/

using System.Collections.Generic;

namespace Server.Mobiles;

/// <summary>
/// Necromancy familiars: command-immune, glued to the caster, assist its fights if combat-capable,
/// snap to it when outpaced or stuck. <see cref="Obey"/> and <see cref="Think"/> share one decision.
/// </summary>
public class FamiliarAI : BaseAI
{
    public FamiliarAI(BaseFamiliar familiar) : base(familiar)
    {
    }

    private BaseFamiliar Familiar => (BaseFamiliar)Mobile;

    // Assist targets, and the familiar itself, stay within this of the caster.
    public int LeashRange => Mobile.RangePerception;

    public override bool CanDetectHidden => false;

    public override bool Think() => Act();

    public override bool Obey() => Act();

    // Command immunity: no issue-phase side effects; rests on Come so TeleportPets still applies.
    public override OrderType IssueOrder(
        OrderType order, OrderType previous, Mobile issuer, bool resuming, Mobile interruptedTarget
    )
    {
        AITimer.Prod();
        return Mobile.Controlled ? OrderType.Come : OrderType.None;
    }

    public override void OnAggressiveAction(Mobile aggressor)
    {
        if (!Familiar.AssistsMaster || aggressor.Hidden || Familiar.ControlMaster?.Hidden == true)
        {
            return;
        }

        if (Mobile.Combatant == null)
        {
            Mobile.Warmode = true;
            Mobile.Combatant = aggressor;
            return;
        }

        base.OnAggressiveAction(aggressor);
    }

    private bool Act()
    {
        var master = Familiar.ControlMaster;

        // Deletion is BaseFamiliar.OnThink's.
        if (Mobile.Deleted || master?.Deleted != false)
        {
            return true;
        }

        // Left behind: stand down until TeleportPets or the unsummon.
        if (master.Map != Mobile.Map)
        {
            StandDown();
            return true;
        }

        // Herding outranks combat.
        if (Mobile.TargetLocation != null)
        {
            StandDown();

            if (CheckHerding())
            {
                DebugSay("Fetching for my master.");
                return true;
            }
        }

        if (Familiar.AssistsMaster && !master.Hidden && TryAssist(master))
        {
            return true;
        }

        Follow(master);
        return true;
    }

    // The caster's target; else the closest mobile in a fight with the caster's side that is
    // still fighting one of us (the caster's own Combatant expires while a monster keeps hitting).
    private bool TryAssist(Mobile master)
    {
        var target = master.Combatant;

        if (!IsValidAssistTarget(master, target))
        {
            target = Mobile.Combatant;

            if (!IsValidAssistTarget(master, target) || !IsFightingUs(master, target))
            {
                target = FindAggressor(master);

                if (target == null)
                {
                    return false;
                }
            }
        }

        if (!Mobile.InRange(master, LeashRange))
        {
            DebugSay("Too far from my master; returning.");
            return false;
        }

        Mobile.Warmode = true;
        Mobile.Combatant = target;

        if (Mobile.Combatant != target)
        {
            return false; // setter refused it
        }

        Mobile.SetCurrentSpeedToActive();
        this.DebugSayFormatted($"Assisting my master against {target.Name}.");
        MoveTo(target, Mobile.RangeFight);
        return true;
    }

    private bool IsFightingUs(Mobile master, Mobile target)
    {
        var combatant = target.Combatant;

        return combatant == Mobile || combatant == master ||
               combatant is BaseCreature { Controlled: true } pet && pet.ControlMaster == master;
    }

    // Closest mobile in a fight with the caster's side.
    private Mobile FindAggressor(Mobile master)
    {
        Mobile best = null;
        var bestDist = double.MaxValue;

        ScanAggression(master, master.Aggressors, false, ref best, ref bestDist);
        ScanAggression(master, Mobile.Aggressors, false, ref best, ref bestDist);
        ScanAggression(master, master.Aggressed, true, ref best, ref bestDist);

        return best;
    }

    // defenders: an Aggressed list, read the Defender.
    private void ScanAggression(
        Mobile master, List<AggressorInfo> list, bool defenders, ref Mobile best, ref double bestDist
    )
    {
        for (var i = 0; i < list.Count; i++)
        {
            var info = list[i];

            if (info.Expired)
            {
                continue;
            }

            var other = defenders ? info.Defender : info.Attacker;

            if (other == best || !IsValidAssistTarget(master, other) || !IsFightingUs(master, other))
            {
                continue;
            }

            var dist = master.GetDistanceToSqrt(other);

            if (dist < bestDist)
            {
                best = other;
                bestDist = dist;
            }
        }
    }

    private bool IsValidAssistTarget(Mobile master, Mobile target) =>
        target?.Deleted == false && target != Mobile && target != master && target.Alive &&
        !target.Hidden && target.Map == Mobile.Map && !target.IsDeadBondedPet &&
        target.AccessLevel == AccessLevel.Player && master.InRange(target, LeashRange) &&
        Mobile.CanBeHarmful(target, false);

    // Clears the move intent too, or a move-wake resumes the abandoned pursuit.
    private void StandDown()
    {
        Mobile.Warmode = false;
        Mobile.Combatant = null;
        Mobile.SetCurrentSpeedToActive();
        ClearMoveIntent();
    }

    private void Follow(Mobile master)
    {
        StandDown();

        MoveTo(master, 1);
        TryKeepUp(master);
    }

    // Outpaced on open ground, or given up: snap. A live detour finishes; Blocked is still counting.
    private void TryKeepUp(Mobile master)
    {
        var snap = LastApproach switch
        {
            ApproachOutcome.GaveUp         => true,
            ApproachOutcome.DirectProgress => !Mobile.InRange(master, BaseFamiliar.KeepUpRange),
            _                              => false
        };

        if (!snap || !TryFindLanding(master, out var loc))
        {
            return;
        }

        DebugSay("Keeping up with my master.");
        Mobile.SetLocation(loc, true);
        ResetApproachState();
    }

    private static readonly (int dx, int dy)[] _landingRing =
    [
        (0, 1), (1, 0), (0, -1), (-1, 0), (1, 1), (-1, 1), (1, -1), (-1, -1)
    ];

    // An adjacent tile on the caster's floor this creature can stand on.
    private bool TryFindLanding(Mobile master, out Point3D loc)
    {
        var map = master.Map;
        var start = Utility.Random(_landingRing.Length); // no favoured side

        for (var i = 0; i < _landingRing.Length; i++)
        {
            var (dx, dy) = _landingRing[(start + i) % _landingRing.Length];
            var x = master.X + dx;
            var y = master.Y + dy;

            if (map.CanSpawnMobile(x, y, master.Z - 5, master.Z + 5, Mobile.CanSwim, Mobile.CantWalk, out var z))
            {
                loc = new Point3D(x, y, z);
                return true;
            }
        }

        loc = Point3D.Zero;
        return false;
    }
}
