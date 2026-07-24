/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BlocklistGate.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Net;

namespace Server.Network.Bans.Blocklist;

/// <summary>
/// Pure accept-gate decision, isolated for testability. Allocation-free: no closures on the accept
/// path. Returns true when the connection should be denied; <paramref name="shouldReport"/> indicates
/// whether this hit should be contributed to the ban channel (once per <see cref="PromotedGuard"/> TTL).
/// </summary>
public static class BlocklistGate
{
    public static bool Evaluate(
        IPAddress ip, bool whitelisted, PromotedGuard guard, long nowTicks,
        bool reportHits, long ttlMs, out bool shouldReport)
    {
        shouldReport = false;
        if (whitelisted || !FileBlocklist.IsBanned(ip))
        {
            return false; // pass
        }
        if (reportHits)
        {
            shouldReport = guard.TryMark(ip.ToUInt128(), nowTicks, ttlMs);
        }
        return true; // deny
    }
}
