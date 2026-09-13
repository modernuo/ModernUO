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

namespace Server.Mobiles;

/// <summary>
/// Necromancy familiars: command-immune companions that stay glued to their caster, assist
/// whatever the caster fights (if combat-capable), and snap to the caster when outpaced or
/// stuck. One decision routine owns movement for both the controlled (<see cref="Obey"/>)
/// and uncontrolled (<see cref="Think"/>) dispatch, so nothing else competes for the step.
/// </summary>
public class FamiliarAI : BaseAI
{
    public FamiliarAI(BaseFamiliar familiar) : base(familiar)
    {
    }

    private BaseFamiliar Familiar => (BaseFamiliar)Mobile;

    // How far from the caster an assist may reach: the target must be inside it and the
    // familiar must not stray outside it.
    public int LeashRange => Mobile.RangePerception;

    public override bool CanDetectHidden => false;

    public override bool Think() => Act();

    public override bool Obey() => Act();

    // Command immunity: the issue phase does nothing — no posture reset, no speed flip, no
    // Home — and the resting order is pinned at Come (which TeleportPets honours) so a
    // system-issued Attack cannot leave the familiar on an order travel ignores.
    public override OrderType IssueOrder(
        OrderType order, OrderType previous, Mobile issuer, bool resuming, Mobile interruptedTarget
    )
    {
        AITimer.Prod();
        return Mobile.Controlled ? OrderType.Come : OrderType.None;
    }

    // Retaliation: a combat familiar answers whoever hits it (or, via the caster's
    // DoHarmful, the caster); the nearest-attacker swap is the base rule.
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

        // Lifecycle is BaseFamiliar.OnThink's.
        if (Mobile.Deleted || master?.Deleted != false)
        {
            return true;
        }

        // Left behind (the caster travelled without it): stand down and wait for TeleportPets
        // or the unsummon rather than keep fighting alone.
        if (master.Map != Mobile.Map)
        {
            StandDown();
            return true;
        }

        if (CheckHerding())
        {
            DebugSay("Fetching for my master.");
            return true;
        }

        if (Familiar.AssistsMaster && !master.Hidden && TryAssist(master))
        {
            return true;
        }

        Follow(master);
        return true;
    }

    // The caster's target first, then whatever is already on us (OnAggressiveAction).
    private bool TryAssist(Mobile master)
    {
        var target = master.Combatant;

        if (!IsValidAssistTarget(master, target))
        {
            target = Mobile.Combatant;

            if (!IsValidAssistTarget(master, target))
            {
                return false;
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
            return false; // the setter refused it (region / harmful check)
        }

        Mobile.SetCurrentSpeedToActive();
        this.DebugSayFormatted($"Assisting my master against {target.Name}.");
        MoveTo(target, Mobile.RangeFight);
        return true;
    }

    private bool IsValidAssistTarget(Mobile master, Mobile target) =>
        target?.Deleted == false && target != Mobile && target != master && target.Alive &&
        !target.Hidden && target.Map == Mobile.Map && !target.IsDeadBondedPet &&
        target.AccessLevel == AccessLevel.Player && master.InRange(target, LeashRange) &&
        Mobile.CanBeHarmful(target, false);

    private void StandDown()
    {
        Mobile.Warmode = false;
        Mobile.Combatant = null;
        Mobile.SetCurrentSpeedToActive();
    }

    private void Follow(Mobile master)
    {
        StandDown();

        MoveTo(master, 1);
        TryKeepUp(master);
    }

    // Snap to the caster when simply outpaced on open ground, or once the approach has
    // given up. A live detour (Routing) is left to finish; Blocked is still counting.
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

    // First tile adjacent to the caster, on the caster's floor, that this creature can stand
    // on; none means keep walking.
    private bool TryFindLanding(Mobile master, out Point3D loc)
    {
        var map = master.Map;
        var start = Utility.Random(_landingRing.Length); // no fixed favourite side

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
