/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BanExemptionsTests.cs                                           *
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
using System.Text;
using Server.Network;
using Server.Network.Bans;
using Xunit;

namespace Server.Tests.Network.Exemptions;

// Addresses come from TEST-NET-1 (192.0.2.0/24) to stay clear of the other ban test classes if xUnit runs
// them concurrently.
public class BanExemptionsTests
{
    private static readonly IPAddress _listed = IPAddress.Parse("192.0.2.10");
    private static readonly IPAddress _unlisted = IPAddress.Parse("192.0.2.11");

    private static void WithManualAllowlist(string contents) =>
        ManualAllowlist.LoadForTesting(BlocklistSnapshot.Build(Encoding.ASCII.GetBytes(contents), out _, out _));

    private static void WithEmptyManualAllowlist() => ManualAllowlist.LoadForTesting(BlocklistSnapshot.Empty);

    [Fact]
    public void Manual_allowlist_exempts_behavioral_contributions()
    {
        WithManualAllowlist("192.0.2.10");

        // Subtracting from the blocklist does nothing for behavioural detections, which never consult it.
        Assert.True(BanExemptions.IsExempt(_listed, BanReasons.ForeignProtocol, NeverCalled));
        Assert.True(BanExemptions.IsExempt(_listed, BanReasons.RateLimit, NeverCalled));
        Assert.True(BanExemptions.IsExempt(_listed, BanReasons.SilentConnect, NeverCalled));
        Assert.True(BanExemptions.IsExempt(_listed, BanReasons.InvalidSeed, NeverCalled));
    }

    [Fact]
    public void Manual_allowlist_covers_cidr_entries()
    {
        // Carve-outs are CIDRs, so a shared-CGNAT player is only covered if ranges work here.
        WithManualAllowlist("192.0.2.0/24");

        Assert.True(BanExemptions.IsExempt(_listed, BanReasons.RateLimit, NeverCalled));
        Assert.True(BanExemptions.IsExempt(IPAddress.Parse("192.0.2.254"), BanReasons.RateLimit, NeverCalled));
        Assert.False(BanExemptions.IsExempt(IPAddress.Parse("192.0.3.1"), BanReasons.RateLimit, AlwaysFalse));
    }

    [Fact]
    public void Manual_bans_are_never_exempt_even_when_allowlisted()
    {
        WithManualAllowlist("192.0.2.10");

        // An explicit decision outranks the operator's own carve-out, and must not cost a strike.
        Assert.False(BanExemptions.IsExempt(_listed, BanReasons.Manual, NeverCalled));
    }

    [Fact]
    public void Unopted_reasons_are_never_exempt()
    {
        WithManualAllowlist("192.0.2.10");

        Assert.False(BanExemptions.IsExempt(_listed, BanReasons.Blocklist, NeverCalled));
        Assert.False(BanExemptions.IsExempt(_listed, "some-future-reason", NeverCalled));
    }

    [Fact]
    public void Manual_allowlist_does_not_spend_the_earned_lists_strikes()
    {
        WithManualAllowlist("192.0.2.10");

        // Unconditional, so the revocable list must not be consulted -- that would burn a strike.
        Assert.True(BanExemptions.IsExempt(_listed, BanReasons.RateLimit, NeverCalled));
    }

    [Fact]
    public void Falls_through_to_the_login_allowlist_when_not_file_listed()
    {
        WithEmptyManualAllowlist();

        var consulted = 0;

        var result = BanExemptions.IsExempt(
            _unlisted,
            BanReasons.RateLimit,
            (_, _) =>
            {
                consulted++;
                return true;
            }
        );

        Assert.True(result);
        Assert.Equal(1, consulted);
    }

    [Fact]
    public void Null_address_is_never_exempt()
    {
        WithEmptyManualAllowlist();

        Assert.False(BanExemptions.IsExempt(null, BanReasons.RateLimit, NeverCalled));
    }

    private static bool NeverCalled(IPAddress address, string reason)
    {
        Assert.Fail("The login allowlist must not be consulted once the answer is already decided.");
        return false;
    }

    private static bool AlwaysFalse(IPAddress address, string reason) => false;
}
