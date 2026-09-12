using System;
using System.Diagnostics;
using System.Net.Sockets;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

/// <summary>
/// Send-side behaviour of a NetState after its disconnect is handed to the socket.
/// </summary>
[Collection("Sequential Server Tests")]
public class NetStateDisconnectTests
{
    private static NetState CreateAuthenticatedNetState()
    {
        var ns = PacketTestUtilities.CreateTestNetState();
        ns.Account = new MockAccount(); // skips the unattached-socket sweep
        return ns;
    }

    private static (NetState ns, Mobile m) CreateClient(string name)
    {
        var ns = CreateAuthenticatedNetState();
        var m = new Mobile(World.NewMobile) { Name = name };
        m.DefaultMobileInit();
        ns.Mobile = m;
        m.NetState = ns;
        return (ns, m);
    }

    [Fact]
    public void Send_AfterDisconnectHandedToSocket_DropsPacket()
    {
        var ns = CreateAuthenticatedNetState();

        try
        {
            ns.Disconnect("test");
            NetState.Slice(); // handoff; the in-flight recv keeps it pending

            Assert.True(ns.Running);
            Assert.True(ns._socket.DisconnectPending);
            Assert.Equal(0, ns._socket.SendBuffer.ReadableBytes);

            ns.Send([0x73, 0x00]);

            Assert.True(ns.CannotSendPackets());
            Assert.Equal(0, ns._socket.SendBuffer.ReadableBytes);
        }
        finally
        {
            ns.Dispose();
        }
    }

    [Fact]
    public void Send_AfterImmediateDisconnect_DropsPacket()
    {
        var ns = CreateAuthenticatedNetState();

        // Immediate branch of RingSocket.Disconnect(): Connected drops, DisconnectPending never set,
        // Disconnected event lands next Slice()
        try
        {
            NetState.SocketManager.DisconnectImmediate(ns._socket);

            Assert.True(ns.Running);
            Assert.False(ns._socket.Connected);
            Assert.False(ns._socket.DisconnectPending);

            ns.Send([0x73, 0x00]);

            Assert.True(ns.CannotSendPackets());
            Assert.Equal(0, ns._socket.SendBuffer.ReadableBytes);
        }
        finally
        {
            ns.Dispose();
        }
    }

    [Fact]
    public void Send_BeforeDisconnect_IsDeliveredThenPeerSeesEof()
    {
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        ns.Account = new MockAccount();

        try
        {
            byte[] payload = [0x8C, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

            ns.Send(payload);
            ns.Disconnect("redirect");
            NetState.Slice(); // flush, then handoff

            Assert.True(ns._socket.DisconnectPending);

            var received = new byte[payload.Length];
            var total = 0;
            var deadline = Stopwatch.StartNew();

            while (total < payload.Length && deadline.ElapsedMilliseconds < 5000)
            {
                NetState.Slice();
                if (client.Poll(1000, SelectMode.SelectRead))
                {
                    var read = client.Receive(received, total, payload.Length - total, SocketFlags.None);
                    Assert.NotEqual(0, read);
                    total += read;
                }
            }

            Assert.Equal(payload, received);

            // FIN follows once the send completes
            var eof = -1;
            while (eof != 0 && deadline.ElapsedMilliseconds < 5000)
            {
                NetState.Slice();
                if (client.Poll(1000, SelectMode.SelectRead))
                {
                    eof = client.Receive(received);
                }
            }

            Assert.Equal(0, eof);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Send_LargeBeforeDisconnect_IsFullyDrainedThenPeerSeesEof()
    {
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        ns.Account = new MockAccount();

        try
        {
            // Several posts' worth, larger than the loopback kernel buffers, so the drain spans completions
            const int chunk = 32 * 1024;
            var payload = new byte[6 * chunk];
            new System.Random(7).NextBytes(payload);

            for (var offset = 0; offset < payload.Length; offset += chunk)
            {
                ns.Send(payload.AsSpan(offset, chunk));
            }

            ns.Disconnect("redirect");
            NetState.Slice(); // flush, then handoff

            Assert.True(ns._socket.DisconnectPending);

            var received = new byte[payload.Length];
            var total = 0;
            var deadline = Stopwatch.StartNew();

            while (total < payload.Length && deadline.ElapsedMilliseconds < 10000)
            {
                NetState.Slice();
                if (client.Poll(1000, SelectMode.SelectRead))
                {
                    var read = client.Receive(received, total, payload.Length - total, SocketFlags.None);
                    Assert.NotEqual(0, read);
                    total += read;
                }
            }

            Assert.Equal(payload.Length, total);
            Assert.Equal(payload, received);

            var eof = -1;
            while (eof != 0 && deadline.ElapsedMilliseconds < 10000)
            {
                NetState.Slice();
                if (client.Poll(1000, SelectMode.SelectRead))
                {
                    eof = client.Receive(received);
                }
            }

            Assert.Equal(0, eof);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void PendingDisconnect_ForceClosesAfterDrainTimeout()
    {
        var ns = CreateAuthenticatedNetState();

        try
        {
            ns.Disconnect("test");
            NetState.Slice(); // handoff; the in-flight recv keeps it pending and arms the deadline

            Assert.True(ns._socket.DisconnectPending);
            var armed = Core.TickCount;

            ns.CheckAlive(armed + NetState.DrainTimeoutMs - 1);
            Assert.True(ns._socket.Connected);

            ns.CheckAlive(armed + NetState.DrainTimeoutMs);
            Assert.False(ns._socket.Connected);
        }
        finally
        {
            ns.Dispose();
        }
    }

    [Fact]
    public void SendBufferExhausted_WhileDisconnectQueued_KeepsFirstReason()
    {
        var ns = CreateAuthenticatedNetState();

        try
        {
            ns.Send(new byte[NetState.MaxSendBufferSize + 1]); // cannot fit; queues the disconnect
            ns.Send(new byte[NetState.MaxSendBufferSize + 2]); // same tick; must not re-report

            Assert.Contains($"needed {NetState.MaxSendBufferSize + 1}", ns._disconnectReason);
        }
        finally
        {
            ns.Dispose();
        }
    }

    [Fact]
    public void CancelAllTrades_CancelsEveryTrade()
    {
        var (from, fromMobile) = CreateClient("from");
        var (to, toMobile) = CreateClient("to");

        try
        {
            from.AddTrade(to);
            var trade = from.FindTrade(toMobile);
            Assert.NotNull(trade);
            Assert.True(trade.Valid);

            from.CancelAllTrades();

            Assert.False(trade.Valid);
            Assert.Null(from.Trades);
            Assert.Null(to.Trades);
            Assert.Null(from.FindTrade(toMobile));
            Assert.Null(to.FindTrade(fromMobile));
        }
        finally
        {
            from.Mobile = null;
            from.Dispose();
            fromMobile.Delete();
            to.Mobile = null;
            to.Dispose();
            toMobile.Delete();
        }
    }
}
