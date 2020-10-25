using System;
using System.Net.Sockets;
using System.Threading;
using Server.Network;

namespace Server.Tests.Network
{
    public static class PacketTestUtilities
    {
        public static Span<byte> Compile(this Packet p) =>
            p.Compile(false, out var length).AsSpan(0, length);

        public static NetState CreateTestNetState() =>
            new NetState(new Socket(SocketType.Stream, ProtocolType.Tcp), Thread.CurrentThread);
    }
}
