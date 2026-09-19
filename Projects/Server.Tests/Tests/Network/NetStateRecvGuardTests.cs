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

    [Fact]
    public void HandleReceive_PacketLongerThanTheRecvBuffer_IsAnError()
    {
        RegisterVariableLength();
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        try
        {
            ns._protocolState = NetState.ProtocolState.GameServer_LoggedIn;

            // A variable-length header claiming more than the buffer can ever hold would otherwise
            // park the connection with nothing to arm a recv into
            var declared = ns._socket.RecvBuffer.PhysicalSize; // one more than the buffer can ever hold
            // Writing directly into the recv buffer while a recv is armed is safe here: the peer
            // sends nothing, so no completion races this write.
            var header = new byte[] { TestPacketId, (byte)(declared >> 8), (byte)declared };
            header.AsSpan().CopyTo(ns._socket.RecvBuffer.GetWriteSpan());
            ns._socket.RecvBuffer.CommitWrite(header.Length);

            ns.HandleReceive();

            Assert.Equal(NetState.ProtocolState.Error, ns._protocolState);
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
            var header = new byte[] { TestPacketId, (byte)(declared >> 8), (byte)declared };
            header.AsSpan().CopyTo(ns._socket.RecvBuffer.GetWriteSpan());
            ns._socket.RecvBuffer.CommitWrite(header.Length);

            ns.HandleReceive();

            Assert.Equal(NetState.ProtocolState.GameServer_LoggedIn, ns._protocolState);
            Assert.True(ns.Running);
            Assert.Equal(header.Length, ns._socket.RecvBuffer.ReadableBytes);
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
            // Writing directly into the recv buffer while a recv is armed is safe here: the peer
            // sends nothing until after HandleReceive returns, so no completion races this write.
            var header = new byte[] { TestPacketId, (byte)(declared >> 8), (byte)declared };
            header.AsSpan().CopyTo(ns._socket.RecvBuffer.GetWriteSpan());
            ns._socket.RecvBuffer.CommitWrite(header.Length);

            ns.HandleReceive();

            Assert.Equal(NetState.ProtocolState.GameServer_LoggedIn, ns._protocolState);
            Assert.True(ns.Running);

            client.Send(new byte[] { 0x01 });
            SliceUntil(() => ns._socket.RecvBuffer.PhysicalSize == NetState.RecvBufferSize);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }
}
