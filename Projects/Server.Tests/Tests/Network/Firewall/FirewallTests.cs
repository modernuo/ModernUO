using System;
using System.Net;
using System.Threading.Tasks;
using Server.Network;
using Xunit;

namespace Server.Tests;

public class FirewallTests
{
    private class TestFirewallEntry : BaseFirewallEntry
    {
        private readonly UInt128 _minIp;
        private readonly UInt128 _maxIp;

        public override UInt128 MinIpAddress => _minIp;
        public override UInt128 MaxIpAddress => _maxIp;

        public TestFirewallEntry(string minIp, string maxIp = null)
        {
            _minIp = IPAddress.Parse(minIp).ToUInt128();
            _maxIp = maxIp == null ? _minIp : IPAddress.Parse(maxIp).ToUInt128();
        }
    }

    [Fact]
    public void Firewall_BlocksIPAddress_WhenAdded()
    {
        var ip = IPAddress.Parse("192.168.1.1");
        var entry = new TestFirewallEntry("192.168.1.1");

        Assert.False(Firewall.IsBlocked(ip));

        Firewall.Add(entry);

        Assert.True(Firewall.IsBlocked(ip));
    }

    [Fact]
    public void Firewall_DoesNotBlockIPAddress_WhenNotAdded()
    {
        var ip = IPAddress.Parse("192.168.1.2");
        Assert.False(Firewall.IsBlocked(ip));
    }

    [Fact]
    public void Firewall_StopsBlockingIPAddress_WhenRemoved()
    {
        var ip = IPAddress.Parse("192.168.1.3");
        var entry = new TestFirewallEntry("192.168.1.3");

        Firewall.Add(entry);
        Assert.True(Firewall.IsBlocked(ip));

        Firewall.Remove(entry);
        Assert.False(Firewall.IsBlocked(ip));
    }

    [Fact]
    public void Firewall_BlocksIPRange()
    {
        var entry = new TestFirewallEntry("10.0.0.1", "10.0.0.5");

        Firewall.Add(entry);

        Assert.True(Firewall.IsBlocked(IPAddress.Parse("10.0.0.1")));
        Assert.True(Firewall.IsBlocked(IPAddress.Parse("10.0.0.3")));
        Assert.True(Firewall.IsBlocked(IPAddress.Parse("10.0.0.5")));

        Assert.False(Firewall.IsBlocked(IPAddress.Parse("10.0.0.6")));
    }

    [Fact]
    public void Firewall_CacheInvalidation_WorksOnUpdate()
    {
        var ip = IPAddress.Parse("192.168.1.10");
        var entry = new TestFirewallEntry("192.168.1.10");

        Firewall.Add(entry);
        Assert.True(Firewall.IsBlocked(ip));

        Firewall.Remove(entry);
        Assert.False(Firewall.IsBlocked(ip));
    }

    [Fact]
    public void Firewall_ReadsFirewallSetCorrectly()
    {
        var entry = new TestFirewallEntry("172.16.0.1");
        Firewall.Add(entry);

        bool found = false;
        Firewall.ReadFirewallSet(set =>
        {
            found = set.Contains(entry);
        });

        Assert.True(found);
    }

    [Fact]
    public void Firewall_IsThreadSafe()
    {
        IPAddress[] testIps = new IPAddress[256];
        for (int i = 0; i <= 255; i++)
        {
            testIps[i] = IPAddress.Parse($"192.168.0.{i}");
        }

        var entry = new TestFirewallEntry("192.168.0.1", "192.168.0.255");
        Firewall.Add(entry);

        Parallel.ForEach(testIps, ip =>
        {
            bool shouldBlock = ip.ToString().EndsWith(".1") || ip.ToString().EndsWith(".255") ||
                               (int.Parse(ip.ToString().Split('.')[3]) <= 255);
            Assert.Equal(shouldBlock, Firewall.IsBlocked(ip));
        });

        Firewall.Remove(entry);

        Parallel.ForEach(testIps, ip =>
        {
            Assert.False(Firewall.IsBlocked(ip));
        });
    }

    [Fact]
    public void Firewall_DoesNotThrowWhenRemovingNonExistentEntry()
    {
        var entry = new TestFirewallEntry("203.0.113.5");
        Assert.False(Firewall.Remove(entry));
    }

    [Fact]
    public void Firewall_CacheHandlesMultipleUpdates()
    {
        var ip = IPAddress.Parse("192.168.1.20");
        var entry = new TestFirewallEntry("192.168.1.20");

        Firewall.Add(entry);
        Assert.True(Firewall.IsBlocked(ip));

        Firewall.Remove(entry);
        Assert.False(Firewall.IsBlocked(ip));

        Firewall.Add(entry);
        Assert.True(Firewall.IsBlocked(ip));

        Firewall.Remove(entry);
        Assert.False(Firewall.IsBlocked(ip));
    }
}
