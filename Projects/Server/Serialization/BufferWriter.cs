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
        private const int LargeByteBufferSize = 256;

        private readonly Encoding m_Encoding;
        private readonly bool m_PrefixStrings;

        protected long Index { get; set; }

        private byte[] m_CharacterBuffer;

        private int m_MaxBufferChars;
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
            _buffer = new byte[BufferSize];
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
            Array.Resize(ref _buffer, size);
        }

        public virtual void Flush()
        {
            Resize(Buffer.Length * 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FlushIfNeeded(int amount)
        {
            while (Index + amount > Buffer.Length)
            {
                Flush();
            }
        }

        public void Reset()
        {
            Index = 0;
        }

        public void Write(ReadOnlySpan<byte> bytes)
        {
            if (Index + bytes.Length > Buffer.Length)
            {
                Flush();
            }

            bytes.CopyTo(Buffer.AsSpan((int)Index));
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

            Buffer[Index] = (byte)value;
            Buffer[Index + 1] = (byte)(value >> 8);
            Buffer[Index + 2] = (byte)(value >> 16);
            Buffer[Index + 3] = (byte)(value >> 24);
            Buffer[Index + 4] = (byte)(value >> 32);
            Buffer[Index + 5] = (byte)(value >> 40);
            Buffer[Index + 6] = (byte)(value >> 48);
            Buffer[Index + 7] = (byte)(value >> 56);
            Index += 8;
        }

        public void Write(ulong value)
        {
            FlushIfNeeded(8);

            Buffer[Index] = (byte)value;
            Buffer[Index + 1] = (byte)(value >> 8);
            Buffer[Index + 2] = (byte)(value >> 16);
            Buffer[Index + 3] = (byte)(value >> 24);
            Buffer[Index + 4] = (byte)(value >> 32);
            Buffer[Index + 5] = (byte)(value >> 40);
            Buffer[Index + 6] = (byte)(value >> 48);
            Buffer[Index + 7] = (byte)(value >> 56);
            Index += 8;
        }

        public void Write(int value)
        {
            FlushIfNeeded(4);

            Buffer[Index] = (byte)value;
            Buffer[Index + 1] = (byte)(value >> 8);
            Buffer[Index + 2] = (byte)(value >> 16);
            Buffer[Index + 3] = (byte)(value >> 24);
            Index += 4;
        }

        public void Write(uint value)
        {
            FlushIfNeeded(4);

            Buffer[Index] = (byte)value;
            Buffer[Index + 1] = (byte)(value >> 8);
            Buffer[Index + 2] = (byte)(value >> 16);
            Buffer[Index + 3] = (byte)(value >> 24);
            Index += 4;
        }

        public void Write(short value)
        {
            FlushIfNeeded(4);

            Buffer[Index] = (byte)value;
            Buffer[Index + 1] = (byte)(value >> 8);
            Index += 2;
        }

        public void Write(ushort value)
        {
            FlushIfNeeded(2);

            Buffer[Index] = (byte)value;
            Buffer[Index + 1] = (byte)(value >> 8);
            Index += 2;
        }

        public unsafe void Write(double value)
        {
            FlushIfNeeded(8);

            fixed (byte* pBuffer = Buffer)
            {
                *(double*)(pBuffer + Index) = value;
            }

            Index += 8;
        }

        public unsafe void Write(float value)
        {
            FlushIfNeeded(4);

            fixed (byte* pBuffer = Buffer)
            {
                *(float*)(pBuffer + Index) = value;
            }

            Index += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte value)
        {
            FlushIfNeeded(1);
            Buffer[Index++] = value;
        }

        public void Write(byte[] value, int length)
        {
            var remaining = length;
            var idx = 0;

            while (remaining > 0)
            {
                int size = Math.Min(Buffer.Length - (int)Index, remaining);
                System.Buffer.BlockCopy(value, idx, Buffer, (int)Index, size);
                // value.Slice(idx).CopyTo(m_Buffer.AsSpan(m_Index, size));

                remaining -= size;
                Index += size;
                idx += size;

                FlushIfNeeded(0);
            }
        }

        public void Write(sbyte value)
        {
            FlushIfNeeded(1);
            Buffer[Index++] = (byte)value;
        }

        public void Write(bool value)
        {
            FlushIfNeeded(1);
            Buffer[Index++] = value ? 1 : 0;
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
            var length = m_Encoding.GetByteCount(value);

            WriteEncodedInt(length);

            if (m_CharacterBuffer == null)
            {
                m_CharacterBuffer = new byte[LargeByteBufferSize];
                m_MaxBufferChars = LargeByteBufferSize / m_Encoding.GetMaxByteCount(1);
            }

            if (length > LargeByteBufferSize)
            {
                var current = 0;
                var charsLeft = value.Length;

                while (charsLeft > 0)
                {
                    var charCount = Math.Min(charsLeft, m_MaxBufferChars);
                    var byteLength = m_Encoding.GetBytes(value, current, charCount, m_CharacterBuffer, 0);

                    FlushIfNeeded(byteLength);

                    System.Buffer.BlockCopy(m_CharacterBuffer, 0, Buffer, (int)Index, byteLength);
                    Index += byteLength;

                    current += charCount;
                    charsLeft -= charCount;
                }
            }
            else
            {
                var byteLength = m_Encoding.GetBytes(value, 0, value.Length, m_CharacterBuffer, 0);
                FlushIfNeeded(byteLength);

                System.Buffer.BlockCopy(m_CharacterBuffer, 0, Buffer, (int)Index, byteLength);
                Index += byteLength;
            }
        }
    }
}
