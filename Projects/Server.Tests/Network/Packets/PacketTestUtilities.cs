using System;
using System.Buffers.Binary;
using System.Reflection;
using System.Runtime.CompilerServices;
using Server.Network;

namespace Server.Tests.Network.Packets
{
    public static class PacketTestUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<byte> Compile(this Packet p) =>
            p.Compile(false, out var length).AsSpan(0, length);
    }
}
