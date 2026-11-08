using System.Net;
using System.Net.Sockets;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

/// <summary>
/// SocketHelper reads addresses from raw handles, as the accept path does. The IPv6 cases guard
/// against reading AF_INET6 as Windows' 23 on platforms where it is 10 (Linux), 30 or 28.
/// </summary>
public class SocketHelperTests
{
    [Fact]
    public void ReadsIPv4Peer()
    {
        using var pair = ConnectedPair.Create(IPAddress.Loopback, IPAddress.Loopback);

        Assert.Equal(IPAddress.Loopback, SocketHelper.GetRemoteAddress(pair.Handle));
        Assert.Equal(pair.Port, SocketHelper.GetLocalEndPoint(pair.Handle)?.Port);
    }

    [SkippableFact]
    public void ReadsIPv6Peer()
    {
        Skip.IfNot(Socket.OSSupportsIPv6, "IPv6 unavailable");

        using var pair = ConnectedPair.Create(IPAddress.IPv6Loopback, IPAddress.IPv6Loopback);

        Assert.Equal(IPAddress.IPv6Loopback, SocketHelper.GetRemoteAddress(pair.Handle));
        var local = SocketHelper.GetLocalEndPoint(pair.Handle);
        Assert.Equal(IPAddress.IPv6Loopback, local?.Address);
        Assert.Equal(pair.Port, local?.Port);
    }

    [SkippableFact]
    public void ReportsDualModeIPv4PeerAsIPv4()
    {
        Skip.IfNot(Socket.OSSupportsIPv6, "IPv6 unavailable");

        // An IPv4 client on a dual-mode listener reaches it as ::ffff:127.0.0.1.
        using var pair = ConnectedPair.Create(IPAddress.Loopback.MapToIPv6(), IPAddress.Loopback, dualMode: true);

        Assert.Equal(IPAddress.Loopback, SocketHelper.GetRemoteAddress(pair.Handle));
        Assert.Equal(IPAddress.Loopback, SocketHelper.GetLocalEndPoint(pair.Handle)?.Address);
    }

    private sealed class ConnectedPair : System.IDisposable
    {
        private Socket _listener;
        private Socket _client;
        private Socket _accepted;

        public nint Handle => _accepted.Handle;

        public int Port { get; private set; }

        public static ConnectedPair Create(IPAddress listenOn, IPAddress connectTo, bool dualMode = false)
        {
            var pair = new ConnectedPair
            {
                _listener = new Socket(listenOn.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            };

            if (listenOn.AddressFamily == AddressFamily.InterNetworkV6)
            {
                pair._listener.DualMode = dualMode;
            }

            pair._listener.Bind(new IPEndPoint(listenOn, 0));
            pair._listener.Listen(1);
            pair.Port = ((IPEndPoint)pair._listener.LocalEndPoint!).Port;

            pair._client = new Socket(connectTo.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            pair._client.Connect(connectTo, pair.Port);
            pair._accepted = pair._listener.Accept();
            return pair;
        }

        public void Dispose()
        {
            _accepted?.Dispose();
            _client?.Dispose();
            _listener?.Dispose();
        }
    }
}
