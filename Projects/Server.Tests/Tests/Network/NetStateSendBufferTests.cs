using System;
using System.Diagnostics;
using System.Net.Sockets;
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

    private static byte[] ReadAll(Socket client, int length)
    {
        var received = new byte[length];
        var total = 0;
        var deadline = Stopwatch.StartNew();
        while (total < length && deadline.ElapsedMilliseconds < 10000)
        {
            NetState.Slice();
            if (client.Poll(1000, SelectMode.SelectRead))
            {
                var read = client.Receive(received, total, length - total, SocketFlags.None);
                Assert.NotEqual(0, read);
                total += read;
            }
        }

        Assert.Equal(length, total);
        return received;
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

            // Let the last send completion land before judging the drain
            for (var i = 0; i < 20; i++)
            {
                NetState.Slice();
                System.Threading.Thread.Sleep(5);
            }

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
        var previous = NetState.UnderMemoryCeiling;
        NetState.UnderMemoryCeiling = static () => false;
        var ns = CreateAuthenticatedNetState(out var client);
        var baseSize = ns._socket.SendBuffer.PhysicalSize;

        try
        {
            for (var i = 0; i < 6; i++)
            {
                ns.Send(Pattern(baseSize / 4, i));
            }

            Assert.Equal(baseSize, ns._socket.SendBuffer.PhysicalSize);
            Assert.Contains("Send buffer exhausted", ns._disconnectReason);
        }
        finally
        {
            NetState.UnderMemoryCeiling = previous;
            ns.Dispose();
            client.Close();
        }
    }
}
