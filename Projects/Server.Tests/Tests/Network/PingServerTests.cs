using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

public class PingServerTests
{
    /// <summary>
    /// A ping listener must receive on its own family: an IPv4 UdpClient around an IPv6 socket
    /// throws on every ReceiveAsync, and a receive loop retrying that spins the startup thread.
    /// </summary>
    [SkippableTheory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    public async Task ListenerReceivesOnItsFamily(string address)
    {
        var ip = IPAddress.Parse(address);
        Skip.If(ip.AddressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6, "IPv6 unavailable");

        using var listener = PingServer.CreateListener(new IPEndPoint(ip, 0));
        Assert.NotNull(listener);
        var port = ((IPEndPoint)listener.Client.LocalEndPoint!).Port;

        using var sender = new UdpClient(ip.AddressFamily);
        byte[] payload = [1, 2, 3, 4];
        await sender.SendAsync(payload, new IPEndPoint(ip, port));

        var result = await listener.ReceiveAsync().WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(payload, result.Buffer);
    }
}
