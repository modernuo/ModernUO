using System;
using System.Runtime.CompilerServices;
using Server.Network;

namespace Server.Tests.Network
{
    public static class PacketTestUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> Compile(this Packet p) =>
            p.Compile(false, out var length).AsSpan(0, length);
    }
}
