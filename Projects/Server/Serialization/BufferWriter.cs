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
using Server.Guilds;

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
                // In case the length of the bytes is too long, and we loop in case doubling is not enough
                while (FlushIfNeeded(remaining))
                {
                }

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
                SeekOrigin.Begin   => Index = offset,
                SeekOrigin.Current => Index += offset,
                SeekOrigin.End     => Index = BufferSize - offset,
                _                  => Index
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
                    Write((byte)0);
                }
                else
                {
                    Write((byte)1);
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
            Write(value.Ticks);
        }

        public void Write(DateTimeOffset value)
        {
            Write(value.Ticks);
            Write(value.Offset.Ticks);
        }

        public void WriteDeltaTime(DateTime value)
        {
            var ticks = value.Ticks;
            var now = DateTime.UtcNow.Ticks;

            TimeSpan d;

            try
            {
                d = new TimeSpan(ticks - now);
            }
            catch
            {
                d = TimeSpan.MaxValue;
            }

            Write(d);
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
            _buffer[Index] = (byte)(value >> 8);
            _buffer[Index] = (byte)(value >> 16);
            _buffer[Index] = (byte)(value >> 24);
            _buffer[Index] = (byte)(value >> 32);
            _buffer[Index] = (byte)(value >> 40);
            _buffer[Index] = (byte)(value >> 48);
            _buffer[Index] = (byte)(value >> 56);
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

        public void Write(sbyte value)
        {
            FlushIfNeeded(1);
            _buffer[Index++] = (byte)value;
        }

        public void Write(bool value)
        {
            FlushIfNeeded(1);
            _buffer[Index++] = value ? 1 : 0;
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

        public void WriteEntity(IEntity value)
        {
            Write(value?.Deleted != false ? Serial.MinusOne : value.Serial);
        }

        public void Write(Item value)
        {
            Write(value?.Deleted != false ? Serial.MinusOne : value.Serial);
        }

        public void Write(Mobile value)
        {
            Write(value?.Deleted != false ? Serial.MinusOne : value.Serial);
        }

        public void Write(BaseGuild value)
        {
            Write(value?.Serial ?? 0);
        }

        public void WriteItem<T>(T value) where T : Item
        {
            Write(value);
        }

        public void WriteMobile<T>(T value) where T : Mobile
        {
            Write(value);
        }

        public void WriteGuild<T>(T value) where T : BaseGuild
        {
            Write(value);
        }

        public void Write(List<Item> list)
        {
            WriteItemList(list);
        }

        public void Write(List<Item> list, bool tidy)
        {
            WriteItemList(list, tidy);
        }

        public void WriteItemList<T>(List<T> list) where T : Item
        {
            WriteItemList(list, false);
        }

        public void WriteItemList<T>(List<T> list, bool tidy) where T : Item
        {
            if (tidy)
            {
                for (var i = 0; i < list.Count;)
                {
                    if (list[i].Deleted)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            Write(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                Write(list[i]);
            }
        }

        public void Write(HashSet<Item> set)
        {
            Write(set, false);
        }

        public void Write(HashSet<Item> set, bool tidy)
        {
            if (tidy)
            {
                set.RemoveWhere(item => item.Deleted);
            }

            Write(set.Count);

            foreach (var item in set)
            {
                Write(item);
            }
        }

        public void WriteItemSet<T>(HashSet<T> set) where T : Item
        {
            WriteItemSet(set, false);
        }

        public void WriteItemSet<T>(HashSet<T> set, bool tidy) where T : Item
        {
            if (tidy)
            {
                set.RemoveWhere(item => item.Deleted);
            }

            Write(set.Count);

            foreach (var item in set)
            {
                Write(item);
            }
        }

        public void Write(List<Mobile> list)
        {
            Write(list, false);
        }

        public void Write(List<Mobile> list, bool tidy)
        {
            if (tidy)
            {
                for (var i = 0; i < list.Count;)
                {
                    if (list[i].Deleted)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            Write(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                Write(list[i]);
            }
        }

        public void WriteMobileList<T>(List<T> list) where T : Mobile
        {
            WriteMobileList(list, false);
        }

        public void WriteMobileList<T>(List<T> list, bool tidy) where T : Mobile
        {
            if (tidy)
            {
                for (var i = 0; i < list.Count;)
                {
                    if (list[i].Deleted)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            Write(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                Write(list[i]);
            }
        }

        public void Write(HashSet<Mobile> set)
        {
            Write(set, false);
        }

        public void Write(HashSet<Mobile> set, bool tidy)
        {
            if (tidy)
            {
                set.RemoveWhere(mobile => mobile.Deleted);
            }

            Write(set.Count);

            foreach (var mob in set)
            {
                Write(mob);
            }
        }

        public void WriteMobileSet<T>(HashSet<T> set) where T : Mobile
        {
            WriteMobileSet(set, false);
        }

        public void WriteMobileSet<T>(HashSet<T> set, bool tidy) where T : Mobile
        {
            if (tidy)
            {
                set.RemoveWhere(mob => mob.Deleted);
            }

            Write(set.Count);

            foreach (var mob in set)
            {
                Write(mob);
            }
        }

        public void Write(List<BaseGuild> list)
        {
            Write(list, false);
        }

        public void Write(List<BaseGuild> list, bool tidy)
        {
            if (tidy)
            {
                for (var i = 0; i < list.Count;)
                {
                    if (list[i].Disbanded)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            Write(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                Write(list[i]);
            }
        }

        public void WriteGuildList<T>(List<T> list) where T : BaseGuild
        {
            WriteGuildList(list, false);
        }

        public void WriteGuildList<T>(List<T> list, bool tidy) where T : BaseGuild
        {
            if (tidy)
            {
                for (var i = 0; i < list.Count;)
                {
                    if (list[i].Disbanded)
                    {
                        list.RemoveAt(i);
                    }
                    else
                    {
                        ++i;
                    }
                }
            }

            Write(list.Count);

            for (var i = 0; i < list.Count; ++i)
            {
                Write(list[i]);
            }
        }

        public void Write(HashSet<BaseGuild> set)
        {
            Write(set, false);
        }

        public void Write(HashSet<BaseGuild> set, bool tidy)
        {
            if (tidy)
            {
                set.RemoveWhere(guild => guild.Disbanded);
            }

            Write(set.Count);

            foreach (var guild in set)
            {
                Write(guild);
            }
        }

        public void WriteGuildSet<T>(HashSet<T> set) where T : BaseGuild
        {
            WriteGuildSet(set, false);
        }

        public void WriteGuildSet<T>(HashSet<T> set, bool tidy) where T : BaseGuild
        {
            if (tidy)
            {
                set.RemoveWhere(guild => guild.Disbanded);
            }

            Write(set.Count);

            foreach (var guild in set)
            {
                Write(guild);
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
