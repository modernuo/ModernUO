/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: SpanWriter.cs - Created: 2019/08/05 - Updated: 2019/12/24       *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
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

    /// <summary>
    ///   Underlying Span.
    /// </summary>
    public Span<byte> RawSpan { get; }

    /// <summary>
    ///   Underlying Span up to the bytes written.
    /// </summary>
    public Span<byte> Span => RawSpan.Slice(0, WrittenCount);

    /// <summary>
    ///   Gets the total length of the span.
    /// </summary>
    public int Length => RawSpan.Length;

    /// <summary>
    ///   Total bytes written to the span.
    /// </summary>
    public int WrittenCount { get; private set; }

    /// <summary>
    ///   Current position in the the span.
    /// </summary>
    public int Position
    {
      get => m_Position;
      set
      {
        m_Position = value;

        if (value > WrittenCount)
          WrittenCount = value;
      }
    }

    /// <summary>
    ///   Instantiates a new SpanWriter instance.
    /// </summary>
    public SpanWriter(Span<byte> span)
    {
      RawSpan = span;
      m_Position = 0;
      WrittenCount = 0;
    }

    /// <summary>
    ///   Writes a 1-byte boolean value to the span.
    /// </summary>
    public unsafe void Write(bool value)
    {
      RawSpan[Position++] = *(byte*)&value;
    }

    /// <summary>
    ///   Writes a 1-byte unsigned integer value to the span.
    /// </summary>
    public void Write(byte value)
    {
      RawSpan[Position++] = value;
    }

    /// <summary>
    ///   Writes a 1-byte signed integer value to the span.
    /// </summary>
    public void Write(sbyte value)
    {
      RawSpan[Position++] = (byte)value;
    }

    /// <summary>
    ///   Writes a 2-byte signed integer value to the span.
    /// </summary>
    public void Write(short value)
    {
      Write((byte)(value >> 8));
      Write((byte)value);
    }

    /// <summary>
    ///   Writes a 2-byte unsigned integer value to the span.
    /// </summary>
    public void Write(ushort value)
    {
      Write((byte)(value >> 8));
      Write((byte)value);
    }

    /// <summary>
    ///   Writes a 4-byte signed integer value to the span.
    /// </summary>
    public void Write(int value)
    {
      Write((byte)(value >> 24));
      Write((byte)(value >> 16));
      Write((byte)(value >> 8));
      Write((byte)value);
    }

    /// <summary>
    ///   Writes a 4-byte unsigned integer value to the span.
    /// </summary>
    public void Write(uint value)
    {
      Write((byte)(value >> 24));
      Write((byte)(value >> 16));
      Write((byte)(value >> 8));
      Write((byte)value);
    }

    /// <summary>
    ///   Writes an 8-byte signed integer value to the span.
    /// </summary>
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

    /// <summary>
    ///   Writes an 8-byte unsigned integer value to the span.
    /// </summary>
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

    /// <summary>
    ///   Writes a sequence of bytes to the span.
    /// </summary>
    public void Write(ReadOnlySpan<byte> input)
    {
      var size = Math.Min(input.Length, Length - Position);

      input.Slice(0, size).CopyTo(RawSpan.Slice(Position));
      Position += size;
    }

    /// <summary>
    ///   Writes an ASCII-encoded string value to the span.
    /// </summary>
    public void WriteAscii(string value)
    {
      Position += Encoding.ASCII.GetBytes(value ?? "", RawSpan.Slice(Position));
    }

    /// <summary>
    ///   Writes a fixed-length ASCII-encoded string value to the span.
    /// </summary>
    public void WriteAsciiFixed(string value, int size, bool zero = false)
    {
      value ??= "";

      var length = Math.Min(size, value.Length);

      Encoding.ASCII.GetBytes(value.AsSpan(0, length), RawSpan.Slice(Position));

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

    /// <summary>
    ///   Writes a dynamic-length ASCII-encoded string value to the span, followed by a 1-byte null character.
    /// </summary>
    public void WriteAsciiNull(string value)
    {
      Position += Encoding.ASCII.GetBytes(value ?? "", RawSpan.Slice(Position));
      Write((byte)0);
    }

    /// <summary>
    ///   Writes a dynamic-length ASCII-encoded string value to the span, followed by a 1-byte null character.
    /// </summary>
    public void WriteAsciiNull(string value, int size)
    {
      value ??= "";

      size = Math.Min(size, value.Length);
      Position += Encoding.ASCII.GetBytes(value.AsSpan(0, size), RawSpan.Slice(Position));
      Write((byte)0);
    }

    /// <summary>
    ///   Writes a dynamic-length little-endian unicode string value to the span.
    /// </summary>
    public void WriteLittleUni(string value)
    {
      Position += Encoding.Unicode.GetBytes(value ?? "", RawSpan.Slice(Position));
    }

    /// <summary>
    ///   Writes a dynamic-length little-endian unicode string value to the span, followed by a 2-byte null character.
    /// </summary>
    public void WriteLittleUniNull(string value)
    {
      WriteLittleUni(value);
      Write((ushort)0);
    }

    /// <summary>
    ///   Writes a dynamic-length big-endian unicode string value to the span.
    /// </summary>
    public void WriteBigUni(string value)
    {
      Position += Encoding.BigEndianUnicode.GetBytes(value ?? "", RawSpan.Slice(Position));
    }

    /// <summary>
    ///   Writes a dynamic-length big-endian unicode string value to the span, followed by a 2-byte null character.
    /// </summary>
    public void WriteBigUniNull(string value, bool zero = false)
    {
      WriteBigUni(value);

      if (zero)
        Fill(2);
      else
        Position += 2;
    }

    /// <summary>
    ///   Writes a dynamic-length utf-8 string value, followed by a 1-byte null character.
    /// </summary>
    public void WriteUTF8Null(string value)
    {
      Position += Encoding.UTF8.GetBytes(value ?? "", RawSpan.Slice(Position)) + 1;
    }

    /// <summary>
    ///   Copies the span to the destination.
    /// </summary>
    public void CopyTo(Span<byte> destination)
    {
      RawSpan.CopyTo(destination);
    }

    /// <summary>
    ///   Fills the buffer with zeroes up to count
    /// </summary>
    public void Fill(int count)
    {
      count = Math.Min(count, RawSpan.Length - Position);
      RawSpan.Slice(Position, count).Clear();
      Position += count;
    }
  }
}
