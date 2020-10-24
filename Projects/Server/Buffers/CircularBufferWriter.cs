/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CircularBufferWriter.cs                                         *
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
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server;

namespace System.Buffers
{
    public ref struct CircularBufferWriter
    {
        private readonly Span<byte> _first;
        private readonly Span<byte> _second;

        public int Length { get; }
        public int Position { get; private set; }

        public CircularBufferWriter(ArraySegment<byte>[] buffers) : this(buffers[0], buffers[1])
        {
        }

        public CircularBufferWriter(Span<byte> first, Span<byte> second)
        {
            _first = first;
            _second = second;
            Position = 0;
            Length = first.Length + second.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            if (Position < _first.Length)
            {
                _first[Position++] = value;
            }
            else if (Position < Length)
            {
                _second[Position++ - _first.Length] = value;
            }
            else
            {
                throw new OutOfMemoryException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(bool value)
        {
            if (value)
            {
                Write((byte)1);
            }
            else
            {
                Write((byte)0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte value)
        {
            Write((byte)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(short value)
        {
            if (Position < _first.Length)
            {
                if (!BinaryPrimitives.TryWriteInt16BigEndian(_first.Slice(Position), value))
                {
                    // Not enough space. Split the spans
                    Write((byte)(value >> 8));
                    Write((byte)value);
                }
                else
                {
                    Position += 2;
                }
            }
            else if (BinaryPrimitives.TryWriteInt16BigEndian(_second.Slice(Position - _first.Length), value))
            {
                Position += 2;
            }
            else
            {
                throw new OutOfMemoryException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value)
        {
            if (Position < _first.Length)
            {
                if (!BinaryPrimitives.TryWriteUInt16BigEndian(_first.Slice(Position), value))
                {
                    // Not enough space. Split the spans
                    Write((byte)(value >> 8));
                    Write((byte)value);
                }
                else
                {
                    Position += 2;
                }
            }
            else if (BinaryPrimitives.TryWriteUInt16BigEndian(_second.Slice(Position - _first.Length), value))
            {
                Position += 2;
            }
            else
            {
                throw new OutOfMemoryException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            if (Position < _first.Length)
            {
                if (!BinaryPrimitives.TryWriteInt32BigEndian(_first.Slice(Position), value))
                {
                    // Not enough space. Split the spans
                    Write((byte)(value >> 24));
                    Write((byte)(value >> 16));
                    Write((byte)(value >> 8));
                    Write((byte)value);
                }
                else
                {
                    Position += 4;
                }
            }
            else if (BinaryPrimitives.TryWriteInt32BigEndian(_second.Slice(Position - _first.Length), value))
            {
                Position += 4;
            }
            else
            {
                throw new OutOfMemoryException();
            }
        }

        /// <summary>
        /// Writes a 4-byte unsigned integer value to the underlying stream.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value)
        {
            if (Position < _first.Length)
            {
                if (!BinaryPrimitives.TryWriteUInt32BigEndian(_first.Slice(Position), value))
                {
                    // Not enough space. Split the spans
                    Write((byte)(value >> 24));
                    Write((byte)(value >> 16));
                    Write((byte)(value >> 8));
                    Write((byte)value);
                }
                else
                {
                    Position += 4;
                }
            }
            else if (BinaryPrimitives.TryWriteUInt32BigEndian(_second.Slice(Position - _first.Length), value))
            {
                Position += 4;
            }
            else
            {
                throw new OutOfMemoryException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> buffer)
        {
            if (Position + buffer.Length > Length)
            {
                throw new OutOfMemoryException();
            }

            if (Position < _first.Length)
            {
                var sz = Math.Min(buffer.Length, _first.Length - Position);
                buffer.CopyTo(_first.Slice(Position));
                buffer.Slice(sz).CopyTo(_second);
                Position += buffer.Length;
            }
            else if (Position < Length)
            {
                buffer.CopyTo(_second.Slice(Position - _first.Length));
            }
            else
            {
                throw new OutOfMemoryException();
            }
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

            int offset = 0;
            int charCount;

            if (Position < _first.Length)
            {
                bool firstOnly = Position + byteCount <= _first.Length;

                charCount = firstOnly ? src.Length : Math.DivRem(_first.Length - Position, sizeT, out offset);
                var bytesWritten = encoding.GetBytes(src.Slice(0, charCount), _first.Slice(Position));
                Position += bytesWritten;
                byteCount -= bytesWritten;

                // Character split between First and Second
                // This only happens with sizeT = 2 based encodings
                if (offset != 0)
                {
                    Span<byte> chr = stackalloc byte[2];
                    encoding.GetBytes(src.Slice(charCount++, 1), chr);
                    _first[Position] = chr[0];
                    _second[0] = chr[1];
                    Position += 2;
                    byteCount -= 2;
                }
                else if (firstOnly && byteCount > 0)
                {
                    Position += byteCount;
                    _first.Slice(Position, byteCount).Clear();
                    return;
                }
            }
            else
            {
                offset = Position - _first.Length;
                charCount = 0;
            }

            if (charCount < src.Length)
            {
                var bytesWritten = encoding.GetBytes(src.Slice(charCount), _second.Slice(offset));
                Position += bytesWritten;
                byteCount -= bytesWritten;

                if (byteCount > 0)
                {
                    _second.Slice(offset + bytesWritten, byteCount).Clear();
                }
            }
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
            if (Position < _first.Length)
            {
                _first.Slice(Position).Fill(0);
                _second.Fill(0);
            }
            else
            {
                _second.Slice(Position - _first.Length).Fill(0);
            }

            Position = Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear(int amount)
        {
            if (Position + amount > Length)
            {
                throw new OutOfMemoryException();
            }

            if (Position < _first.Length)
            {
                var sz = Math.Min(amount, _first.Length - Position);

                _first.Slice(Position, sz).Clear();
                _second.Slice(0, Length - sz).Clear();
            }
            else
            {
                _second.Slice(Position - _first.Length, amount).Clear();
            }

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
