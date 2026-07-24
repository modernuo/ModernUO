/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PromotedGuardTests.cs                                           *
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
using Server.Network.Bans.Blocklist;
using Xunit;

namespace Server.Tests.Network.Bans.Blocklist;

public class PromotedGuardTests
{
    [Fact]
    public void First_mark_true_then_suppressed_until_ttl()
    {
        var g = new PromotedGuard();
        Assert.True(g.TryMark((UInt128)42, 1000, 5000));
        Assert.False(g.TryMark((UInt128)42, 2000, 5000)); // within TTL
        Assert.True(g.TryMark((UInt128)42, 6001, 5000));  // expired → re-mark
    }

    [Fact]
    public void Sweep_removes_expired_entries_allowing_remark()
    {
        var g = new PromotedGuard();
        Assert.True(g.TryMark((UInt128)7, 0, 1000));
        Assert.False(g.TryMark((UInt128)7, 500, 1000)); // still within TTL

        g.Sweep(500); // not yet expired, sweep should not remove it
        Assert.False(g.TryMark((UInt128)7, 999, 1000));

        g.Sweep(1001); // now expired, sweep removes it
        Assert.True(g.TryMark((UInt128)7, 1002, 1000)); // fresh mark, not "still marked"
    }
}
