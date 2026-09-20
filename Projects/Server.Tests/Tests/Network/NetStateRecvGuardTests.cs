using System;
using System.Buffers;
using System.Diagnostics;
using System.Network;
using System.Threading;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class NetStateRecvGuardTests
{
    private const byte TestPacketId = 0xB1;

    private static unsafe void RegisterVariableLength()
    {
        if (IncomingPackets.GetHandler(TestPacketId) == null)
        {
            IncomingPackets.Register(TestPacketId, 0, false, &NoOp);
        }
    }

    private static void NoOp(NetState state, SpanReader reader)
    {
    }

    // Not 0x73: NetStateSendBufferTests registers its own no-op there first-come-wins, since the
    // handler table is process-global, and this test needs its own handler's side effect to fire.
    private const byte TestPingPacketId = 0x72;

    private static byte _lastPing;

    private static unsafe void RegisterNoOpPing()
    {
        if (IncomingPackets.GetHandler(TestPingPacketId) == null)
        {
            IncomingPackets.Register(TestPingPacketId, 2, false, &RecordPing);
        }
    }

    private static void RecordPing(NetState state, SpanReader reader) => _lastPing = reader.ReadByte();

    private const byte AttachAccountPacketId = 0x71;

    private static unsafe void RegisterAttachAccount()
    {
        if (IncomingPackets.GetHandler(AttachAccountPacketId) == null)
        {
            IncomingPackets.Register(AttachAccountPacketId, 3, false, &AttachAccount);
        }
    }

    // Stands in for the 0x91 handler: the account attaches mid-parse, so the promotion is pending
    // with a receive armed when the next packet in the same completion reaches the guard
    private static void AttachAccount(NetState state, SpanReader reader) => state.Account = new MockAccount();

    private static void SliceUntil(Func<bool> done)
    {
        var deadline = Stopwatch.StartNew();
        while (!done() && deadline.ElapsedMilliseconds < 5000)
        {
            NetState.Slice();
            Thread.Sleep(5);
        }

        Assert.True(done());
    }

    [SkippableFact]
    public void HandleReceive_PacketLongerThanTheRecvBuffer_IsAnError()
    {
        RegisterVariableLength();
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        try
        {
            // A 16-bit length cannot exceed a 64 KiB buffer, so the guard is unreachable there and
            // this only ever hit the `< 3` check on a buffer that size.
            Skip.If(ns._socket.RecvBuffer.PhysicalSize > ushort.MaxValue);

            ns._protocolState = NetState.ProtocolState.GameServer_LoggedIn;

            // A variable-length header claiming more than the buffer can ever hold would otherwise
            // park the connection with nothing to arm a recv into
            var declared = ns._socket.RecvBuffer.PhysicalSize; // one more than the buffer can ever hold
            client.Send(new byte[] { TestPacketId, (byte)(declared >> 8), (byte)declared });
            SliceUntil(() => ns._protocolState == NetState.ProtocolState.Error);

            Assert.Contains("bad state", ns._disconnectReason);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void HandleReceive_PacketThatFits_WaitsForTheRest()
    {
        RegisterVariableLength();
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        try
        {
            ns._protocolState = NetState.ProtocolState.GameServer_LoggedIn;

            var declared = ns._socket.RecvBuffer.PhysicalSize / 2;
            client.Send(new byte[] { TestPacketId, (byte)(declared >> 8), (byte)declared });
            SliceUntil(() => ns._socket.RecvBuffer.ReadableBytes == 3);

            Assert.Equal(NetState.ProtocolState.GameServer_LoggedIn, ns._protocolState);
            Assert.True(ns.Running);
            Assert.Equal(3, ns._socket.RecvBuffer.ReadableBytes);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [SkippableFact]
    public void HandleReceive_OversizePacket_WithAnAccount_RetriesPromotionInsteadOfErroring()
    {
        Skip.If(NetState.InitialRecvBufferSize == 0);
        RegisterVariableLength();
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        try
        {
            // _protocolState is still AwaitingSeed here, so the Account setter's gate does not fire
            ns.Account = new MockAccount();
            Assert.Equal(NetState.InitialRecvBufferSize, ns._socket.RecvBuffer.PhysicalSize);

            ns._protocolState = NetState.ProtocolState.GameServer_LoggedIn;

            var declared = NetState.InitialRecvBufferSize; // as much as the initial buffer can ever hold
            client.Send(new byte[] { TestPacketId, (byte)(declared >> 8), (byte)declared });
            SliceUntil(() => ns._socket.RecvBuffer.ReadableBytes == 3);

            Assert.Equal(NetState.ProtocolState.GameServer_LoggedIn, ns._protocolState);
            Assert.True(ns.Running);

            // The deferred promotion applies at this next completion; the header is then delivered
            // whole to the 0xB1 no-op handler
            client.Send(new byte[declared - 3]);
            SliceUntil(
                () => ns._socket.RecvBuffer.ReadableBytes == 0 &&
                      ns._socket.RecvBuffer.PhysicalSize == NetState.RecvBufferSize
            );

            Assert.True(ns.Running);
            Assert.Equal(NetState.ProtocolState.GameServer_LoggedIn, ns._protocolState);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [SkippableFact]
    public void HandleReceive_AfterABurstFillsTheInitialBuffer_KeepsReceiving()
    {
        Skip.If(NetState.InitialRecvBufferSize == 0);
        RegisterNoOpPing();
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        try
        {
            ns._protocolState = NetState.ProtocolState.GameServer_LoggedIn;

            // One segment of pings fills the 4 KiB buffer (capacity is PhysicalSize - 1, odd, so the
            // last ping straddles two completions); the parser consumes them all
            var capacity = ns._socket.RecvBuffer.PhysicalSize - 1;
            var pings = new byte[capacity + 1];
            for (var i = 0; i < pings.Length; i += 2)
            {
                pings[i] = TestPingPacketId;
                pings[i + 1] = (byte)(i / 2);
            }

            client.Send(pings);
            SliceUntil(() => ns._socket.RecvBuffer.ReadableBytes == 0 && ns._receivedData);
            Assert.True(ns.Running);

            // Without a re-arm this ping never arrives
            client.Send(new byte[] { TestPingPacketId, 0xEE });
            SliceUntil(() => _lastPing == 0xEE);
            Assert.True(ns.Running);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [SkippableFact]
    public void HandleReceive_AccountAttachedMidParse_ThenOversizeHeader_WaitsForThePendingPromotion()
    {
        Skip.If(NetState.InitialRecvBufferSize == 0);
        RegisterAttachAccount();
        RegisterVariableLength();
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        try
        {
            ns._protocolState = NetState.ProtocolState.GameServer_LoggedIn;
            var declared = ns._socket.RecvBuffer.PhysicalSize;

            // One send, one completion on loopback: the account packet promotes (deferred: a receive is
            // armed), then the oversize header reaches the guard with that promotion pending. If the OS
            // ever splits this into two completions instead, the first assertion block still holds — the
            // header then hits the guard after the promotion already applied and simply waits on the
            // 64 KiB buffer, so the test cannot flake, it just exercises the weaker path on that run.
            client.Send(new byte[] { AttachAccountPacketId, 0x00, 0x00, TestPacketId, (byte)(declared >> 8), (byte)declared });
            SliceUntil(() => ns.Account != null);

            Assert.True(ns.Running);
            Assert.Equal(NetState.ProtocolState.GameServer_LoggedIn, ns._protocolState);
            Assert.Equal(3, ns._socket.RecvBuffer.ReadableBytes); // the header waits, not rejected

            // The next completion applies the promotion and the whole packet is delivered
            client.Send(new byte[declared - 3]);
            SliceUntil(
                () => ns._socket.RecvBuffer.ReadableBytes == 0 &&
                      ns._socket.RecvBuffer.PhysicalSize == NetState.RecvBufferSize
            );

            Assert.True(ns.Running);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }
}
