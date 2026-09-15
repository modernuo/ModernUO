/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ApproachOutcome.cs                                              *
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

/// <summary>Which exit <see cref="BaseAI.ApproachTarget"/> took, for policies that need more
/// than its bool.</summary>
public enum ApproachOutcome
{
    None,
    Arrived,        // already within range
    Waiting,        // frozen, casting, or the move budget has not elapsed
    DirectProgress, // greedy step succeeded and closed the distance (open ground)
    Routing,        // a PathFollower is active and working the detour
    Blocked,        // move-eligible tick took no step; stall counter still running
    GaveUp,         // stall counter exhausted on a stationary goal
    InvalidGoal     // deleted target, deleted self, or DisallowAllMoves
}
