/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
using Server.Text;

namespace Server.Network;

public ref struct CircularBufferReader
{
    private readonly ReadOnlySpan<byte> _first;
    private readonly ReadOnlySpan<byte> _second;

    public int Length { get; }
    public int Position { get; private set; }
    public int Remaining => Length - Position;

    // Only used for debugging!
    public ReadOnlySpan<byte> First => _first;
    public ReadOnlySpan<byte> Second => _second;

    public CircularBufferReader(ref CircularBuffer<byte> buffer) : this(buffer.GetSpan(0), buffer.GetSpan(1))
    {
    }

    public CircularBufferReader(ArraySegment<byte>[] buffer) : this(buffer[0], buffer[1])
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
            using var sw = new StreamWriter("unhandled-packets.log", true);
            sw.WriteLine("Client: {0}: Unhandled packet 0x{1:X2}", state, _first[0]);
            sw.FormatBuffer(_first, _second, Length);
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
            if (!BinaryPrimitives.TryReadInt16BigEndian(_first[Position..], out value))
            {
                // Not enough space. Split the spans
                return (short)((ReadByte() >> 8) | ReadByte());
            }
        }
        else if (!BinaryPrimitives.TryReadInt16BigEndian(_second[(Position - _first.Length)..], out value))
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
            if (!BinaryPrimitives.TryReadUInt16BigEndian(_first[Position..], out value))
            {
                // Not enough space. Split the spans
                return (ushort)((ReadByte() >> 8) | ReadByte());
            }
        }
        else if (!BinaryPrimitives.TryReadUInt16BigEndian(_second[(Position - _first.Length)..], out value))
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
            if (!BinaryPrimitives.TryReadInt32BigEndian(_first[Position..], out value))
            {
                // Not enough space. Split the spans
                return (ReadByte() >> 24) | (ReadByte() >> 16) | (ReadByte() >> 8) | ReadByte();
            }
        }
        else if (!BinaryPrimitives.TryReadInt32BigEndian(_second[(Position - _first.Length)..], out value))
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
            if (!BinaryPrimitives.TryReadUInt32BigEndian(_first[Position..], out value))
            {
                // Not enough space. Split the spans
                return (uint)((ReadByte() >> 24) | (ReadByte() >> 16) | (ReadByte() >> 8) | ReadByte());
            }
        }
        else if (!BinaryPrimitives.TryReadUInt32BigEndian(_second[(Position - _first.Length)..], out value))
        {
            throw new OutOfMemoryException();
        }

        Position += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64()
    {
        long value;

        if (Position < _first.Length)
        {
            if (!BinaryPrimitives.TryReadInt64BigEndian(_first[Position..], out value))
            {
                // Not enough space. Split the spans
                return ((long)ReadByte() >> 56) |
                       ((long)ReadByte() >> 48) |
                       ((long)ReadByte() >> 40) |
                       ((long)ReadByte() >> 32) |
                       ((long)ReadByte() >> 24) |
                       ((long)ReadByte() >> 16) |
                       ((long)ReadByte() >> 8) |
                       ReadByte();
            }
        }
        else if (!BinaryPrimitives.TryReadInt64BigEndian(_second[(Position - _first.Length)..], out value))
        {
            throw new OutOfMemoryException();
        }

        Position += 8;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64()
    {
        ulong value;

        if (Position < _first.Length)
        {
            if (!BinaryPrimitives.TryReadUInt64BigEndian(_first[Position..], out value))
            {
                // Not enough space. Split the spans
                return ((ulong)ReadByte() >> 56) |
                       ((ulong)ReadByte() >> 48) |
                       ((ulong)ReadByte() >> 40) |
                       ((ulong)ReadByte() >> 32) |
                       ((ulong)ReadByte() >> 24) |
                       ((ulong)ReadByte() >> 16) |
                       ((ulong)ReadByte() >> 8) |
                       ReadByte();
            }
        }
        else if (!BinaryPrimitives.TryReadUInt64BigEndian(_second[(Position - _first.Length)..], out value))
        {
            throw new OutOfMemoryException();
        }

        Position += 8;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(Encoding encoding, bool safeString = false, int fixedLength = -1)
    {
        int byteLength = encoding.GetByteLengthForEncoding();

        bool isFixedLength = fixedLength > -1;

        var remaining = Remaining;
        int size;

        if (isFixedLength)
        {
            size = fixedLength * byteLength;
            if (size > Remaining)
            {
                throw new OutOfMemoryException();
            }
        }
        else
        {
            size = remaining - (remaining & (byteLength - 1));
        }

        ReadOnlySpan<byte> span;
        int index;

        if (Position < _first.Length)
        {
            var firstLength = Math.Min(_first.Length - Position, size);

            // Find terminator
            index = _first.Slice(Position, firstLength).IndexOfTerminator(byteLength);

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
                    index = _second[..remaining].IndexOfTerminator(byteLength);

                    int secondLength = index < 0 ? remaining : index;
                    int length = firstLength + secondLength;

                    // Assume no strings should be too long for the stack
                    Span<byte> bytes = stackalloc byte[length];
                    _first[Position..].CopyTo(bytes);
                    _second[..secondLength].CopyTo(bytes[firstLength..]);

                    Position += length + (index >= 0 ? byteLength : 0);
                    return TextEncoding.GetString(bytes, encoding, safeString);
                }
            }

            span = _first.Slice(Position, index);
        }
        else
        {
            size = Math.Min(remaining, size);
            span = _second.Slice( Position - _first.Length, size);
            index = span.IndexOfTerminator(byteLength);

            if (index >= 0)
            {
                span = span[..index];
            }
            else
            {
                index = size;
            }
        }

        Position += isFixedLength ? size : index + byteLength;
        return TextEncoding.GetString(span, encoding, safeString);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUniSafe(int fixedLength) => ReadString(TextEncoding.UnicodeLE, true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUniSafe() => ReadString(TextEncoding.UnicodeLE, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUni(int fixedLength) => ReadString(TextEncoding.UnicodeLE, false, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUni() => ReadString(TextEncoding.UnicodeLE);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUniSafe(int fixedLength) => ReadString(TextEncoding.Unicode, true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUniSafe() => ReadString(TextEncoding.Unicode, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUni(int fixedLength) => ReadString(TextEncoding.Unicode, false, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUni() => ReadString(TextEncoding.Unicode);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8Safe(int fixedLength) => ReadString(TextEncoding.UTF8, true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8Safe() => ReadString(TextEncoding.UTF8, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8() => ReadString(TextEncoding.UTF8);

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
            SeekOrigin.End   => Length + offset,
            _                => Position + offset // Current
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Read(Span<byte> bytes)
    {
        if (bytes.Length < Length)
        {
            throw new ArgumentOutOfRangeException(nameof(bytes));
        }

        if (_first.Length > 0 && !_first.TryCopyTo(bytes))
        {
            return false;
        }

        return _second.Length <= 0 || _second.TryCopyTo(bytes[_first.Length..]);
    }
}
