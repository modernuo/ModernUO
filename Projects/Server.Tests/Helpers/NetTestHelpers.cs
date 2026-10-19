using System;
using System.Buffers;
using System.Diagnostics;
using System.Net.Sockets;
using System.Network;
using System.Runtime.CompilerServices;
using System.Threading;
using Server.Network;
using Xunit;

namespace Server.Tests.Network;

/// <summary>
/// Shared plumbing for tests that drive a real <see cref="NetState"/> over loopback.
/// </summary>
public static class NetTestHelpers
{
    public const byte PingPacketId = 0x73;

    /// <summary>The payload byte of the last ping delivered to the handler from <see cref="RegisterPing"/>.</summary>
    public static byte LastPing { get; private set; }

    /// <summary>
    /// Restores the handler that was in a packet slot before a test replaced it. The handler table is
    /// process-global, so a handler left behind would silently answer for that id in every later test.
    /// </summary>
    public readonly struct HandlerRegistration : IDisposable
    {
        private readonly int _packetId;
        private readonly PacketHandler _previous;

        internal HandlerRegistration(int packetId, PacketHandler previous)
        {
            _packetId = packetId;
            _previous = previous;
        }

        public void Dispose() => IncomingPackets.Handlers[_packetId] = _previous;
    }

    public static unsafe HandlerRegistration RegisterHandler(
        int packetId, int length, delegate*<NetState, SpanReader, void> onReceive
    )
    {
        var previous = IncomingPackets.GetHandler(packetId);
        IncomingPackets.Register(packetId, length, false, onReceive);
        return new HandlerRegistration(packetId, previous);
    }

    /// <summary>Registers a two-byte ping whose payload byte is recorded in <see cref="LastPing"/>.</summary>
    public static unsafe HandlerRegistration RegisterPing()
    {
        LastPing = 0;
        return RegisterHandler(PingPacketId, 2, &RecordPing);
    }

    private static void RecordPing(NetState state, SpanReader reader) => LastPing = reader.ReadByte();

    public static byte[] Pattern(int length, int seed)
    {
        var data = new byte[length];
        new System.Random(seed).NextBytes(data);
        return data;
    }

    /// <summary>
    /// Pre-auth buffers are off only where the platform floor leaves no room below the base size
    /// (the Windows Server 2012/2016 mapping path, or a 64 KiB-page kernel). Anywhere else a zero
    /// initial size is a configuration regression and fails rather than skips.
    /// </summary>
    public static void RequirePreAuthBuffers()
    {
        var floor = IORingBuffer.MinimumSize;
        Skip.If(
            floor >= NetState.RecvBufferSize || floor >= NetState.SendBufferSize,
            $"Pre-auth buffers are off on this platform: the {floor}-byte mapping floor leaves no room below the base buffers (recv {NetState.RecvBufferSize}, send {NetState.SendBufferSize})"
        );

        Assert.True(
            NetState.InitialRecvBufferSize > 0 && NetState.InitialSendBufferSize > 0,
            $"Pre-auth buffers are off although the {floor}-byte platform floor allows them (initial recv {NetState.InitialRecvBufferSize}, send {NetState.InitialSendBufferSize})"
        );
    }

    /// <summary>Runs the network slice until <paramref name="done"/> holds, failing with the condition that timed out.</summary>
    public static void SliceUntil(
        Func<bool> done, int timeoutMs = 5000, [CallerArgumentExpression(nameof(done))] string condition = null
    )
    {
        var deadline = Stopwatch.StartNew();
        while (!done())
        {
            if (deadline.ElapsedMilliseconds >= timeoutMs)
            {
                Assert.Fail($"Timed out after {timeoutMs} ms waiting for {condition}");
            }

            NetState.Slice();
            Thread.Sleep(5);
        }
    }

    /// <summary>Reads exactly <paramref name="length"/> bytes from the peer, slicing the server while it waits.</summary>
    public static byte[] ReadAll(Socket client, int length, int timeoutMs = 10000)
    {
        var received = new byte[length];
        var total = 0;
        var deadline = Stopwatch.StartNew();
        while (total < length)
        {
            if (deadline.ElapsedMilliseconds >= timeoutMs)
            {
                Assert.Fail($"Timed out after {timeoutMs} ms with {total} of {length} bytes received by the peer");
            }

            NetState.Slice();

            // Poll takes microseconds, not milliseconds
            if (client.Poll(50_000, SelectMode.SelectRead))
            {
                var read = client.Receive(received, total, length - total, SocketFlags.None);
                Assert.True(read != 0, $"Peer saw EOF after {total} of {length} bytes");
                total += read;
            }
        }

        return received;
    }

    /// <summary>Waits for the server's FIN to reach the peer, failing if any data arrives first.</summary>
    public static void WaitForEof(Socket client, int timeoutMs = 10000)
    {
        var scratch = new byte[64];
        var deadline = Stopwatch.StartNew();
        while (true)
        {
            if (deadline.ElapsedMilliseconds >= timeoutMs)
            {
                Assert.Fail($"Timed out after {timeoutMs} ms waiting for the peer to see EOF");
            }

            NetState.Slice();

            if (client.Poll(50_000, SelectMode.SelectRead))
            {
                var read = client.Receive(scratch);
                Assert.True(read == 0, $"Peer received {read} unexpected bytes while waiting for EOF");
                return;
            }
        }
    }
}
