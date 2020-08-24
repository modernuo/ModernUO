using System;
using System.Runtime.CompilerServices;
using Server.Network;

namespace Server.Tests.Network.Packets
{
    public static class PacketTestUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> Compile(this Packet p) =>
            p.Compile(false, out int length).AsSpan(0, length);
    }
}
