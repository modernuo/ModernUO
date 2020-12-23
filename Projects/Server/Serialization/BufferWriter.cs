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
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

namespace Server
{
    public class BufferWriter : IGenericWriter
    {
        private readonly Encoding m_Encoding;
        private readonly bool m_PrefixStrings;

        protected long Index { get; set; }
        private byte[] _buffer;

        public BufferWriter(byte[] buffer, bool prefixStr)
        {
            m_PrefixStrings = prefixStr;
            m_Encoding = Utility.UTF8;
            _buffer = buffer;
        }

        public BufferWriter(bool prefixStr)
        {
            m_PrefixStrings = prefixStr;
            m_Encoding = Utility.UTF8;
            _buffer = GC.AllocateUninitializedArray<byte>(BufferSize);
        }

        public BufferWriter(int count, bool prefixStr)
        {
            m_PrefixStrings = prefixStr;
            m_Encoding = Utility.UTF8;
            _buffer = GC.AllocateUninitializedArray<byte>(count);
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

            Array.Resize(ref _buffer, size);
        }

        public virtual void Flush()
        {
            // Need to avoid buffer.Length = 2, buffer * 2 is 4, but we need 8 or 16bytes, causing an exception.
            // The least we need is 16bytes + Index, but we use BufferSize since it should always be big enough for a single
            // non-dynamic field.
            Resize(Math.Max(BufferSize, _buffer.Length * 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool FlushIfNeeded(int amount)
        {
            if (Index + amount > _buffer.Length)
            {
                Flush();
                return true;
            }

            return false;
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

        public virtual long Seek(long offset, SeekOrigin origin)
        {
            return origin switch
            {
                SeekOrigin.Current => Index += offset,
                SeekOrigin.End     => Index = _buffer.Length - offset,
                _   => Index = offset // Begin
            };
        }

        public void WriteEncodedInt(int value)
        {
            var v = (uint)value;

            while (v >= 0x80)
            {
                Write((byte)(v | 0x80));
                v >>= 7;
            }

            Write((byte)v);
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

        public void Write(DateTime value)
        {
            var ticks = (value.Kind switch
            {
                DateTimeKind.Local       => value.ToUniversalTime(),
                DateTimeKind.Unspecified => value.ToLocalTime().ToUniversalTime(),
                _                        => value
            }).Ticks;

            Write(ticks);
        }

        public void WriteDeltaTime(DateTime value)
        {
            var ticks = (value.Kind switch
            {
                DateTimeKind.Local       => value.ToUniversalTime(),
                DateTimeKind.Unspecified => value.ToLocalTime().ToUniversalTime(),
                _                        => value
            }).Ticks;

            // Technically supports negative deltas for times in the past
            Write(ticks - DateTime.UtcNow.Ticks);
        }

        public void Write(IPAddress value)
        {
            Span<byte> stack = stackalloc byte[16];
            value.TryWriteBytes(stack, out var bytesWritten);
            Write((byte)bytesWritten);
            Write(stack.Slice(0, bytesWritten));
        }

        public void Write(TimeSpan value)
        {
            Write(value.Ticks);
        }

        public void Write(decimal value)
        {
            var bits = decimal.GetBits(value);

            for (var i = 0; i < 4; ++i)
            {
                Write(bits[i]);
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

        public void Write(Point3D value)
        {
            Write(value.m_X);
            Write(value.m_Y);
            Write(value.m_Z);
        }

        public void Write(Point2D value)
        {
            Write(value.m_X);
            Write(value.m_Y);
        }

        public void Write(Rectangle2D value)
        {
            Write(value.Start);
            Write(value.End);
        }

        public void Write(Rectangle3D value)
        {
            Write(value.Start);
            Write(value.End);
        }

        public void Write(Map value)
        {
            Write((byte)(value?.MapIndex ?? 0xFF));
        }

        public void Write(Race value)
        {
            Write((byte)(value?.RaceIndex ?? 0xFF));
        }

        public void Write(ISerializable value)
        {
            Write(value?.Deleted != false ? Serial.MinusOne : value.Serial);
        }

        public void Write<T>(ICollection<T> coll) where T : class, ISerializable
        {
            Write(coll.Count);
            foreach (var entry in coll)
            {
                Write(entry);
            }
        }

        public void Write<T>(ICollection<T> coll, Action<IGenericWriter, T> action) where T : class, ISerializable
        {
            if (coll == null)
            {
                Write(0);
                return;
            }

            Write(coll.Count);
            foreach (var entry in coll)
            {
                action(this, entry);
            }
        }

        internal void InternalWriteString(string value)
        {
            var remaining = m_Encoding.GetByteCount(value);

            WriteEncodedInt(remaining);

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

                Write(span.Slice(0, bytesWritten));
            }
        }
    }
}
