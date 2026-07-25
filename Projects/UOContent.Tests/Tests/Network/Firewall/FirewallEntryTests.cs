using System.Net;
using Server.Network;
using Xunit;

namespace Server.Tests;

public class FirewallEntryTests
{
    [Theory]
    [InlineData("192.168.1.1", "192.168.1.1")]
    [InlineData("::ffff:192.168.1.1", "192.168.1.1")]
    [InlineData("ae45:c5c7:9372:2d3a:413c:6490:017d:2c18", "ae45:c5c7:9372:2d3a:413c:6490:017d:2c18")]
    public void TestSingleIpFirewallEntry(string ip, string startAndEndIp)
    {
        var entry = new SingleIpFirewallEntry(ip);

        Assert.Equal(entry.MaxIpAddress, entry.MinIpAddress);
        Assert.Equal(IPAddress.Parse(startAndEndIp), entry.MinIpAddress.ToIpAddress());
    }

    [Theory]
    [InlineData("192.168.1.1/24", "192.168.1.0", "192.168.1.255")]
    [InlineData("::ffff:10.25.3.250/112", "10.25.0.0", "10.25.255.255")]
    [InlineData("::ffff:10.25.5.250/124", "10.25.5.240", "10.25.5.255")]
    [InlineData("::ffff:192.168.1.1/120", "192.168.1.0", "192.168.1.255")]
    [InlineData("d15e:d490:03cd:f9e1:95d8:8413:e6b8:e226/88", "D15E:D490:03CD:F9E1:95D8:8400::", "D15E:D490:03CD:F9E1:95D8:84FF:FFFF:FFFF")]
    [InlineData("2001:4860:4860::8888/32", "2001:4860:0000:0000:0000:0000:0000:0000", "2001:4860:FFFF:FFFF:FFFF:FFFF:FFFF:FFFF")]
    public void TestCidrPatternIpFirewallEntry(string cidr, string startIp, string endIp)
    {
        var entry = new CidrFirewallEntry(cidr);

        Assert.Equal(IPAddress.Parse(startIp), entry.MinIpAddress.ToIpAddress());
        Assert.Equal(IPAddress.Parse(endIp), entry.MaxIpAddress.ToIpAddress());
    }
}
