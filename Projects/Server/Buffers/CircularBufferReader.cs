/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: PacketReader.cs                                                 *
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
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Server.Network
{
    public ref struct CircularBufferReader
    {
        private readonly ReadOnlySpan<byte> _first;
        private readonly ReadOnlySpan<byte> _second;

        public int Length { get; }
        public int Position { get; private set; }
        public int Remaining => Length - Position;

        public CircularBufferReader(ref CircularBuffer<byte> buffer) : this(buffer.GetSpan(0), buffer.GetSpan(1))
        {
        }

        public CircularBufferReader(CircularBuffer<byte> buffer) : this(buffer.GetSpan(0), buffer.GetSpan(1))
        {
        }

        public CircularBufferReader(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
        {
            _first = first;
            _second = second;
            Position = 0;
            Length = first.Length + second.Length;
        }

        public void Trace(NetState state)
        {
            // We don't have data, so nothing to trace
            if (_first.Length == 0)
            {
                return;
            }

            try
            {
                using var sw = new StreamWriter("Packets.log", true);

                sw.WriteLine("Client: {0}: Unhandled packet 0x{1:X2}", state, _first[0]);

                Utility.FormatBuffer(sw, _first.ToArray(), new Memory<byte>(_second.ToArray()));

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
            if (Position < _first.Length)
            {
                return _first[Position++];
            }

            if (Position < Length)
            {
                return _second[Position++ - _first.Length];
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

            if (Position < _first.Length)
            {
                if (!BinaryPrimitives.TryReadInt16BigEndian(_first.Slice(Position), out value))
                {
                    // Not enough space. Split the spans
                    return (short)((ReadByte() >> 8) | ReadByte());
                }
            }
            else if (!BinaryPrimitives.TryReadInt16BigEndian(_second.Slice(Position - _first.Length), out value))
            {
                throw new OutOfMemoryException();
            }

            Position += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadUInt16()
        {
            ushort value;

            if (Position < _first.Length)
            {
                if (!BinaryPrimitives.TryReadUInt16BigEndian(_first.Slice(Position), out value))
                {
                    // Not enough space. Split the spans
                    return (ushort)((ReadByte() >> 8) | ReadByte());
                }
            }
            else if (!BinaryPrimitives.TryReadUInt16BigEndian(_second.Slice(Position - _first.Length), out value))
            {
                throw new OutOfMemoryException();
            }

            Position += 2;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt32()
        {
            int value;

            if (Position < _first.Length)
            {
                if (!BinaryPrimitives.TryReadInt32BigEndian(_first.Slice(Position), out value))
                {
                    // Not enough space. Split the spans
                    return (ReadByte() >> 24) | (ReadByte() >> 16) | (ReadByte() >> 8) | ReadByte();
                }
            }
            else if (!BinaryPrimitives.TryReadInt32BigEndian(_second.Slice(Position - _first.Length), out value))
            {
                throw new OutOfMemoryException();
            }

            Position += 4;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ReadUInt32()
        {
            uint value;

            if (Position < _first.Length)
            {
                if (!BinaryPrimitives.TryReadUInt32BigEndian(_first.Slice(Position), out value))
                {
                    // Not enough space. Split the spans
                    return (uint)((ReadByte() >> 24) | (ReadByte() >> 16) | (ReadByte() >> 8) | ReadByte());
                }
            }
            else if (!BinaryPrimitives.TryReadUInt32BigEndian(_second.Slice(Position - _first.Length), out value))
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

            ReadOnlySpan<byte> span;
            int index;

            if (Position < _first.Length)
            {
                var firstLength = Math.Min(_first.Length - Position, size);

                // Find terminator
                index = Utility.IndexOfTerminator(_first.Slice(Position, firstLength), sizeT);

                if (index < 0)
                {
                    remaining = size - firstLength;
                    // We don't have a terminator, but a fixed size to the end of the first span, so stop there
                    if (remaining <= 0)
                    {
                        index = firstLength;
                    }
                    else
                    {
                        index = Utility.IndexOfTerminator(_second.Slice(0, remaining), sizeT);

                        int secondLength = index < 0 ? remaining : index;
                        int length = firstLength + secondLength;

                        // Assume no strings should be too long for the stack
                        Span<byte> bytes = stackalloc byte[length];
                        _first.Slice(Position).CopyTo(bytes);
                        _second.Slice(0, secondLength).CopyTo(bytes.Slice(firstLength));

                        Position += length + (index >= 0 ? sizeT : 0);
                        return Utility.GetString(bytes, encoding, safeString);
                    }
                }

                span = _first.Slice(Position, index);
            }
            else
            {
                size = Math.Min(remaining, size);
                span = _second.Slice( Position - _first.Length, size);
                index = Utility.IndexOfTerminator(span, sizeT);

                if (index >= 0)
                {
                    span = span.Slice(0, index);
                }
                else
                {
                    index = size;
                }
            }

            Position += isFixedLength ? size : index + sizeT;
            return Utility.GetString(span, encoding, safeString);
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
