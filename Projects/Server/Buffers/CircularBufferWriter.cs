/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
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
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Text;

namespace System.Buffers;

public ref struct CircularBufferWriter
{
    private readonly Span<byte> _first;
    private readonly Span<byte> _second;

    public int Length { get; }
    public int Position { get; private set; }

    public CircularBufferWriter(CircularBuffer<byte> buffer) : this(buffer.GetSpan(0), buffer.GetSpan(1))
    {
    }

    public CircularBufferWriter(ArraySegment<byte>[] buffer) : this(buffer[0], buffer[1])
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
            if (!BinaryPrimitives.TryWriteInt16BigEndian(_first[Position..], value))
            {
                if (BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }

                Write((byte)(value >> 8));
                Write((byte)value);
            }
            else
            {
                Position += 2;
            }
        }
        else if (BinaryPrimitives.TryWriteInt16BigEndian(_second[(Position - _first.Length)..], value))
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
            if (!BinaryPrimitives.TryWriteUInt16BigEndian(_first[Position..], value))
            {
                if (BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }

                Write((byte)(value >> 8));
                Write((byte)value);
            }
            else
            {
                Position += 2;
            }
        }
        else if (BinaryPrimitives.TryWriteUInt16BigEndian(_second[(Position - _first.Length)..], value))
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
            if (!BinaryPrimitives.TryWriteInt32BigEndian(_first[Position..], value))
            {
                if (BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }

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
        else if (BinaryPrimitives.TryWriteInt32BigEndian(_second[(Position - _first.Length)..], value))
        {
            Position += 4;
        }
        else
        {
            throw new OutOfMemoryException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLE(int value)
    {
        if (Position < _first.Length)
        {
            if (!BinaryPrimitives.TryWriteInt32LittleEndian(_first[Position..], value))
            {
                if (!BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }

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
        else if (BinaryPrimitives.TryWriteInt32LittleEndian(_second[(Position - _first.Length)..], value))
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
            if (!BinaryPrimitives.TryWriteUInt32BigEndian(_first[Position..], value))
            {
                if (BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }

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
        else if (BinaryPrimitives.TryWriteUInt32BigEndian(_second[(Position - _first.Length)..], value))
        {
            Position += 4;
        }
        else
        {
            throw new OutOfMemoryException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLE(uint value)
    {
        if (Position < _first.Length)
        {
            if (!BinaryPrimitives.TryWriteUInt32LittleEndian(_first[Position..], value))
            {
                if (!BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }

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
        else if (BinaryPrimitives.TryWriteUInt32LittleEndian(_second[(Position - _first.Length)..], value))
        {
            Position += 4;
        }
        else
        {
            throw new OutOfMemoryException();
        }
    }

    /// <summary>
    /// Writes a 8-byte signed integer value to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(long value)
    {
        if (Position < _first.Length)
        {
            if (!BinaryPrimitives.TryWriteInt64BigEndian(_first[Position..], value))
            {
                if (BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }

                Write((byte)(value >> 56));
                Write((byte)(value >> 48));
                Write((byte)(value >> 40));
                Write((byte)(value >> 32));
                Write((byte)(value >> 24));
                Write((byte)(value >> 16));
                Write((byte)(value >> 8));
                Write((byte)value);

            }
            else
            {
                Position += 8;
            }
        }
        else if (BinaryPrimitives.TryWriteInt64BigEndian(_second[(Position - _first.Length)..], value))
        {
            Position += 8;
        }
        else
        {
            throw new OutOfMemoryException();
        }
    }

    /// <summary>
    /// Writes a 8-byte unsigned integer value to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ulong value)
    {
        if (Position < _first.Length)
        {
            if (!BinaryPrimitives.TryWriteUInt64BigEndian(_first[Position..], value))
            {
                if (BitConverter.IsLittleEndian)
                {
                    value = BinaryPrimitives.ReverseEndianness(value);
                }

                Write((byte)(value >> 56));
                Write((byte)(value >> 48));
                Write((byte)(value >> 40));
                Write((byte)(value >> 32));
                Write((byte)(value >> 24));
                Write((byte)(value >> 16));
                Write((byte)(value >> 8));
                Write((byte)value);
            }
            else
            {
                Position += 8;
            }
        }
        else if (BinaryPrimitives.TryWriteUInt64BigEndian(_second[(Position - _first.Length)..], value))
        {
            Position += 8;
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
            buffer[..sz].CopyTo(_first[Position..]);
            if (sz < buffer.Length)
            {
                buffer[sz..].CopyTo(_second);
            }
        }
        else if (Position < Length)
        {
            buffer.CopyTo(_second[(Position - _first.Length)..]);
        }
        else
        {
            throw new OutOfMemoryException();
        }

        Position += buffer.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(ReadOnlySpan<char> value, Encoding encoding)
    {
        var byteCount = encoding.GetByteCount(value);

        if (Position + byteCount > Length)
        {
            throw new OutOfMemoryException();
        }

        Span<byte> bytes = stackalloc byte[byteCount];
        encoding.GetBytes(value, bytes);

        int count;
        if (Position < _first.Length)
        {
            count = Math.Min(_first.Length - Position, byteCount);
            bytes[..count].CopyTo(_first[Position..]);
            byteCount -= count;
            Position += count;
        }
        else
        {
            count = 0;
        }

        if (byteCount > 0)
        {
            bytes[count..].CopyTo(_second[Math.Max(0, Position - _first.Length)..]);
            Position += byteCount;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLittleUni(string value, int fixedLength = -1)
    {
        if (fixedLength < 0) { fixedLength = value.Length; }

        WriteString(value.AsSpan(0, Math.Min(fixedLength, value.Length)), TextEncoding.UnicodeLE);
        var count = fixedLength - value.Length;
        if (count > 0)
        {
            Clear(count * 2);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLittleUniNull(string value)
    {
        WriteString(value, TextEncoding.UnicodeLE);
        Write((ushort)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBigUni(string value, int fixedLength = -1)
    {
        if (fixedLength < 0) { fixedLength = value.Length; }

        WriteString(value.AsSpan(0, Math.Min(fixedLength, value.Length)), TextEncoding.Unicode);
        var count = fixedLength - value.Length;
        if (count > 0)
        {
            Clear(count * 2);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBigUniNull(string value)
    {
        WriteString(value, TextEncoding.Unicode);
        Write((ushort)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUTF8(string value) => WriteString(value, TextEncoding.UTF8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUTF8Null(string value)
    {
        WriteString(value, TextEncoding.UTF8);
        Write((byte)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAscii(string value, int fixedLength = -1)
    {
        if (fixedLength < 0) { fixedLength = value.Length; }

        WriteString(value.AsSpan(0, Math.Min(fixedLength, value.Length)), Encoding.ASCII);
        var count = fixedLength - value.Length;
        if (count > 0)
        {
            Clear(count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAsciiNull(string value)
    {
        WriteString(value, Encoding.ASCII);
        Write((byte)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(int amount)
    {
        if (Position + amount > Length)
        {
            throw new OutOfMemoryException();
        }

        int count;
        if (Position < _first.Length)
        {
            count = Math.Min(amount, _first.Length - Position);
            _first.Slice(Position, count).Clear();
            count = amount - count;
        }
        else
        {
            count = amount;
        }

        if (count > 0)
        {
            _second.Slice(Position - _first.Length, count).Clear();
        }

        Position += amount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Seek(int offset, SeekOrigin origin) =>
        Position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.End   => Length + offset,
            _                => Position + offset // Current
        };
}
