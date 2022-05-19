/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: BufferedFileWriter.cs                                           *
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
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Server.Collections;
using Server.Text;

namespace Server
{
    public class BufferWriter : IGenericWriter
    {
        private readonly Encoding m_Encoding;
        private readonly bool m_PrefixStrings;
        private long _bytesWritten;
        private long _index;

        protected long Index
        {
            get => _index;
            set
            {
                if (value < 0 || value > _buffer.Length)
                {
                    // If you are receiving this exception and your value is too large, you may need to use `Resize`
                    // If you are receiving this exception and your value is negative, you probably used Seek incorrectly.
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _index = value;

                if (value > _bytesWritten)
                {
                    _bytesWritten = value;
                }
            }
        }

        private byte[] _buffer;

        public BufferWriter(byte[] buffer, bool prefixStr)
        {
            m_PrefixStrings = prefixStr;
            m_Encoding = TextEncoding.UTF8;
            _buffer = buffer;
        }

        public BufferWriter(bool prefixStr) : this(0, prefixStr)
        {
        }

        public BufferWriter(int count, bool prefixStr)
        {
            m_PrefixStrings = prefixStr;
            m_Encoding = TextEncoding.UTF8;
            _buffer = GC.AllocateUninitializedArray<byte>(count < 1 ? BufferSize : count);
        }

        public virtual long Position => Index;

        protected virtual int BufferSize => 256;

        public byte[] Buffer => _buffer;

        public virtual void Close()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Resize(int size)
        {
            // We shouldn't ever resize to a 0 length buffer. That is dangerous
            if (size <= 0)
            {
                size = BufferSize;
            }

            if (size < _buffer.Length)
            {
                _bytesWritten = size;
            }

            var newBuffer = GC.AllocateUninitializedArray<byte>(size);
            _buffer.AsSpan(0, Math.Min(size, _buffer.Length)).CopyTo(newBuffer);
            _buffer = newBuffer;
        }

        public virtual void Flush()
        {
            // Need to avoid buffer.Length = 2, buffer * 2 is 4, but we need 8 or 16bytes, causing an exception.
            // The least we need is 16bytes + Index, but we use BufferSize since it should always be big enough for a single
            // non-dynamic field.
            Resize(Math.Max(BufferSize, _buffer.Length * 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FlushIfNeeded(int amount)
        {
            if (Index + amount > _buffer.Length)
            {
                Flush();
            }
        }

        public void Write(ReadOnlySpan<byte> bytes)
        {
            var remaining = bytes.Length;
            var idx = 0;

            while (remaining > 0)
            {
                FlushIfNeeded(remaining);

                var count = Math.Min((int)(_buffer.Length - Index), remaining);
                bytes.Slice(idx, count).CopyTo(_buffer.AsSpan((int)Index, count));

                idx += count;
                Index += count;
                remaining -= count;
            }
        }

        public void Write(BitArray bitArray)
        {
            var byteLength = BitArray.GetByteArrayLengthFromBitLength(bitArray.Length);

            ((IGenericWriter)this).WriteEncodedInt(bitArray.Length);
            FlushIfNeeded(byteLength);
            bitArray.CopyTo(_buffer.AsSpan((int)Index, byteLength));
            Index += byteLength;
        }

        public virtual long Seek(long offset, SeekOrigin origin)
        {
            Debug.Assert(
                origin != SeekOrigin.End || offset <= 0 && offset > -_buffer.Length,
                "Attempting to seek to an invalid position using SeekOrigin.End"
            );
            Debug.Assert(
                origin != SeekOrigin.Begin || offset >= 0 && offset < _buffer.Length,
                "Attempting to seek to an invalid position using SeekOrigin.Begin"
            );
            Debug.Assert(
                origin != SeekOrigin.Current || Index + offset >= 0 && Index + offset < _buffer.Length,
                "Attempting to seek to an invalid position using SeekOrigin.Current"
            );

            return Index = Math.Max(0, origin switch
            {
                SeekOrigin.Current => Index + offset,
                SeekOrigin.End     => _bytesWritten + offset,
                _   => offset // Begin
            });
        }

        public void Write(string value)
        {
            if (m_PrefixStrings)
            {
                if (value == null)
                {
                    Write(false);
                }
                else
                {
                    Write(true);
                    InternalWriteString(value);
                }
            }
            else
            {
                InternalWriteString(value);
            }
        }

        public void Write(long value)
        {
            FlushIfNeeded(8);

            _buffer[Index++] = (byte)value;
            _buffer[Index++] = (byte)(value >> 8);
            _buffer[Index++] = (byte)(value >> 16);
            _buffer[Index++] = (byte)(value >> 24);
            _buffer[Index++] = (byte)(value >> 32);
            _buffer[Index++] = (byte)(value >> 40);
            _buffer[Index++] = (byte)(value >> 48);
            _buffer[Index++] = (byte)(value >> 56);
        }

        public void Write(ulong value)
        {
            FlushIfNeeded(8);

            _buffer[Index++] = (byte)value;
            _buffer[Index++] = (byte)(value >> 8);
            _buffer[Index++] = (byte)(value >> 16);
            _buffer[Index++] = (byte)(value >> 24);
            _buffer[Index++] = (byte)(value >> 32);
            _buffer[Index++] = (byte)(value >> 40);
            _buffer[Index++] = (byte)(value >> 48);
            _buffer[Index++] = (byte)(value >> 56);
        }

        public void Write(int value)
        {
            FlushIfNeeded(4);

            _buffer[Index++] = (byte)value;
            _buffer[Index++] = (byte)(value >> 8);
            _buffer[Index++] = (byte)(value >> 16);
            _buffer[Index++] = (byte)(value >> 24);
        }

        public void Write(uint value)
        {
            FlushIfNeeded(4);

            _buffer[Index++] = (byte)value;
            _buffer[Index++] = (byte)(value >> 8);
            _buffer[Index++] = (byte)(value >> 16);
            _buffer[Index++] = (byte)(value >> 24);
        }

        public void Write(short value)
        {
            FlushIfNeeded(2);

            _buffer[Index++] = (byte)value;
            _buffer[Index++] = (byte)(value >> 8);
        }

        public void Write(ushort value)
        {
            FlushIfNeeded(2);

            _buffer[Index++] = (byte)value;
            _buffer[Index++] = (byte)(value >> 8);
        }

        public unsafe void Write(double value)
        {
            FlushIfNeeded(8);

            fixed (byte* pBuffer = _buffer)
            {
                *(double*)(pBuffer + Index) = value;
            }

            Index += 8;
        }

        public unsafe void Write(float value)
        {
            FlushIfNeeded(4);

            fixed (byte* pBuffer = _buffer)
            {
                *(float*)(pBuffer + Index) = value;
            }

            Index += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            FlushIfNeeded(1);
            _buffer[Index++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(sbyte value)
        {
            FlushIfNeeded(1);
            _buffer[Index++] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write(bool value)
        {
            FlushIfNeeded(1);
            _buffer[Index++] = *(byte*)&value; // up to 30% faster to dereference the raw value on the stack
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(Serial serial) => Write(serial.Value);

        internal void InternalWriteString(string value)
        {
            var remaining = m_Encoding.GetByteCount(value);

            ((IGenericWriter)this).WriteEncodedInt(remaining);

            if (remaining == 0)
            {
                return;
            }

            // It is much faster to encode to stack buffer, then copy to the real buffer
            Span<byte> span = stackalloc byte[Math.Min(BufferSize, 256)];
            var maxChars = span.Length / m_Encoding.GetMaxByteCount(1);
            var charsLeft = value.Length;
            var current = 0;

            while (charsLeft > 0)
            {
                var charCount = Math.Min(charsLeft, maxChars);
                var bytesWritten = m_Encoding.GetBytes(value.AsSpan(current, charCount), span);
                remaining -= bytesWritten;
                charsLeft -= charCount;
                current += charCount;

                Write(span[..bytesWritten]);
            }
        }
    }
}
