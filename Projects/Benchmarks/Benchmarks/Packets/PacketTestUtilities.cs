using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Server;
using Server.Network;

namespace Benchmarks
{
    public static class PacketTestUtilities
    {
        public static Span<byte> Compile(this Packet p) =>
            p.Compile(false, out var length).AsSpan(0, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> data, ref int pos, Serial serial)
        {
            BinaryPrimitives.WriteUInt32BigEndian(data.Slice(pos, 4), serial.Value);
            pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> data, ref int pos, ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(data.Slice(pos, 2), value);
            pos += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Span<byte> data, ref int pos, byte value) => data[pos++] = value;
    }
}
