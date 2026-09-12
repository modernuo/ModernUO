using System;
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
    private static NetState CreateAuthenticatedNetState(out Socket client)
    {
        var ns = PacketTestUtilities.CreateTestNetState(out client);
        ns.Account = new MockAccount();
        return ns;
    }

    private static byte[] Pattern(int length, int seed)
    {
        var data = new byte[length];
        new System.Random(seed).NextBytes(data);
        return data;
    }

    // A chunk's compressed length is only knowable by compressing it. Random bytes inflate, so a
    // short random prefix lands under the target; a zero costs two bits, so appending zeros walks the
    // compressed length up one byte at a time and cannot step over the target.
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

    // The peer having read everything is not the same as the send buffer being free: the completion
    // that releases the bytes still has to land on the loop.
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
            // No Slice() between sends: nothing drains, so the base buffer overflows on its own
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
            // No Slice() between sends: compressed output accumulates until the base buffer overflows.
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
            // No Slice() between sends: nothing drains, so the fill lands in the base buffer as written.
            // Random bytes never compress smaller than themselves, and every chunk is sized off what is
            // left (worst Huffman ratio 11/8, plus headroom), so the fill cannot overflow on its own.
            var seed = 600;
            while (ns._socket.SendBuffer.WritableBytes > 4096)
            {
                var writable = ns._socket.SendBuffer.WritableBytes;
                SendAndRecord(Pattern(Math.Min(baseSize / 8, (writable - 2048) * 8 / 11), seed++));
            }

            // Completely full, which is the branch under test: Send() has no span to compress into.
            SendAndRecord(CompressesToExactly(ns._socket.SendBuffer.WritableBytes, seed));
            Assert.Equal(0, ns._socket.SendBuffer.WritableBytes);
            Assert.Equal(baseSize, ns._socket.SendBuffer.PhysicalSize);

            // Raw, this is larger than everything the full buffer has left; compressed it is a quarter
            // of its size, so one tier is all it needs. Growing against the raw length instead demands
            // the whole packet's worth of writable space, which refuses at the maximum on a shard whose
            // maximum is near its base size.
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

            // The first send leaves a non-empty remainder that is too small for the second send.
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
            // Shrinking the peer's receive buffer and never reading stalls the transfer: the posted
            // send stays outstanding, so the second write has to grow around bytes still in flight
            // rather than around bytes merely queued.
            client.ReceiveBufferSize = 4096;

            var first = Pattern(baseSize / 2, 300);
            var second = Pattern(baseSize / 2 + 1, 301);

            ns.Send(first);
            NetState.Slice(); // posts the first send; loopback stalls once the peer's buffer fills
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

            // The sweep is the only thing that shrinks an idle connection in production; nothing here
            // calls TryShrinkSendBuffer itself.
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
            NetState.Slice(); // hands the disconnect to the socket; it is draining from here

            // CannotSendPackets short-circuits before any growth; keeping the buffer from draining is
            // the last thing a closing connection needs.
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

        // Zero is a deliberate "never grow", not a too-small budget
        Assert.Equal(0L, NetState.CoerceSendBufferGrowthBudget(0, sendBufferSize, maxSendBufferSize));

        Assert.Equal(minimum, NetState.CoerceSendBufferGrowthBudget(1, sendBufferSize, maxSendBufferSize));
        Assert.Equal(minimum * 4, NetState.CoerceSendBufferGrowthBudget(minimum * 4, sendBufferSize, maxSendBufferSize));

        // Growth is off when the maximum is the base size, so the budget is taken as configured
        Assert.Equal(1L, NetState.CoerceSendBufferGrowthBudget(1, sendBufferSize, sendBufferSize));
    }
}
