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
        public Span<byte> First;
        public Span<byte> Second;
        public int Length { get; }

        /// <summary>
        /// Gets or sets the current stream m_Position.
        /// </summary>
        public int Position { get; private set; }

        public CircularBufferWriter(ArraySegment<byte>[] buffers)
        {
            First = buffers[0];
            Second = buffers[1];
            Position = 0;
            Length = First.Length + Second.Length;
        }

        public CircularBufferWriter(Span<byte> first, Span<byte> second)
        {
            First = first;
            Second = second;
            Position = 0;
            Length = first.Length + second.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            if (Position < First.Length)
            {
                First[Position++] = value;
            }
            else if (Position < Length)
            {
                Second[Position++ - First.Length] = value;
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
            if (Position < First.Length)
            {
                if (!BinaryPrimitives.TryWriteInt16BigEndian(First.Slice(Position), value))
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
            else if (BinaryPrimitives.TryWriteInt16BigEndian(Second.Slice(Position - First.Length), value))
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
            if (Position < First.Length)
            {
                if (!BinaryPrimitives.TryWriteUInt16BigEndian(First.Slice(Position), value))
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
            else if (BinaryPrimitives.TryWriteUInt16BigEndian(Second.Slice(Position - First.Length), value))
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
            if (Position < First.Length)
            {
                if (!BinaryPrimitives.TryWriteInt32BigEndian(First.Slice(Position), value))
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
            else if (BinaryPrimitives.TryWriteInt32BigEndian(Second.Slice(Position - First.Length), value))
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
            if (Position < First.Length)
            {
                if (!BinaryPrimitives.TryWriteUInt32BigEndian(First.Slice(Position), value))
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
            else if (BinaryPrimitives.TryWriteUInt32BigEndian(Second.Slice(Position - First.Length), value))
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
            if (Position < First.Length)
            {
                var sz = Math.Min(buffer.Length, First.Length - Position);
                buffer.CopyTo(First.Slice(Position));
                buffer.Slice(sz).CopyTo(Second);
                Position += buffer.Length;
            }
            else if (Position < Length)
            {
                buffer.CopyTo(Second.Slice(Position - First.Length));
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


            int offset;
            int bytesWritten;
            int sz;

            if (Position < First.Length)
            {
                if (Position + byteCount > First.Length)
                {
                    var remaining = First.Length - Position;
                    sz = Math.DivRem(remaining, sizeT, out offset);
                    bytesWritten = encoding.GetBytes(src.Slice(0, sz), First.Slice(Position));
                    Position += bytesWritten;
                    byteCount -= bytesWritten;

                    // Character split between First and Second
                    // This only happens with sizeT = 2 based encodings
                    if (offset != 0)
                    {
                        Span<byte> chr = stackalloc byte[2];
                        encoding.GetBytes(src.Slice(sz++, 1), chr);
                        First[Position] = chr[0];
                        Second[0] = chr[1];
                        Position += 2;
                        byteCount -= 2;
                    }
                }
                else
                {
                    bytesWritten = encoding.GetBytes(src, First.Slice(Position));
                    Position += bytesWritten;
                    byteCount -= bytesWritten;

                    if (byteCount > 0)
                    {
                        Position += byteCount;
                        First.Slice(Position, byteCount).Clear();
                    }

                    return;
                }
            }
            else
            {
                offset = Position - First.Length;
                sz = 0;
            }

            bytesWritten = encoding.GetBytes(src.Slice(sz), Second.Slice(offset));
            Position += bytesWritten;
            byteCount -= bytesWritten;

            if (byteCount > 0)
            {
                Second.Slice(offset + bytesWritten, byteCount).Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLittleUni(string value) => WriteString<char>(value, Utility.UnicodeLE);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLittleUni(string value, int fixedLength) => WriteString<char>(value, Utility.UnicodeLE, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBigUni(string value) => WriteString<char>(value, Utility.Unicode);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBigUni(string value, int fixedLength) => WriteString<char>(value, Utility.Unicode, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUTF8(string value) => WriteString<byte>(value, Utility.UTF8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAscii(string value) => WriteString<byte>(value, Encoding.ASCII);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAscii(string value, int fixedLength) => WriteString<byte>(value, Utility.ASCII, fixedLength);

        /// <summary>
        /// Fills the remaining length of the buffer with 0x00's
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill()
        {
            if (Position < First.Length)
            {
                First.Slice(Position).Fill(0);
                Second.Fill(0);
            }
            else
            {
                Second.Slice(Position - First.Length).Fill(0);
            }

            Position = Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(int amount)
        {
            if (Position < First.Length)
            {
                var sz = Math.Min(amount, First.Length - Position);

                First.Slice(Position, sz).Fill(0);
                Second.Slice(0, Length - sz).Fill(0);
            }
            else
            {
                Second.Slice(Position - First.Length, amount).Fill(0);
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
