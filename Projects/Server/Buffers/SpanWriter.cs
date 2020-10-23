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

using System;
using System.Text;

namespace Server.Buffers
{
    /// <summary>
    ///   Provides functionality for writing primitive binary data.
    /// </summary>
    public ref struct SpanWriter
    {
        private int m_Position;

        public Span<byte> Buffer { get; }

        public Span<byte> Span => Buffer.Slice(0, WrittenCount);

        public int Length => Buffer.Length;

        public int WrittenCount { get; private set; }

        public int Position
        {
            get => m_Position;
            set
            {
                m_Position = value;

                if (value > WrittenCount)
                {
                    WrittenCount = value;
                }
            }
        }

        public SpanWriter(Span<byte> span)
        {
            Buffer = span;
            m_Position = 0;
            WrittenCount = 0;
        }

        public unsafe void Write(bool value)
        {
            Buffer[Position++] = *(byte*) & value;
        }

        public void Write(byte value)
        {
            Buffer[Position++] = value;
        }

        public void Write(sbyte value)
        {
            Buffer[Position++] = (byte)value;
        }

        public void Write(short value)
        {
            Write((byte)(value >> 8));
            Write((byte)value);
        }

        public void Write(ushort value)
        {
            Write((byte)(value >> 8));
            Write((byte)value);
        }

        public void Write(int value)
        {
            Write((byte)(value >> 24));
            Write((byte)(value >> 16));
            Write((byte)(value >> 8));
            Write((byte)value);
        }

        public void Write(uint value)
        {
            Write((byte)(value >> 24));
            Write((byte)(value >> 16));
            Write((byte)(value >> 8));
            Write((byte)value);
        }

        public void Write(long value)
        {
            Write((byte)(value >> 56));
            Write((byte)(value >> 48));
            Write((byte)(value >> 40));
            Write((byte)(value >> 32));

            Write((byte)(value >> 24));
            Write((byte)(value >> 16));
            Write((byte)(value >> 8));
            Write((byte)value);
        }

        public void Write(ulong value)
        {
            Write((byte)(value >> 56));
            Write((byte)(value >> 48));
            Write((byte)(value >> 40));
            Write((byte)(value >> 32));

            Write((byte)(value >> 24));
            Write((byte)(value >> 16));
            Write((byte)(value >> 8));
            Write((byte)value);
        }

        public void Write(ReadOnlySpan<byte> input)
        {
            int size = Math.Min(input.Length, Length - Position);

            input.Slice(0, size).CopyTo(Buffer.Slice(Position));
            Position += size;
        }

        public void WriteAscii(string value)
        {
            Position += Encoding.ASCII.GetBytes(value ?? "", Buffer.Slice(Position));
        }

        public void WriteAsciiFixed(string value, int size, bool zero = false)
        {
            value ??= "";

            int length = Math.Min(size, value.Length);

            Encoding.ASCII.GetBytes(value.AsSpan(0, length), Buffer.Slice(Position));

            if (zero)
            {
                Position += length;
                Fill(size - length);
            }
            else
            {
                Position += size;
            }
        }

        public void WriteAsciiNull(string value)
        {
            Position += Encoding.ASCII.GetBytes(value ?? "", Buffer.Slice(Position));
            Write((byte)0);
        }

        public void WriteAsciiNull(string value, int size)
        {
            value ??= "";

            size = Math.Min(size, value.Length);
            Position += Encoding.ASCII.GetBytes(value.AsSpan(0, size), Buffer.Slice(Position));
            Write((byte)0);
        }

        public void WriteLittleUni(string value)
        {
            Position += Encoding.Unicode.GetBytes(value ?? "", Buffer.Slice(Position));
        }

        public void WriteLittleUniNull(string value)
        {
            WriteLittleUni(value);
            Write((ushort)0);
        }

        public void WriteBigUni(string value)
        {
            Position += Encoding.BigEndianUnicode.GetBytes(value ?? "", Buffer.Slice(Position));
        }

        public void WriteBigUniNull(string value, bool zero = false)
        {
            WriteBigUni(value);

            if (zero)
            {
                Fill(2);
            }
            else
            {
                Position += 2;
            }
        }

        public void WriteUTF8Null(string value)
        {
            Position += Encoding.UTF8.GetBytes(value ?? "", Buffer.Slice(Position)) + 1;
        }

        public void CopyTo(Span<byte> destination)
        {
            Buffer.Slice(Position).CopyTo(destination);
        }

        public void Fill(int count)
        {
            count = Math.Min(count, Buffer.Length - Position);
            Buffer.Slice(Position, count).Clear();
            Position += count;
        }
    }
}
