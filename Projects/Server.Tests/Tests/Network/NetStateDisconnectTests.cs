using Server.Network;
using Xunit;

namespace Server.Tests.Network;

/// <summary>
/// Send-side behaviour of a NetState that is on its way out. A disconnect is handed to the socket
/// in Slice(); from then on the transport drains what is buffered and closes. Nothing new may be
/// written, or the drain never finishes and every refused packet logs an exhaustion warning.
/// </summary>
[Collection("Sequential Server Tests")]
public class NetStateDisconnectTests
{
    private static NetState CreateAuthenticatedNetState()
    {
        var ns = PacketTestUtilities.CreateTestNetState();
        ns.Account = new MockAccount(); // keeps the unattached-socket sweep off this connection
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

        ns.Disconnect("test");
        NetState.Slice(); // hands the disconnect to the socket; a recv is in flight so it stays pending

        Assert.True(ns.Running);
        Assert.True(ns._socket.DisconnectPending);
        Assert.Equal(0, ns._socket.SendBuffer.ReadableBytes);

        ns.Send([0x73, 0x00]);

        Assert.True(ns.CannotSendPackets());
        Assert.Equal(0, ns._socket.SendBuffer.ReadableBytes);
    }

    [Fact]
    public void SendBufferExhausted_WhileDisconnectQueued_KeepsFirstReason()
    {
        var ns = CreateAuthenticatedNetState();
        var capacity = ns._socket.SendBuffer.PhysicalSize;

        ns.Send(new byte[capacity + 1]); // cannot fit: first exhaustion queues the disconnect
        ns.Send(new byte[capacity + 2]); // same tick, before Slice(): must not re-report

        Assert.Contains($"needed {capacity + 1}", ns._disconnectReason);
    }

    [Fact]
    public void CancelAllTrades_CancelsEveryTrade()
    {
        var (from, fromMobile) = CreateClient("from");
        var (to, toMobile) = CreateClient("to");

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
}
