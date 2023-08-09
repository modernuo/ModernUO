/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
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

using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Toolkit.HighPerformance;
using Server;
using Server.Buffers;
using Server.Text;

namespace System.Buffers;

public ref struct SpanWriter
{
    private readonly bool _resize;
    private byte[] _arrayToReturnToPool;
    private Span<byte> _buffer;
    private int _position;

    public int BytesWritten { get; private set; }

    public int Position
    {
        get => _position;
        private set
        {
            _position = value;

            if (value > BytesWritten)
            {
                BytesWritten = value;
            }
        }
    }

    public int Capacity => _buffer.Length;

    public ReadOnlySpan<byte> Span => _buffer[..Position];

    public Span<byte> RawBuffer => _buffer;

    /**
         * Converts the writer to a Span<byte> using a SpanOwner.
         * If the buffer was stackalloc, it will be copied to a rented buffer.
         * Otherwise the existing rented buffer is used.
         *
         * Note:
         * Do not use the SpanWriter after calling this method.
         * This method will effectively dispose of the SpanWriter and is therefore considered terminal.
         */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanOwner ToSpan()
    {
        var toReturn = _arrayToReturnToPool;

        SpanOwner apo;
        if (_position == 0)
        {
            apo = new SpanOwner(_position, Array.Empty<byte>());
            if (toReturn != null)
            {
                STArrayPool<byte>.Shared.Return(toReturn);
            }
        }
        else if (toReturn != null)
        {
            apo = new SpanOwner(_position, toReturn);
        }
        else
        {
            var buffer = STArrayPool<byte>.Shared.Rent(_position);
            _buffer.CopyTo(buffer);
            apo = new SpanOwner(_position, buffer);
        }

        this = default; // Don't allow two references to the same buffer
        return apo;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanWriter(Span<byte> initialBuffer, bool resize = false)
    {
        _resize = resize;
        _buffer = initialBuffer;
        _position = 0;
        BytesWritten = 0;
        _arrayToReturnToPool = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanWriter(int initialCapacity, bool resize = false)
    {
        _resize = resize;
        _arrayToReturnToPool = STArrayPool<byte>.Shared.Rent(initialCapacity);
        _buffer = _arrayToReturnToPool;
        _position = 0;
        BytesWritten = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalCapacity)
    {
        var newSize = Math.Max(BytesWritten + additionalCapacity, _buffer.Length * 2);
        byte[] poolArray = STArrayPool<byte>.Shared.Rent(newSize);

        _buffer[..BytesWritten].CopyTo(poolArray);

        byte[] toReturn = _arrayToReturnToPool;
        _buffer = _arrayToReturnToPool = poolArray;
        if (toReturn != null)
        {
            STArrayPool<byte>.Shared.Return(toReturn);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowIfNeeded(int count)
    {
        if (_position + count > _buffer.Length)
        {
            if (!_resize)
            {
                throw new OutOfMemoryException();
            }

            Grow(count);
        }
    }

    public ref byte GetPinnableReference() => ref MemoryMarshal.GetReference(_buffer);

    public void EnsureCapacity(int capacity)
    {
        if (capacity > _buffer.Length)
        {
            if (!_resize)
            {
                throw new OutOfMemoryException();
            }

            Grow(capacity - BytesWritten);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Write(bool value)
    {
        GrowIfNeeded(1);
        _buffer[Position++] = *(byte*)&value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value)
    {
        GrowIfNeeded(1);
        _buffer[Position++] = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(sbyte value)
    {
        GrowIfNeeded(1);
        _buffer[Position++] = (byte)value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(short value)
    {
        GrowIfNeeded(2);
        BinaryPrimitives.WriteInt16BigEndian(_buffer[_position..], value);
        Position += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLE(short value)
    {
        GrowIfNeeded(2);
        BinaryPrimitives.WriteInt16LittleEndian(_buffer[_position..], value);
        Position += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ushort value)
    {
        GrowIfNeeded(2);
        BinaryPrimitives.WriteUInt16BigEndian(_buffer[_position..], value);
        Position += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLE(ushort value)
    {
        GrowIfNeeded(2);
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer[_position..], value);
        Position += 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int value)
    {
        GrowIfNeeded(4);
        BinaryPrimitives.WriteInt32BigEndian(_buffer[_position..], value);
        Position += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLE(int value)
    {
        GrowIfNeeded(4);
        BinaryPrimitives.WriteInt32LittleEndian(_buffer[_position..], value);
        Position += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value)
    {
        GrowIfNeeded(4);
        BinaryPrimitives.WriteUInt32BigEndian(_buffer[_position..], value);
        Position += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Serial serial) => Write(serial.Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLE(uint value)
    {
        GrowIfNeeded(4);
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer[_position..], value);
        Position += 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(long value)
    {
        GrowIfNeeded(8);
        BinaryPrimitives.WriteInt64BigEndian(_buffer[_position..], value);
        Position += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ulong value)
    {
        GrowIfNeeded(8);
        BinaryPrimitives.WriteUInt64BigEndian(_buffer[_position..], value);
        Position += 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<byte> buffer)
    {
        var count = buffer.Length;
        GrowIfNeeded(count);
        buffer.CopyTo(_buffer[_position..]);
        Position += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAscii(char chr) => Write((byte)chr);

    public void WriteAscii(
        ref RawInterpolatedStringHandler handler)
    {
        Write(handler.Text, Encoding.ASCII);
        handler.Clear();
    }

    public void WriteAscii(
        IFormatProvider? formatProvider,
        [InterpolatedStringHandlerArgument("formatProvider")]
        ref RawInterpolatedStringHandler handler)
    {
        Write(handler.Text, Encoding.ASCII);
        handler.Clear();
    }

    public void Write(
        Encoding encoding,
        ref RawInterpolatedStringHandler handler)
    {
        Write(handler.Text, encoding);
        handler.Clear();
    }

    public void Write(
        Encoding encoding,
        IFormatProvider? formatProvider,
        [InterpolatedStringHandlerArgument("formatProvider")]
        ref RawInterpolatedStringHandler handler)
    {
        Write(handler.Text, encoding);
        handler.Clear();
    }

    public void Write(ReadOnlySpan<char> value, Encoding encoding, int fixedLength = -1)
    {
        var charLength = Math.Min(fixedLength > -1 ? fixedLength : value.Length, value.Length);
        var src = value[..charLength];

        var byteLength = encoding.GetByteLengthForEncoding();
        var byteCount = encoding.GetByteCount(src);
        if (fixedLength > src.Length)
        {
            byteCount += (fixedLength - src.Length) * byteLength;
        }

        if (byteCount == 0)
        {
            return;
        }

        GrowIfNeeded(byteCount);

        var bytesWritten = encoding.GetBytes(src, _buffer[_position..]);
        Position += bytesWritten;

        if (fixedLength > -1)
        {
            var extra = fixedLength * byteLength - bytesWritten;
            if (extra > 0)
            {
                Clear(extra);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLittleUni(string value) => Write(value, TextEncoding.UnicodeLE);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLittleUniNull(string value)
    {
        Write(value, TextEncoding.UnicodeLE);
        Write((ushort)0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLittleUni(string value, int fixedLength) => Write(value, TextEncoding.UnicodeLE, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBigUni(string value) => Write(value, TextEncoding.Unicode);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBigUniNull(string value)
    {
        Write(value, TextEncoding.Unicode);
        Write((ushort)0); // '\0'
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBigUni(string value, int fixedLength) => Write(value, TextEncoding.Unicode, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUTF8(string value) => Write(value, TextEncoding.UTF8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUTF8Null(string value)
    {
        Write(value, TextEncoding.UTF8);
        Write((byte)0); // '\0'
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAscii(string value) => Write(value, Encoding.ASCII);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAsciiNull(string value)
    {
        Write(value, Encoding.ASCII);
        Write((byte)0); // '\0'
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAscii(string value, int fixedLength) => Write(value, Encoding.ASCII, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(int count)
    {
        GrowIfNeeded(count);
        _buffer.Slice(_position, count).Clear();
        Position += count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Seek(int offset, SeekOrigin origin)
    {
        Debug.Assert(
            origin != SeekOrigin.End || _resize || offset <= 0,
            "Attempting to seek to a position beyond capacity using SeekOrigin.End without resize"
        );

        Debug.Assert(
            origin != SeekOrigin.End || offset >= -_buffer.Length,

            "Attempting to seek to a negative position using SeekOrigin.End"
        );

        Debug.Assert(
            origin != SeekOrigin.Begin || offset >= 0,
            "Attempting to seek to a negative position using SeekOrigin.Begin"
        );

        Debug.Assert(
            origin != SeekOrigin.Begin || _resize || offset <= _buffer.Length,
            "Attempting to seek to a position beyond the capacity using SeekOrigin.Begin without resize"
        );

        Debug.Assert(
            origin != SeekOrigin.Current || _position + offset >= 0,
            "Attempting to seek to a negative position using SeekOrigin.Current"
        );

        Debug.Assert(
            origin != SeekOrigin.Current || _resize || _position + offset <= _buffer.Length,
            "Attempting to seek to a position beyond the capacity using SeekOrigin.Current without resize"
        );

        var newPosition = Math.Max(0, origin switch
        {
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End     => BytesWritten + offset,
            _                  => offset // Begin
        });

        if (newPosition >= _buffer.Length)
        {
            Grow(newPosition - _buffer.Length + 1);
        }

        return Position = newPosition;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        byte[] toReturn = _arrayToReturnToPool;
        this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
        if (toReturn != null)
        {
            STArrayPool<byte>.Shared.Return(toReturn);
        }
    }

    public struct SpanOwner : IDisposable
    {
        private readonly int _length;
        private readonly byte[] _arrayToReturnToPool;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SpanOwner(int length, byte[] buffer)
        {
            _length = length;
            _arrayToReturnToPool = buffer;
        }

        public Span<byte> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MemoryMarshal.CreateSpan(ref _arrayToReturnToPool.DangerousGetReference(), _length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            byte[] toReturn = _arrayToReturnToPool;
            this = default;
            if (_length > 0)
            {
                STArrayPool<byte>.Shared.Return(toReturn);
            }
        }
    }
}
