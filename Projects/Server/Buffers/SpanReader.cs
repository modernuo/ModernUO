/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpanReader.cs                                                   *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Server.Buffers
{
    ref struct SpanReader
    {
        private readonly ReadOnlySpan<byte> _buffer;

        public int Length { get; }
        public int Position { get; private set; }
        public int Remaining => Length - Position;

        public SpanReader(ReadOnlySpan<byte> span)
        {
            _buffer = span;
            Position = 0;
            Length = span.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (Position >= Length)
            {
                throw new OutOfMemoryException();
            }

            return _buffer[Position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean() => ReadByte() > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte() => (sbyte)ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            if (!BinaryPrimitives.TryReadInt16BigEndian(_buffer.Slice(Position), out var value))
            {
                throw new OutOfMemoryException();
            }

            Position += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            if (!BinaryPrimitives.TryReadUInt16BigEndian(_buffer.Slice(Position), out var value))
            {
                throw new OutOfMemoryException();
            }

            Position += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            if (!BinaryPrimitives.TryReadInt32BigEndian(_buffer.Slice(Position), out var value))
            {
                throw new OutOfMemoryException();
            }

            Position += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            if (!BinaryPrimitives.TryReadUInt32BigEndian(_buffer.Slice(Position), out var value))
            {
                throw new OutOfMemoryException();
            }

            Position += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString(Encoding encoding, bool safeString = false, int fixedLength = -1)
        {
            int sizeT = Utility.GetByteLengthForEncoding(encoding);

            bool isFixedLength = fixedLength > -1;

            var remaining = Remaining;
            int size;

            if (isFixedLength)
            {
                size = fixedLength * sizeT;
                if (size > Remaining)
                {
                    throw new OutOfMemoryException();
                }
            }
            else
            {
                size = remaining - (remaining & (sizeT - 1));
            }

            int index = Utility.IndexOfTerminator(_buffer.Slice(Position, size), sizeT);

            Position += isFixedLength || index < 0 ? size : index + sizeT;
            return Utility.GetString(_buffer.Slice(Position, index < 0 ? size : index), encoding, safeString);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUniSafe(int fixedLength) => ReadString(Utility.UnicodeLE, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUniSafe() => ReadString(Utility.UnicodeLE, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUni(int fixedLength) => ReadString(Utility.UnicodeLE, false, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUni() => ReadString(Utility.UnicodeLE);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUniSafe(int fixedLength) => ReadString(Utility.Unicode, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUniSafe() => ReadString(Utility.Unicode, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUni(int fixedLength) => ReadString(Utility.Unicode, false, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUni() => ReadString(Utility.Unicode);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8Safe(int fixedLength) => ReadString(Utility.UTF8, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8Safe() => ReadString(Utility.UTF8, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8() => ReadString(Utility.UTF8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAsciiSafe(int fixedLength) => ReadString(Encoding.ASCII, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAsciiSafe() => ReadString(Encoding.ASCII, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAscii(int fixedLength) => ReadString(Encoding.ASCII, false, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAscii() => ReadString(Encoding.ASCII);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Seek(int offset, SeekOrigin origin) =>
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.End   => Length - offset,
                _                => Position + offset // Current
            };
    }
}
