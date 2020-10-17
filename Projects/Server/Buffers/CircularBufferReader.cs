/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: CircularBufferReader.cs                                         *
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
using System.Runtime.InteropServices;
using System.Text;

namespace Server.Network
{
    public ref struct CircularBufferReader
    {
        public Span<byte> First;
        public Span<byte> Second;

        public int Length { get; }
        public int Position { get; private set; }
        public int Remaining => Length - Position;

        public CircularBufferReader(ArraySegment<byte>[] buffers)
        {
            First = buffers[0];
            Second = buffers[1];
            Position = 0;
            Length = First.Length + Second.Length;
        }

        public CircularBufferReader(Span<byte> first, Span<byte> second)
        {
            First = first;
            Second = second;
            Position = 0;
            Length = first.Length + second.Length;
        }

        public void Trace(NetState state)
        {
            // We don't have data, so nothing to trace
            if (First.Length == 0)
            {
                return;
            }

            try
            {
                using var sw = new StreamWriter("Packets.log", true);

                sw.WriteLine("Client: {0}: Unhandled packet 0x{1:X2}", state, First[0]);

                Utility.FormatBuffer(sw, First.ToArray(), new Memory<byte>(Second.ToArray()));

                sw.WriteLine();
                sw.WriteLine();
            }
            catch
            {
                // ignored
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte()
        {
            if (Position < First.Length)
            {
                return First[Position++];
            }

            if (Position < Length)
            {
                return Second[Position++ - First.Length];
            }

            throw new OutOfMemoryException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBoolean() => ReadByte() > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte ReadSByte() => (sbyte)ReadByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadInt16()
        {
            short value;

            if (Position < First.Length)
            {
                if (!BinaryPrimitives.TryReadInt16BigEndian(First.Slice(Position), out value))
                    // Not enough space. Split the spans
                {
                    return (short)((ReadByte() >> 8) | ReadByte());
                }

                Position += 2;
            }
            else if (BinaryPrimitives.TryReadInt16BigEndian(Second.Slice(Position - First.Length), out value))
            {
                Position += 2;
            }
            else
            {
                throw new OutOfMemoryException();
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            ushort value;

            if (Position < First.Length)
            {
                if (!BinaryPrimitives.TryReadUInt16BigEndian(First.Slice(Position), out value))
                    // Not enough space. Split the spans
                {
                    return (ushort)((ReadByte() >> 8) | ReadByte());
                }

                Position += 2;
            }
            else if (BinaryPrimitives.TryReadUInt16BigEndian(Second.Slice(Position - First.Length), out value))
            {
                Position += 2;
            }
            else
            {
                throw new OutOfMemoryException();
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            int value;

            if (Position < First.Length)
            {
                if (!BinaryPrimitives.TryReadInt32BigEndian(First.Slice(Position), out value))
                    // Not enough space. Split the spans
                {
                    return (ReadByte() >> 24) | (ReadByte() >> 16) | (ReadByte() >> 8) | ReadByte();
                }

                Position += 4;
            }
            else if (BinaryPrimitives.TryReadInt32BigEndian(Second.Slice(Position - First.Length), out value))
            {
                Position += 4;
            }
            else
            {
                throw new OutOfMemoryException();
            }

            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            uint value;

            if (Position < First.Length)
            {
                if (!BinaryPrimitives.TryReadUInt32BigEndian(First.Slice(Position), out value))
                    // Not enough space. Split the spans
                {
                    return (uint)((ReadByte() >> 24) | (ReadByte() >> 16) | (ReadByte() >> 8) | ReadByte());
                }

                Position += 4;
            }
            else if (BinaryPrimitives.TryReadUInt32BigEndian(Second.Slice(Position - First.Length), out value))
            {
                Position += 4;
            }
            else
            {
                throw new OutOfMemoryException();
            }

            return value;
        }

        private static bool IsSafeChar(ushort c) => c >= 0x20 && c < 0xFFFE;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string ReadString<T>(Encoding encoding, bool safeString = false, int fixedLength = -1) where T : struct, IEquatable<T>
        {
            int sizeT = Unsafe.SizeOf<T>();

            if (sizeT > 2)
            {
                throw new InvalidConstraintException("ReadString only accepts byte, sbyte, char, short, and ushort as a constraint");
            }

            bool firstOnly = Position < First.Length;
            bool isFixedLength = fixedLength > -1;

            var remaining = firstOnly ? Remaining : Position - First.Length;
            int size;

            // Not fixed length
            if (isFixedLength)
            {
                size = fixedLength * sizeT;
                if (fixedLength > Remaining)
                {
                    throw new OutOfMemoryException();
                }
            }
            else
            {
                size = remaining - (remaining & (sizeT - 1));
            }

            Span<byte> span;
            int index;

            if (firstOnly)
            {
                // Find terminator
                index = MemoryMarshal.Cast<byte, T>(First.Slice(Position, size))
                    .IndexOf(default(T)) * sizeT;

                // Split over both spans
                if (index < 0)
                {
                    index = MemoryMarshal.Cast<byte, T>(Second.Slice(0, size - First.Length))
                        .IndexOf(default(T)) * sizeT;

                    remaining = First.Length - Position;

                    int length = Math.Min(index < 0 ? size : remaining + index, size);

                    Span<byte> bytes = stackalloc byte[length];
                    First.Slice(Position).CopyTo(bytes);
                    Second.Slice(0, length - remaining).CopyTo(bytes.Slice(Position));

                    Position += isFixedLength ? size : length;
                    return GetString(bytes, encoding, safeString);
                }

                span = First.Slice(Position, index);
            }
            else
            {
                span = Second.Slice(remaining, size);
                index = MemoryMarshal.Cast<byte, T>(span).IndexOf(default(T)) * sizeT;

                if (index >= 0)
                {
                    span = span.Slice(0, index);
                }
            }

            Position += isFixedLength ? size : index;
            return GetString(span, encoding, safeString);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetString(Span<byte> span, Encoding encoding, bool safeString = false)
        {
            string s = encoding.GetString(span);

            if (!safeString)
            {
                return s;
            }

            ReadOnlySpan<char> chars = s.AsSpan();

            StringBuilder stringBuilder = null;

            for (int i = 0, last = 0; i < chars.Length; i++)
            {
                if (!IsSafeChar(chars[i]) || stringBuilder != null && i == chars.Length - 1)
                {
                    (stringBuilder ??= new StringBuilder()).Append(chars.Slice(last, i - last));
                    last = i + 1; // Skip the unsafe char
                }
            }

            return stringBuilder?.ToString() ?? s;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUniSafe(int fixedLength) => ReadString<char>(Utility.UnicodeLE, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUniSafe() => ReadString<char>(Utility.UnicodeLE, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUni(int fixedLength) => ReadString<char>(Utility.UnicodeLE, false, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadLittleUni() => ReadString<char>(Utility.UnicodeLE);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUniSafe(int fixedLength) => ReadString<char>(Utility.Unicode, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUniSafe() => ReadString<char>(Utility.Unicode, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUni(int fixedLength) => ReadString<char>(Utility.Unicode, false, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadBigUni() => ReadString<char>(Utility.Unicode);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8Safe(int fixedLength) => ReadString<byte>(Utility.UTF8, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8Safe() => ReadString<byte>(Utility.UTF8, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUTF8() => ReadString<byte>(Utility.UTF8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAsciiSafe(int fixedLength) => ReadString<byte>(Encoding.ASCII, true, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAsciiSafe() => ReadString<byte>(Encoding.ASCII, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAscii(int fixedLength) => ReadString<byte>(Encoding.ASCII, false, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadAscii() => ReadString<byte>(Encoding.ASCII);

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
