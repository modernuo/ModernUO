using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Server;

namespace Benchmarks
{
    public ref struct SpanWriter
    {
        public Span<byte> Span;
        public int Pos;

        public SpanWriter(Span<byte> span)
        {
            Span = span;
            Pos = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(Serial serial)
        {
            BinaryPrimitives.WriteUInt32BigEndian(Span.Slice(Pos), serial);
            Pos += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(Span.Slice(Pos), value);
            Pos += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value) => Span[Pos++] = value;
    }
}
