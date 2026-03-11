/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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

using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server;
using Server.Text;

namespace System.Buffers;

public ref struct SpanReader
{
    private readonly ReadOnlySpan<byte> _buffer;

    public int Length { get; }
    public int Position { get; private set; }
    public int Remaining => Length - Position;

    public ReadOnlySpan<byte> Buffer => _buffer;

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
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
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
        if (!BinaryPrimitives.TryReadInt16BigEndian(_buffer[Position..], out var value))
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        Position += 2;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16LE()
    {
        if (!BinaryPrimitives.TryReadInt16LittleEndian(_buffer[Position..], out var value))
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        Position += 2;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16()
    {
        if (!BinaryPrimitives.TryReadUInt16BigEndian(_buffer[Position..], out var value))
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        Position += 2;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16LE()
    {
        if (!BinaryPrimitives.TryReadUInt16LittleEndian(_buffer[Position..], out var value))
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        Position += 2;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32()
    {
        if (!BinaryPrimitives.TryReadInt32BigEndian(_buffer[Position..], out var value))
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        Position += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32()
    {
        if (!BinaryPrimitives.TryReadUInt32BigEndian(_buffer[Position..], out var value))
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        Position += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32LE()
    {
        if (!BinaryPrimitives.TryReadUInt32LittleEndian(_buffer[Position..], out var value))
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        Position += 4;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64()
    {
        if (!BinaryPrimitives.TryReadInt64BigEndian(_buffer[Position..], out var value))
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        Position += 8;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64()
    {
        if (!BinaryPrimitives.TryReadUInt64BigEndian(_buffer[Position..], out var value))
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        Position += 8;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(Encoding encoding, bool safeString = false, int fixedLength = -1)
    {
        if (fixedLength == 0)
        {
            return "";
        }

        var byteLength = encoding.GetByteLengthForEncoding();

        var isFixedLength = fixedLength > -1;

        var remaining = Remaining;
        int size;
        if (isFixedLength)
        {
            size = fixedLength * byteLength;
            if (size > Remaining)
            {
                throw new EndOfStreamException("Cannot read past the end of the buffer.");
            }
        }
        else
        {
            // In case the remaining is not evenly divisible
            size = remaining - (remaining & (byteLength - 1));
        }

        var span = _buffer.Slice(Position, size);
        var index = span.IndexOfTerminator(byteLength);

        if (index > -1)
        {
            span = _buffer.Slice(Position, index);
        }

        Position += isFixedLength || index < 0 ? size : index + 1;

        // The string is either as long as the first terminator character, remaining buffer size, or fixed length.
        return TextEncoding.GetString(span, encoding, safeString);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUniSafe(int fixedLength) => ReadStringLittleUni(true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUniSafe() => ReadStringLittleUni(true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUni(int fixedLength) => ReadStringLittleUni(false, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUni() => ReadStringLittleUni(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUniSafe(int fixedLength) => ReadStringBigUni(true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUniSafe() => ReadStringBigUni(true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUni(int fixedLength) => ReadStringBigUni(false, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUni() => ReadStringBigUni(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8Safe(int fixedLength) => ReadStringUtf8(true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8Safe() => ReadStringUtf8(true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8(int fixedLength) => ReadStringUtf8(false, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8() => ReadStringUtf8(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAsciiSafe(int fixedLength) => ReadStringAscii(true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAsciiSafe() => ReadStringAscii(true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAscii(int fixedLength) => ReadStringAscii(false, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAscii() => ReadStringAscii(false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLatin1Safe(int fixedLength) => ReadStringLatin1(true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLatin1Safe() => ReadStringLatin1(true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLatin1(int fixedLength) => ReadStringLatin1(false, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLatin1() => ReadStringLatin1(false);

    /// <summary>
    /// Reads a big-endian UTF-16 string, filtering control codes and non-characters in safe mode.
    /// </summary>
    private string ReadStringBigUni(bool safeString, int fixedLength = -1)
    {
        if (fixedLength == 0)
        {
            return "";
        }

        const int byteLength = 2;
        var isFixedLength = fixedLength > -1;

        var remaining = Remaining;
        int size;
        if (isFixedLength)
        {
            size = fixedLength * byteLength;
            if (size > remaining)
            {
                throw new EndOfStreamException("Cannot read past the end of the buffer.");
            }
        }
        else
        {
            // Ensure even number of bytes
            size = remaining - (remaining & 1);
        }

        var span = _buffer.Slice(Position, size);
        var index = span.IndexOfTerminator(byteLength);

        if (index > -1)
        {
            span = _buffer.Slice(Position, index);
        }

        Position += isFixedLength || index < 0 ? size : index + byteLength;

        return TextEncoding.GetStringBigUni(span, safeString);
    }

    /// <summary>
    /// Reads a little-endian UTF-16 string, filtering control codes and non-characters in safe mode.
    /// </summary>
    private string ReadStringLittleUni(bool safeString, int fixedLength = -1)
    {
        if (fixedLength == 0)
        {
            return "";
        }

        const int byteLength = 2;
        var isFixedLength = fixedLength > -1;

        var remaining = Remaining;
        int size;
        if (isFixedLength)
        {
            size = fixedLength * byteLength;
            if (size > remaining)
            {
                throw new EndOfStreamException("Cannot read past the end of the buffer.");
            }
        }
        else
        {
            // Ensure even number of bytes
            size = remaining - (remaining & 1);
        }

        var span = _buffer.Slice(Position, size);
        var index = span.IndexOfTerminator(byteLength);

        if (index > -1)
        {
            span = _buffer.Slice(Position, index);
        }

        Position += isFixedLength || index < 0 ? size : index + byteLength;

        return TextEncoding.GetStringLittleUni(span, safeString);
    }

    /// <summary>
    /// Reads an ASCII string, filtering C0 control codes and DEL in safe mode.
    /// </summary>
    private string ReadStringAscii(bool safeString, int fixedLength = -1)
    {
        if (fixedLength == 0)
        {
            return "";
        }

        var isFixedLength = fixedLength > -1;

        int size = isFixedLength ? fixedLength : Remaining;
        if (size > Remaining)
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        var span = _buffer.Slice(Position, size);
        var index = span.IndexOf((byte)0);

        if (index > -1)
        {
            span = _buffer.Slice(Position, index);
        }

        Position += isFixedLength || index < 0 ? size : index + 1;

        return TextEncoding.GetStringAscii(span, safeString);
    }

    /// <summary>
    /// Reads a UTF-8 string, filtering control codes and non-characters in safe mode.
    /// </summary>
    private string ReadStringUtf8(bool safeString, int fixedLength = -1)
    {
        if (fixedLength == 0)
        {
            return "";
        }

        var isFixedLength = fixedLength > -1;

        int size = isFixedLength ? fixedLength : Remaining;
        if (size > Remaining)
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        var span = _buffer.Slice(Position, size);
        var index = span.IndexOf((byte)0);

        if (index > -1)
        {
            span = _buffer.Slice(Position, index);
        }

        Position += isFixedLength || index < 0 ? size : index + 1;

        return TextEncoding.GetStringUtf8(span, safeString);
    }

    /// <summary>
    /// Reads a Latin1 string, filtering C0/C1 control codes in safe mode.
    /// </summary>
    private string ReadStringLatin1(bool safeString, int fixedLength = -1)
    {
        if (fixedLength == 0)
        {
            return "";
        }

        var isFixedLength = fixedLength > -1;

        int size = isFixedLength ? fixedLength : Remaining;
        if (size > Remaining)
        {
            throw new EndOfStreamException("Cannot read past the end of the buffer.");
        }

        var span = _buffer.Slice(Position, size);
        var index = span.IndexOf((byte)0);

        if (index > -1)
        {
            span = _buffer.Slice(Position, index);
        }

        Position += isFixedLength || index < 0 ? size : index + 1;

        return TextEncoding.GetStringLatin1(span, safeString);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Seek(int offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End     => _buffer.Length + offset,
            _                  => offset // Begin
        };

        if (newPosition < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Seek operation would result in a negative position.");
        }

        if (newPosition > _buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"Cannot seek to position {newPosition} beyond buffer length {_buffer.Length}.");
        }

        return Position = newPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Read(scoped Span<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return 0;
        }

        var bytesWritten = Math.Min(bytes.Length, Remaining);
        _buffer.Slice(Position, bytesWritten).CopyTo(bytes);

        Position += bytesWritten;
        return bytesWritten;
    }
}
