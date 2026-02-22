using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Network;
using Server.Network;

namespace Server.Tests.Network;

public static class PacketTestUtilities
{
    private static nint _testListener;
    private static int _testPort;
    private static readonly List<Socket> _testSocketClients = [];

    public static Span<byte> Compile(this Packet p) => p.Compile(false, out var length).AsSpan(0, length);

    /// <summary>
    /// Creates a NetState for unit testing.
    /// Uses a real Socket and RingSocket with actual buffers.
    /// Must be disposed after use (use 'using' statement).
    /// </summary>
    public static NetState CreateTestNetState()
    {
        NetState.Slice(); // Process disconnects/disposes

        for (var i = _testSocketClients.Count - 1; i >= 0; i--)
        {
            var sock = _testSocketClients[i];
            if (!sock.Connected)
            {
                sock.Dispose();
                _testSocketClients.RemoveAt(i);
            }
        }

        var ring = NetState.Ring;

        // Create a test listener if we don't have one (using the ring for RIO-compatible sockets)
        if (_testListener == 0)
        {
            // Disable rate limiter for tests - we don't want connection attempts to be throttled
            // NetState.DisableRateLimiter();

            _testListener = ring.CreateListener("127.0.0.1", 0, 128);
            if (_testListener == -1)
            {
                throw new InvalidOperationException("Failed to create test listener");
            }

            _testPort = SocketHelper.GetLocalEndPoint(_testListener)?.Port ?? 0;
            if (_testPort == 0)
            {
                throw new InvalidOperationException("Failed to get test listener port");
            }
        }

        // Queue an accept operation
        ring.PrepareAccept(_testListener, 0, 0, IORingUserData.EncodeAccept());
        ring.Submit();

        Core._now = DateTime.UtcNow;

        // Create a client socket and connect to trigger the accept
        var testSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        testSocket.Connect(IPAddress.Loopback, _testPort);
        _testSocketClients.Add(testSocket);

        // Slice until we have a new NetState instance added
        // AcceptEx is asynchronous, so we may need to wait/retry
        const int maxRetries = 100;
        for (var i = 0; i < maxRetries; i++)
        {
            NetState.Slice();

            // Get the latest instance connected.
            foreach (var ns in NetState.Instances)
            {
                if (ns.ConnectedOn == Core._now)
                {
                    return ns;
                }
            }

            // Wait a bit for AcceptEx to complete
            if (i < maxRetries - 1)
            {
                System.Threading.Thread.Sleep(1);
            }
        }

        throw new Exception("Failed to slice for test NetState instance after retries");
    }
}
