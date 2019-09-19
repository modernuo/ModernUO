/***************************************************************************
 *                               SpanWriter.cs
 *                            -------------------
 *   begin                : August 5, 2019
 *   copyright            : (C) The ModernUO Team
 *   email                : hi@modernuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

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
    public int Length => Span.Length;

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
      Span[Position++] = *(byte*)&value;
    }

    /// <summary>
    ///   Writes a 1-byte unsigned integer value to the span.
    /// </summary>
    public void Write(byte value)
    {
      Span[Position++] = value;
    }

    /// <summary>
    ///   Writes a 1-byte signed integer value to the span.
    /// </summary>
    public void Write(sbyte value)
    {
      Span[Position++] = (byte)value;
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
      int size = Math.Min(input.Length, Length - Position);

      input.CopyTo(RawSpan.Slice(Position));
      Position += size;
    }

    /// <summary>
    ///   Writes an ASCII-encoded string value to the span.
    /// </summary>
    public void WriteAscii(string value)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteAsciiFixed() with null value");
        value = string.Empty;
      }

      Position += Encoding.ASCII.GetBytes(value, RawSpan.Slice(Position));
    }

    /// <summary>
    ///   Writes a fixed-length ASCII-encoded string value to the span.
    /// </summary>
    public void WriteAsciiFixed(string value, int size)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteAsciiFixed() with null value");
        value = string.Empty;
      }

      size = Math.Min(size, value.Length);

      Position += Encoding.ASCII.GetBytes(value.AsSpan(0, size), RawSpan.Slice(Position));
    }

    /// <summary>
    ///   Writes a dynamic-length ASCII-encoded string value to the span, followed by a 1-byte null character.
    /// </summary>
    public void WriteAsciiNull(string value)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteAsciiNull() with null value");
        value = string.Empty;
      }

      Position += Encoding.ASCII.GetBytes(value, Span.Slice(Position)) + 1;
    }

    /// <summary>
    ///   Writes a dynamic-length ASCII-encoded string value to the span, followed by a 1-byte null character.
    /// </summary>
    public void WriteAsciiNull(string value, int size)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteAsciiNull() with null value");
        value = string.Empty;
      }

      size = Math.Min(size, value.Length);
      Position += Encoding.ASCII.GetBytes(value.AsSpan(0, size), Span.Slice(Position)) + 1;
    }

    /// <summary>
    ///   Writes a dynamic-length little-endian unicode string value to the span, followed by a 2-byte null character.
    /// </summary>
    public void WriteLittleUniNull(string value)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteLittleUniNull() with null value");
        value = string.Empty;
      }

      Position += Encoding.Unicode.GetBytes(value, RawSpan.Slice(Position)) + 2;
    }

    /// <summary>
    ///   Writes a fixed-length little-endian unicode string value to the span. To fit (size), the string content is
    ///   either truncated or padded with null characters.
    /// </summary>
    public void WriteLittleUniFixed(string value, int size)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteLittleUniFixed() with null value");
        value = string.Empty;
      }

      size = Math.Min(size, value.Length);

      Position += Encoding.Unicode.GetBytes(value.AsSpan(0, size), RawSpan.Slice(Position));
    }

    /// <summary>
    ///   Writes a dynamic-length big-endian unicode string value to the span, followed by a 2-byte null character.
    /// </summary>
    public void WriteBigUniNull(string value)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteBigUniNull() with null value");
        value = string.Empty;
      }

      Position += Encoding.BigEndianUnicode.GetBytes(value, RawSpan.Slice(Position)) + 2;
    }

    /// <summary>
    ///   Writes a fixed-length big-endian unicode string value to the span.
    /// </summary>
    public void WriteBigUniFixed(string value, int size)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteBigUniFixed() with null value");
        value = string.Empty;
      }

      size = Math.Min(size, value.Length);

      Position += Encoding.BigEndianUnicode.GetBytes(value.AsSpan(0, size), RawSpan.Slice(Position));
    }

    /// <summary>
    ///   Writes a dynamic-length utf-8 string values.
    /// </summary>
    public void WriteUTF8(string value)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteUTF8Null() with null value");
        value = string.Empty;
      }

      Position += Utility.UTF8.GetBytes(value, RawSpan.Slice(Position));
    }

    /// <summary>
    ///   Writes a dynamic-length utf-8 string value, followed by a 1-byte null character.
    /// </summary>
    public void WriteUTF8Null(string value)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteUTF8Null() with null value");
        value = string.Empty;
      }

      Position += Utility.UTF8.GetBytes(value, RawSpan.Slice(Position)) + 1;
    }

    /// <summary>
    ///   Writes a dynamic-length utf-8 string value up to max characters to the span, followed by a 1-byte null character.
    /// </summary>
    public void WriteUTF8Null(string value, int size)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteUTF8Null() with null value");
        value = string.Empty;
      }

      size = Math.Min(size, value.Length);

      Position += Utility.UTF8.GetBytes(value.AsSpan(0, size), RawSpan.Slice(Position)) + 1;
    }

    /// <summary>
    ///   Copies the span to the destination.
    /// </summary>
    public void CopyTo(Span<byte> destination)
    {
      RawSpan.CopyTo(destination);
    }

    /// <summary>
    ///   Copies the span to the destination up to count or bytes written.
    /// </summary>
    public void CopyTo(Span<byte> destination, int count)
    {
      if (count > WrittenCount)
        CopyTo(destination);
      else
        RawSpan.Slice(0, count).CopyTo(destination);
    }
  }
}
