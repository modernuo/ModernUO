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

    public override bool CanDetectHidden => false;

    public override bool Think() => Act();

    public override bool Obey() => Act();

    // Command immunity: the order value is stored (Summon issues Come, which TeleportPets
    // honours) but the issue phase does nothing — no posture reset, no speed flip, no Home.
    public override OrderType IssueOrder(
        OrderType order, OrderType previous, Mobile issuer, bool resuming, Mobile interruptedTarget
    )
    {
        AITimer.Prod();
        return order;
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

        Follow(master);
        return true;
    }

    private void Follow(Mobile master)
    {
        Mobile.Warmode = false;
        Mobile.Combatant = null;
        Mobile.SetCurrentSpeedToActive();

        MoveTo(master, 1);
    }
}
