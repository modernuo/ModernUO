/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: ConnectionFiltersTests.cs                                       *
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
using System.Net;
using System.Threading;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class ConnectionFiltersTests : IDisposable
{
    public ConnectionFiltersTests() => ConnectionFilters.ResetForTesting();

    public void Dispose() => ConnectionFilters.ResetForTesting();

    [Fact]
    public void No_filters_denies_nothing()
    {
        Assert.False(ConnectionFilters.ShouldDeny(IPAddress.Parse("1.2.3.4"), out var deniedBy));
        Assert.Null(deniedBy);
    }

    [Fact]
    public void Register_is_idempotent_by_name()
    {
        ConnectionFilters.Register(new FakeFilter("dupe", deny: false));
        ConnectionFilters.Register(new FakeFilter("dupe", deny: true));

        // The second registration is ignored, so the deny:true instance never gets consulted.
        Assert.Single(ConnectionFilters.Filters);
        Assert.False(ConnectionFilters.ShouldDeny(IPAddress.Parse("1.2.3.4"), out _));
    }

    [Fact]
    public void First_denying_filter_short_circuits_and_is_named()
    {
        var first = new FakeFilter("allow-all", deny: false);
        var second = new FakeFilter("deny-all", deny: true);
        var third = new FakeFilter("never-reached", deny: true);

        ConnectionFilters.Register(first);
        ConnectionFilters.Register(second);
        ConnectionFilters.Register(third);

        Assert.True(ConnectionFilters.ShouldDeny(IPAddress.Parse("1.2.3.4"), out var deniedBy));
        Assert.Equal("deny-all", deniedBy);
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
        Assert.Equal(0, third.Calls); // short-circuited
    }

    // A filter that throws once throws for every subsequent connection, which would turn one bug into an
    // exception per accept. It must be dropped, and the connection must fail open rather than be denied
    // by a filter that never actually answered.
    [Fact]
    public void Throwing_filter_is_unregistered_and_fails_open()
    {
        var bad = new FakeFilter("bad", deny: true, throws: true);
        var good = new FakeFilter("good", deny: false);

        ConnectionFilters.Register(bad);
        ConnectionFilters.Register(good);

        Assert.False(ConnectionFilters.ShouldDeny(IPAddress.Parse("1.2.3.4"), out _));
        Assert.Single(ConnectionFilters.Filters);
        Assert.Equal("good", ConnectionFilters.Filters[0].Name);

        // Remaining filters still run on the same pass the faulty one was dropped in.
        Assert.Equal(1, good.Calls);
    }

    [Fact]
    public void Register_configures_immediately()
    {
        var filter = new FakeFilter("cfg", deny: false);
        ConnectionFilters.Register(filter);

        Assert.True(filter.Configured);
    }

    private sealed class FakeFilter : IConnectionFilter
    {
        private readonly bool _deny;
        private readonly bool _throws;

        public FakeFilter(string name, bool deny, bool throws = false)
        {
            Name = name;
            _deny = deny;
            _throws = throws;
        }

        public string Name { get; }
        public int Calls { get; private set; }
        public bool Configured { get; private set; }

        public void Register() => Configured = true;

        public void Start(CancellationToken token)
        {
        }

        public void Stop()
        {
        }

        public bool ShouldDeny(IPAddress address)
        {
            Calls++;
            if (_throws)
            {
                throw new InvalidOperationException("simulated filter bug");
            }

            return _deny;
        }
    }
}
