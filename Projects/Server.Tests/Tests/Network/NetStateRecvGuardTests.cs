using System;
using System.Buffers;
using System.Network;
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
}
