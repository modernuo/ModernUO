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
    public void SendBufferExhausted_WhileDisconnectQueued_KeepsFirstReason()
    {
        var ns = CreateAuthenticatedNetState();

        try
        {
            var capacity = ns._socket.SendBuffer.PhysicalSize;

            ns.Send(new byte[capacity + 1]); // cannot fit; queues the disconnect
            ns.Send(new byte[capacity + 2]); // same tick; must not re-report

            Assert.Contains($"needed {capacity + 1}", ns._disconnectReason);
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
