using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Network;
using System.Threading;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

[Collection("Sequential Server Tests")]
public class NetStateSendBufferTests
{
    // Authentication is the 0x91 credentials check on the game server; that is where buffers promote
    private static NetState CreateAuthenticatedNetState(out Socket client)
    {
        var ns = PacketTestUtilities.CreateTestNetState(out client);
        ns._protocolState = NetState.ProtocolState.GameServer_AwaitingGameServerLogin;
        ns.Account = new MockAccount();
        return ns;
    }

    private static byte[] Pattern(int length, int seed)
    {
        var data = new byte[length];
        new System.Random(seed).NextBytes(data);
        return data;
    }

    private static unsafe void RegisterNoOpPing()
    {
        if (IncomingPackets.GetHandler(0x73) == null)
        {
            IncomingPackets.Register(0x73, 2, false, &NoOp);
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
    public void Account_InTheGameServerState_PromotesBothBuffers()
    {
        RegisterNoOpPing();
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        try
        {
            Assert.Equal(IORingBuffer.MinimumSize, ns._socket.SendBuffer.PhysicalSize);
            Assert.Equal(IORingBuffer.MinimumSize, ns._socket.RecvBuffer.PhysicalSize);

            ns._protocolState = NetState.ProtocolState.GameServer_AwaitingGameServerLogin;
            ns.Account = new MockAccount();

            // Send promotes at once; nothing was in flight so no buffer retires
            Assert.Equal(NetState.SendBufferSize, ns._socket.SendBuffer.PhysicalSize);
            Assert.False(ns._sendBufferGrown);

            // Recv promotes at the next completion
            ns._protocolState = NetState.ProtocolState.GameServer_LoggedIn;
            client.Send(new byte[] { 0x73, 0x01 });
            SliceUntil(() => ns._socket.RecvBuffer.PhysicalSize == NetState.RecvBufferSize);
            Assert.True(ns.Running);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Account_OnTheLoginServer_KeepsTheInitialBuffers()
    {
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        try
        {
            ns._protocolState = NetState.ProtocolState.LoginServer_AwaitingLogin;
            ns.Account = new MockAccount();

            Assert.Equal(IORingBuffer.MinimumSize, ns._socket.SendBuffer.PhysicalSize);
            Assert.Equal(IORingBuffer.MinimumSize, ns._socket.RecvBuffer.PhysicalSize);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Send_BeyondTheInitialBuffer_PromotesOnDemand_AndDelivers()
    {
        // A shard's extra pre-auth packet must promote, not strand the connection
        var ns = PacketTestUtilities.CreateTestNetState(out var client);
        try
        {
            var data = Pattern(IORingBuffer.MinimumSize + 512, 7);
            ns.Send(data);

            Assert.True(ns.Running);
            Assert.Equal(string.Empty, ns._disconnectReason);
            Assert.Equal(NetState.SendBufferSize, ns._socket.SendBuffer.PhysicalSize);
            Assert.False(ns._sendBufferGrown); // promotion is not growth: nothing to shrink later
            Assert.Equal(data, ReadAll(client, data.Length));
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    // A random prefix lands under the target; zeros (2 bits each) walk it up a byte at a time
    private static byte[] CompressesToExactly(int compressedLength, int seed)
    {
        var randomLength = compressedLength * 5 / 8;
        var data = new byte[compressedLength * 8];
        Pattern(randomLength, seed).CopyTo(data, 0);

        var scratch = new byte[compressedLength * 2 + 16];
        for (var length = randomLength; length < data.Length; length++)
        {
            var compressed = NetworkCompression.Compress(data.AsSpan(0, length), scratch);
            Assert.InRange(compressed, 1, compressedLength);

            if (compressed == compressedLength)
            {
                return data[..length];
            }
        }

        throw new InvalidOperationException($"No chunk compresses to exactly {compressedLength} bytes");
    }

    private static byte[] ReadAll(Socket client, int length)
    {
        var received = new byte[length];
        var total = 0;
        var deadline = Stopwatch.StartNew();
        while (total < length && deadline.ElapsedMilliseconds < 10000)
        {
            NetState.Slice();

            // Poll takes microseconds, not milliseconds
            if (client.Poll(50_000, SelectMode.SelectRead))
            {
                var read = client.Receive(received, total, length - total, SocketFlags.None);
                Assert.NotEqual(0, read);
                total += read;
            }
        }

        Assert.Equal(length, total);
        return received;
    }

    // Peer receipt does not free the buffer; the completion must land first
    private static void WaitForDrain(NetState ns)
    {
        var deadline = Stopwatch.StartNew();
        while (deadline.ElapsedMilliseconds < 10000)
        {
            NetState.Slice();

            if (ns._socket.SendBuffer.ReadableBytes == 0 && ns._socket.SendBuffer.InFlightBytes == 0)
            {
                break;
            }

            Thread.Sleep(5);
        }

        Assert.Equal(0, ns._socket.SendBuffer.ReadableBytes);
        Assert.Equal(0, ns._socket.SendBuffer.InFlightBytes);
    }

    [Fact]
    public void Send_GrowsInsteadOfDisconnecting_AndStreamStaysIntact()
    {
        var ns = CreateAuthenticatedNetState(out var client);
        var baseSize = ns._socket.SendBuffer.PhysicalSize;

        try
        {
            // No Slice(): nothing drains, so the base buffer overflows
            var chunk = baseSize / 4;
            var expected = new byte[chunk * 6];
            for (var i = 0; i < 6; i++)
            {
                var data = Pattern(chunk, i);
                data.CopyTo(expected, i * chunk);
                ns.Send(data);
            }

            Assert.True(ns.Running);
            Assert.Equal(string.Empty, ns._disconnectReason);
            Assert.True(ns._socket.SendBuffer.PhysicalSize > baseSize);
            Assert.True(ns._sendBufferGrown);

            Assert.Equal(expected, ReadAll(client, expected.Length));
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Send_CompressedGrowth_RetriesAndStreamStaysIntact()
    {
        var ns = CreateAuthenticatedNetState(out var client);
        ns.CompressionEnabled = true;
        var baseSize = ns._socket.SendBuffer.PhysicalSize;

        try
        {
            // No Slice(): compressed output accumulates until the base overflows
            var chunkLength = baseSize / 4;
            var expected = new byte[(chunkLength * 2 + 4) * 6];
            var scratch = new byte[chunkLength * 2 + 4];
            var expectedLength = 0;
            for (var i = 0; i < 6; i++)
            {
                var data = Pattern(chunkLength, i + 100);
                var compressedLength = NetworkCompression.Compress(data, scratch);
                Assert.True(compressedLength > 0);
                scratch.AsSpan(0, compressedLength).CopyTo(expected.AsSpan(expectedLength));
                expectedLength += compressedLength;
                ns.Send(data);
            }

            Assert.True(ns.Running);
            Assert.Equal(string.Empty, ns._disconnectReason);
            Assert.True(ns._socket.SendBuffer.PhysicalSize > baseSize);
            Assert.True(ns._sendBufferGrown);

            Array.Resize(ref expected, expectedLength);
            Assert.Equal(expected, ReadAll(client, expected.Length));
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Send_CompressedGrowth_FullBuffer_GrowsOneTierAndFits()
    {
        var ns = CreateAuthenticatedNetState(out var client);
        ns.CompressionEnabled = true;
        var baseSize = ns._socket.SendBuffer.PhysicalSize;
        var expected = new List<byte>(baseSize * 2);

        void SendAndRecord(byte[] data)
        {
            var scratch = new byte[data.Length * 2 + 4];
            var compressedLength = NetworkCompression.Compress(data, scratch);
            Assert.True(compressedLength > 0);
            expected.AddRange(scratch.AsSpan(0, compressedLength));
            ns.Send(data);
        }

        try
        {
            // Chunks sized so the fill cannot overflow (random bytes never compress; ratio at most 11/8)
            var seed = 600;
            while (ns._socket.SendBuffer.WritableBytes > 4096)
            {
                var writable = ns._socket.SendBuffer.WritableBytes;
                SendAndRecord(Pattern(Math.Min(baseSize / 8, (writable - 2048) * 8 / 11), seed++));
            }

            // Full; no span to compress into
            SendAndRecord(CompressesToExactly(ns._socket.SendBuffer.WritableBytes, seed));
            Assert.Equal(0, ns._socket.SendBuffer.WritableBytes);
            Assert.Equal(baseSize, ns._socket.SendBuffer.PhysicalSize);

            // Raw exceeds the remainder; compressed fits in one tier
            SendAndRecord(new byte[Math.Min(baseSize * 3 / 4, NetworkCompression.DefiniteOverflow)]);

            Assert.True(ns.Running);
            Assert.Equal(string.Empty, ns._disconnectReason);
            Assert.Equal(baseSize * 2, ns._socket.SendBuffer.PhysicalSize);
            Assert.True(ns._sendBufferGrown);

            Assert.Equal(expected.ToArray(), ReadAll(client, expected.Count));
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Send_WithPartialRemainder_GrowsAndCopiesFullPayload()
    {
        var ns = CreateAuthenticatedNetState(out var client);
        var baseSize = ns._socket.SendBuffer.PhysicalSize;

        try
        {
            var first = Pattern(baseSize / 2, 200);
            var second = Pattern(baseSize / 2 + 1, 201);
            var expected = new byte[first.Length + second.Length];
            first.CopyTo(expected, 0);
            second.CopyTo(expected, first.Length);

            // Remainder too small for the second send
            ns.Send(first);
            ns.Send(second);

            Assert.True(ns.Running);
            Assert.Equal(string.Empty, ns._disconnectReason);
            Assert.True(ns._socket.SendBuffer.PhysicalSize > baseSize);
            Assert.True(ns._sendBufferGrown);

            Assert.Equal(expected, ReadAll(client, expected.Length));
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Send_WithSendInFlight_GrowsAndStreamStaysIntact()
    {
        var ns = CreateAuthenticatedNetState(out var client);
        var baseSize = ns._socket.SendBuffer.PhysicalSize;

        try
        {
            // A small peer buffer keeps the first send in flight through the growth
            client.ReceiveBufferSize = 4096;

            var first = Pattern(baseSize / 2, 300);
            var second = Pattern(baseSize / 2 + 1, 301);

            ns.Send(first);
            NetState.Slice(); // posts the first send
            Assert.True(ns._socket.SendBuffer.InFlightBytes > 0);

            ns.Send(second);

            Assert.True(ns.Running);
            Assert.Equal(string.Empty, ns._disconnectReason);
            Assert.True(ns._socket.SendBuffer.PhysicalSize > baseSize);
            Assert.True(ns._sendBufferGrown);

            var expected = new byte[first.Length + second.Length];
            first.CopyTo(expected, 0);
            second.CopyTo(expected, first.Length);

            Assert.Equal(expected, ReadAll(client, expected.Length));
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Shrink_AfterDrainAndHold_ReturnsToBase()
    {
        var ns = CreateAuthenticatedNetState(out var client);
        var baseSize = ns._socket.SendBuffer.PhysicalSize;

        try
        {
            var chunk = baseSize / 4;
            for (var i = 0; i < 6; i++)
            {
                ns.Send(Pattern(chunk, i));
            }

            Assert.True(ns._sendBufferGrown);
            ReadAll(client, chunk * 6);
            WaitForDrain(ns);

            var grewAt = ns._sendBufferGrewAt;
            Assert.False(ns.TryShrinkSendBuffer(grewAt + NetState.SendBufferHoldMs - 1));
            Assert.True(ns.TryShrinkSendBuffer(grewAt + NetState.SendBufferHoldMs));
            Assert.Equal(baseSize, ns._socket.SendBuffer.PhysicalSize);
            Assert.False(ns._sendBufferGrown);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Shrink_ThroughAliveSweep_ReturnsToBase()
    {
        var ns = CreateAuthenticatedNetState(out var client);
        var baseSize = ns._socket.SendBuffer.PhysicalSize;
        var previousTicks = Core._tickCount;

        try
        {
            var chunk = baseSize / 4;
            for (var i = 0; i < 6; i++)
            {
                ns.Send(Pattern(chunk, i + 400));
            }

            Assert.True(ns._sendBufferGrown);
            ReadAll(client, chunk * 6);
            WaitForDrain(ns);

            // The sweep shrinks; nothing here calls TryShrinkSendBuffer
            Core._tickCount = ns._sendBufferGrewAt + NetState.SendBufferHoldMs;
            NetState.CheckAllAlive();

            Assert.Equal(baseSize, ns._socket.SendBuffer.PhysicalSize);
            Assert.False(ns._sendBufferGrown);
        }
        finally
        {
            Core._tickCount = previousTicks;
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Send_PastTheMaximum_FallsBackToExhaustion()
    {
        var ns = CreateAuthenticatedNetState(out var client);

        try
        {
            var chunk = 64 * 1024;
            var writes = NetState.MaxSendBufferSize / chunk + 1;
            for (var i = 0; i < writes; i++)
            {
                ns.Send(Pattern(chunk, i));
            }

            Assert.Contains("Send buffer exhausted", ns._disconnectReason);
            Assert.Equal(NetState.MaxSendBufferSize, ns._socket.SendBuffer.PhysicalSize);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Fact]
    public void Send_AboveMemoryCeiling_DoesNotGrow()
    {
        var previous = NetState._availableMemoryBytes;
        NetState ns = null;
        Socket client = null;

        try
        {
            ns = CreateAuthenticatedNetState(out client);
            var baseSize = ns._socket.SendBuffer.PhysicalSize;
            NetState._availableMemoryBytes = 1; // any working set is above 80% of one byte

            for (var i = 0; i < 6; i++)
            {
                ns.Send(Pattern(baseSize / 4, i));
            }

            Assert.Equal(baseSize, ns._socket.SendBuffer.PhysicalSize);
            Assert.Contains("Send buffer exhausted", ns._disconnectReason);
        }
        finally
        {
            NetState._availableMemoryBytes = previous;
            ns?.Dispose();
            client?.Close();
        }
    }

    [Fact]
    public void Send_OnClosingSocket_DoesNotGrow()
    {
        var ns = CreateAuthenticatedNetState(out var client);
        var baseSize = ns._socket.SendBuffer.PhysicalSize;

        try
        {
            ns.Disconnect("test");
            NetState.Slice(); // handoff

            // CannotSendPackets short-circuits before any growth
            ns.Send(Pattern(baseSize * 2, 500));

            Assert.Equal(baseSize, ns._socket.SendBuffer.PhysicalSize);
            Assert.False(ns._sendBufferGrown);
        }
        finally
        {
            ns.Dispose();
            client.Close();
        }
    }

    [Theory]
    [InlineData(3 * 1024 * 1024, 4 * 1024 * 1024)]   // not a power of two; rounded up
    [InlineData(512 * 1024 * 1024, 256 * 1024 * 1024)] // above the transport ceiling; capped
    [InlineData(1024, 64 * 1024)]                     // below the minimum; raised
    [InlineData(2 * 1024 * 1024, 2 * 1024 * 1024)]   // already valid; untouched
    public void CoercePowerOfTwoSetting_CoercesToATierTheTransportAccepts(int configured, int expected) =>
        Assert.Equal(expected, NetState.CoercePowerOfTwoSetting("network.sendBufferMaxSize", configured, 64 * 1024));

    [Fact]
    public void CoerceSendBufferGrowthBudget_ClampsNegativeAndRaisesBelowOneSlab()
    {
        const int sendBufferSize = 256 * 1024;
        const int maxSendBufferSize = 2 * 1024 * 1024;
        var minimum = RingSocketManager.MinimumSendBufferGrowthBudget(sendBufferSize);

        Assert.Equal(0L, NetState.CoerceSendBufferGrowthBudget(-1, sendBufferSize, maxSendBufferSize));

        // zero means never grow
        Assert.Equal(0L, NetState.CoerceSendBufferGrowthBudget(0, sendBufferSize, maxSendBufferSize));

        Assert.Equal(minimum, NetState.CoerceSendBufferGrowthBudget(1, sendBufferSize, maxSendBufferSize));
        Assert.Equal(minimum * 4, NetState.CoerceSendBufferGrowthBudget(minimum * 4, sendBufferSize, maxSendBufferSize));

        // growth off; budget untouched
        Assert.Equal(1L, NetState.CoerceSendBufferGrowthBudget(1, sendBufferSize, sendBufferSize));
    }

    [Fact]
    public void CoerceMaxBufferSlabs_ClampsToOneSlabPerConnection()
    {
        var maxConnections = NetState.SocketManager.MaxSockets;

        // fewer than one slab is meaningless
        Assert.Equal(1, NetState.CoerceMaxBufferSlabs(0));
        Assert.Equal(1, NetState.CoerceMaxBufferSlabs(-4));

        // more slabs than connections cannot make a slab any smaller
        Assert.Equal(maxConnections, NetState.CoerceMaxBufferSlabs(maxConnections * 2));

        Assert.Equal(128, NetState.CoerceMaxBufferSlabs(128));
    }

    [Fact]
    public void CoerceInitialBufferSlabs_ClampsToTheSlabCount()
    {
        var slabs = NetState.BasePoolSlabCount(128);
        Assert.True(slabs > 1);

        Assert.Equal(1, NetState.CoerceInitialBufferSlabs(0, slabs));
        Assert.Equal(1, NetState.CoerceInitialBufferSlabs(-1, slabs));

        // a pool never holds more slabs than it has
        Assert.Equal(slabs, NetState.CoerceInitialBufferSlabs(slabs + 1, slabs));

        Assert.Equal(4, NetState.CoerceInitialBufferSlabs(4, slabs));
    }

    [Fact]
    public void Ring_RegisteredBufferTable_MatchesTheConfiguredSlabCount()
    {
        var manager = NetState.SocketManager;
        var maxBufferSlabs = NetState.CoerceMaxBufferSlabs(ServerConfiguration.GetSetting("network.maxBufferSlabs", NetState.DefaultMaxBufferSlabs));

        // A table smaller than this throws at manager construction. Connections start on the
        // transport minimum until the game server promotes them, so the formula must count those too.
        Assert.Equal(
            RingSocketManager.RequiredRegisteredBuffers(
                manager.MaxSockets,
                NetState.SendBufferSize,
                manager.MaxSendBufferSize,
                manager.SendBufferGrowthBudget,
                maxBufferSlabs,
                IORingBuffer.MinimumSize,
                IORingBuffer.MinimumSize
            ),
            NetState.Ring.MaxRegisteredBuffers
        );
    }
}
