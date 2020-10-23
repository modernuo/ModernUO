/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SpanWriter.cs                                                   *
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
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Server.Buffers
{
    public ref struct SpanWriter
    {
        private readonly Span<byte> _buffer;

        public int Length => _buffer.Length;

        public ReadOnlySpan<byte> Span => _buffer.Slice(0, Position);

        public int Position { get; private set; }

        public SpanWriter(Span<byte> span)
        {
            _buffer = span;
            Position = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(bool value)
        {
            _buffer[Position++] = *(byte*) & value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            _buffer[Position++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte value)
        {
            _buffer[Position++] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(short value)
        {
            if (!BinaryPrimitives.TryWriteInt16BigEndian(_buffer.Slice(Position), value))
            {
                throw new OutOfMemoryException();
            }

            Position += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value)
        {
            if (!BinaryPrimitives.TryWriteUInt16BigEndian(_buffer.Slice(Position), value))
            {
                throw new OutOfMemoryException();
            }

            Position += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            if (!BinaryPrimitives.TryWriteInt32BigEndian(_buffer.Slice(Position), value))
            {
                throw new OutOfMemoryException();
            }

            Position += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value)
        {
            if (!BinaryPrimitives.TryWriteUInt32BigEndian(_buffer.Slice(Position), value))
            {
                throw new OutOfMemoryException();
            }

            Position += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value)
        {
            if (!BinaryPrimitives.TryWriteInt64BigEndian(_buffer.Slice(Position), value))
            {
                throw new OutOfMemoryException();
            }

            Position += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong value)
        {
            if (!BinaryPrimitives.TryWriteUInt64BigEndian(_buffer.Slice(Position), value))
            {
                throw new OutOfMemoryException();
            }

            Position += 8;
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            var size = buffer.Length;
            if (Position + size > Length)
            {
                throw new OutOfMemoryException();
            }

            buffer.Slice(0, size).CopyTo(_buffer.Slice(Position));
            Position += size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString<T>(string value, Encoding encoding, int fixedLength = -1) where T : struct, IEquatable<T>
        {
            int sizeT = Unsafe.SizeOf<T>();

            if (sizeT > 2)
            {
                throw new InvalidConstraintException("WriteString only accepts byte, sbyte, char, short, and ushort as a constraint");
            }

            value ??= string.Empty;

            var charLength = Math.Min(fixedLength > -1 ? fixedLength : value.Length, value.Length);
            var src = value.AsSpan(0, charLength);

            var byteCount = fixedLength > -1 ? fixedLength * sizeT : encoding.GetByteCount(value);

            if (Position + byteCount > Length)
            {
                throw new OutOfMemoryException();
            }

            Position += encoding.GetBytes(src, _buffer.Slice(Position));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLittleUni(string value) => WriteString<char>(value, Utility.UnicodeLE);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLittleUniNull(string value)
        {
            WriteString<char>(value, Utility.UnicodeLE);
            Write((ushort)0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLittleUni(string value, int fixedLength) => WriteString<char>(value, Utility.UnicodeLE, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBigUni(string value) => WriteString<char>(value, Utility.Unicode);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBigUniNull(string value)
        {
            WriteString<char>(value, Utility.Unicode);
            Write((ushort)0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBigUni(string value, int fixedLength) => WriteString<char>(value, Utility.Unicode, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUTF8(string value) => WriteString<byte>(value, Utility.UTF8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUTF8Null(string value)
        {
            WriteString<byte>(value, Utility.UTF8);
            Write((byte)0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAscii(string value) => WriteString<byte>(value, Encoding.ASCII);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAsciiNull(string value)
        {
            WriteString<byte>(value, Encoding.ASCII);
            Write((byte)0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAscii(string value, int fixedLength) => WriteString<byte>(value, Encoding.ASCII, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            _buffer.Slice(Position).Clear();
            Position = Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(int amount)
        {
            if (Position + amount > Length)
            {
                throw new OutOfMemoryException();
            }

            _buffer.Slice(Position, amount).Clear();
            Position += amount;
        }

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
