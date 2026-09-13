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
        if (!Familiar.AssistsMaster || aggressor.Hidden)
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

        // Lifecycle is BaseFamiliar.OnThink's; a master on another map is TeleportPets' problem.
        if (Mobile.Deleted || master?.Deleted != false || master.Map != Mobile.Map)
        {
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

    private void Follow(Mobile master)
    {
        Mobile.Warmode = false;
        Mobile.Combatant = null;
        Mobile.SetCurrentSpeedToActive();

        MoveTo(master, 1);
    }
}
