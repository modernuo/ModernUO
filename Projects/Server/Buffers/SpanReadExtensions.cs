/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpanReadExtensions.cs                                           *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    public static class SpanReadExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadInt8(this Span<byte> span, ref int pos) => span[pos++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(this Span<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadInt16BigEndian(span.Slice(pos, 2));
            pos += 2;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(this Span<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(pos, 2));
            pos += 2;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this Span<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadInt32BigEndian(span.Slice(pos, 4));
            pos += 4;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this Span<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadUInt32BigEndian(span.Slice(pos, 4));
            pos += 4;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16LE(this Span<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(pos, 2));
            pos += 2;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16LE(this Span<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(pos, 2));
            pos += 2;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32LE(this Span<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(pos, 4));
            pos += 4;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32LE(this Span<byte> span, ref int pos)
        {
            var v = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(pos, 4));
            pos += 4;
            return v;
        }
    }
}
