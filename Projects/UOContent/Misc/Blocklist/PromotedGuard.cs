/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PromotedGuard.cs                                                *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Generic;

namespace Server.Network.Bans.Blocklist;

/// <summary>Suppresses re-reporting the same IP within a TTL. Accept-path thread only.</summary>
public sealed class PromotedGuard
{
    private readonly Dictionary<UInt128, long> _expiry = new();

    public bool TryMark(UInt128 ip, long nowTicks, long ttlMs)
    {
        if (_expiry.TryGetValue(ip, out var exp) && exp - nowTicks > 0)
        {
            return false;
        }
        _expiry[ip] = nowTicks + ttlMs;
        return true;
    }

    public void Sweep(long nowTicks)
    {
        if (_expiry.Count == 0)
        {
            return;
        }
        using var dead = Collections.PooledRefQueue<UInt128>.Create();
        foreach (var (ip, exp) in _expiry)
        {
            if (exp - nowTicks <= 0)
            {
                dead.Enqueue(ip);
            }
        }
        while (dead.Count > 0)
        {
            _expiry.Remove(dead.Dequeue());
        }
    }
}
