using System;
using System.Net;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

public class ServerInfoTests
{
    [Fact]
    public void AnyEncodesAsZero()
    {
        // ClassicUO and its forks read a 0 relay address as "reconnect to the login address".
        var si = new ServerInfo("Test", 0, TimeZoneInfo.Utc, new IPEndPoint(IPAddress.Any, 2593));
        Assert.Equal(0u, si.RawAddress);
    }

    [Fact]
    public void MappedIPv4EncodesAsIPv4()
    {
        var si = new ServerInfo("Test", 0, TimeZoneInfo.Utc, new IPEndPoint(IPAddress.Parse("::ffff:10.1.2.3"), 2593));
        var plain = new ServerInfo("Test", 0, TimeZoneInfo.Utc, new IPEndPoint(IPAddress.Parse("10.1.2.3"), 2593));
        Assert.Equal(plain.RawAddress, si.RawAddress);
    }

    [Fact]
    public void IPv6EncodesAsZero()
    {
        // MapToIPv4 would otherwise list the low 32 bits of the IPv6 address.
        var endPoint = new IPEndPoint(IPAddress.Parse("2001:db8::1"), 2593);
        var si = new ServerInfo("Test", 0, TimeZoneInfo.Utc, endPoint);

        Assert.Equal(0u, si.RawAddress);
        Assert.Equal(endPoint, si.Address);
    }
}
