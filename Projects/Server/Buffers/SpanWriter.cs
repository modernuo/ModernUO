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

using System.Buffers.Binary;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server;

namespace System.Buffers
{
    public ref struct SpanWriter
    {
        private readonly bool _resize;
        private byte[]? _arrayToReturnToPool;
        private Span<byte> _buffer;
        private int _position;

        public int Length { get; private set; }

        public int Position
        {
            get => _position;
            private set
            {
                _position = value;

                if (value > Length)
                {
                    Length = value;
                }
            }
        }

        public int Capacity => _buffer.Length;

        public ReadOnlySpan<byte> Span => _buffer.Slice(0, Position);

        public Span<byte> RawBuffer => _buffer;

        public SpanWriter(Span<byte> initialBuffer, bool resize = false)
        {
            _resize = resize;
            _buffer = initialBuffer;
            _position = 0;
            Length = 0;
            _arrayToReturnToPool = null;
        }

        public SpanWriter(int initialCapacity, bool resize = false)
        {
            _resize = resize;
            _arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _buffer = _arrayToReturnToPool;
            _position = 0;
            Length = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Grow(int additionalCapacity)
        {
            var newSize = Math.Max(Length + additionalCapacity, _buffer.Length * 2);
            byte[] poolArray = ArrayPool<byte>.Shared.Rent(newSize);

            _buffer.Slice(0, Length).CopyTo(poolArray);

            byte[]? toReturn = _arrayToReturnToPool;
            _buffer = _arrayToReturnToPool = poolArray;
            if (toReturn != null)
            {
                ArrayPool<byte>.Shared.Return(toReturn);
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

        public void EnsureCapacity(int capacity)
        {
            if (capacity > _buffer.Length)
            {
                if (!_resize)
                {
                    throw new OutOfMemoryException();
                }

                Grow(capacity - Length);
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
            BinaryPrimitives.WriteInt16BigEndian(_buffer.Slice(_position), value);
            Position += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ushort value)
        {
            GrowIfNeeded(2);
            BinaryPrimitives.WriteUInt16BigEndian(_buffer.Slice(_position), value);
            Position += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int value)
        {
            GrowIfNeeded(4);
            BinaryPrimitives.WriteInt32BigEndian(_buffer.Slice(_position), value);
            Position += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(uint value)
        {
            GrowIfNeeded(4);
            BinaryPrimitives.WriteUInt32BigEndian(_buffer.Slice(_position), value);
            Position += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(long value)
        {
            GrowIfNeeded(8);
            BinaryPrimitives.WriteInt64BigEndian(_buffer.Slice(_position), value);
            Position += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ulong value)
        {
            GrowIfNeeded(8);
            BinaryPrimitives.WriteUInt64BigEndian(_buffer.Slice(_position), value);
            Position += 8;
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            var count = buffer.Length;
            GrowIfNeeded(count);
            buffer.CopyTo(_buffer.Slice(_position));
            Position += count;
        }

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

            GrowIfNeeded(byteCount);

            var bytesWritten = encoding.GetBytes(src, _buffer.Slice(_position));
            Position += bytesWritten;

            if (fixedLength > -1)
            {
                var extra = fixedLength * sizeT - bytesWritten;
                if (extra > 0)
                {
                    Clear(extra);
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
            Write((ushort)0); // '\0'
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBigUni(string value, int fixedLength) => WriteString<char>(value, Utility.Unicode, fixedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUTF8(string value) => WriteString<byte>(value, Utility.UTF8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUTF8Null(string value)
        {
            WriteString<byte>(value, Utility.UTF8);
            Write((byte)0); // '\0'
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAscii(string value) => WriteString<byte>(value, Encoding.ASCII);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAsciiNull(string value)
        {
            WriteString<byte>(value, Encoding.ASCII);
            Write((byte)0); // '\0'
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAscii(string value, int fixedLength) => WriteString<byte>(value, Encoding.ASCII, fixedLength);

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
                origin != SeekOrigin.End || (_resize || offset <= 0) && offset > -_buffer.Length,
                "Attempting to seek to an invalid position using SeekOrigin.End"
            );
            Debug.Assert(
                origin != SeekOrigin.Begin || offset >= 0 && (_resize || offset < _buffer.Length),
                "Attempting to seek to an invalid position using SeekOrigin.Begin"
            );
            Debug.Assert(
                origin != SeekOrigin.Current || Position + offset >= 0 && (_resize || Position + offset < _buffer.Length),
                "Attempting to seek to an invalid position using SeekOrigin.Current"
            );

            var newPosition = Math.Max(0, origin switch
            {
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End     => Length + offset,
                _                  => offset // Begin
            });

            if (newPosition > _buffer.Length)
            {
                Grow(newPosition - _buffer.Length);
            }

            return newPosition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            byte[] toReturn = _arrayToReturnToPool;
            this = default; // for safety, to avoid using pooled array if this instance is erroneously appended to again
            if (toReturn != null)
            {
                ArrayPool<byte>.Shared.Return(toReturn);
            }
        }
    }
}
