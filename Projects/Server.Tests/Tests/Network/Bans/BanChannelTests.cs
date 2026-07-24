using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Server.Network.Bans;
using Xunit;

namespace Server.Tests.Network.Bans;

public class BanChannelTests
{
    private sealed class FakeReporter : IBanReporter
    {
        public readonly List<(IPAddress ip, TimeSpan ttl, string reason)> Reports = [];
        public readonly List<IPAddress> Retractions = [];
        public bool ThrowOnReport;

        public string Name => "fake";
        public bool CanRetract => true;
        public void Configure() { }
        public void Start(CancellationToken token) { }
        public void Stop() { }

        public void Report(IPAddress address, TimeSpan ttl, string reason)
        {
            if (ThrowOnReport)
            {
                throw new InvalidOperationException("boom");
            }

            Reports.Add((address, ttl, reason));
        }

        public void Retract(IPAddress address) => Retractions.Add(address);
    }

    [Fact]
    public void Report_FansOutToAllReporters()
    {
        var a = new FakeReporter();
        var b = new FakeReporter();
        BanChannel.ConfigureForTesting([a, b]);

        BanChannel.Report(IPAddress.Parse("1.2.3.4"), TimeSpan.FromHours(1), "rate-limit");

        Assert.Single(a.Reports);
        Assert.Single(b.Reports);
        Assert.Equal("rate-limit", a.Reports[0].reason);
    }

    [Fact]
    public void Report_SwallowsReporterException()
    {
        var bad = new FakeReporter { ThrowOnReport = true };
        var good = new FakeReporter();
        BanChannel.ConfigureForTesting([bad, good]);

        BanChannel.Report(IPAddress.Parse("1.2.3.4"), TimeSpan.FromHours(1), "manual");

        Assert.Single(good.Reports); // the throwing reporter does not block the others
    }

    [Fact]
    public void Retract_ReachesRetractCapableReporters()
    {
        var a = new FakeReporter();
        BanChannel.ConfigureForTesting([a]);

        BanChannel.Retract(IPAddress.Parse("9.9.9.9"));

        Assert.Single(a.Retractions);
    }
}
