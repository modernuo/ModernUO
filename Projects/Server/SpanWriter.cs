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

namespace Server.Network
{
  /// <summary>
  ///   Provides functionality for writing primitive binary data.
  /// </summary>
  public ref struct SpanWriter
  {
    /// <summary>
    ///   Internal format buffer.
    /// </summary>
    private Span<byte> m_Buffer;

    /// <summary>
    ///   Instantiates a new SpanWriter instance.
    /// </summary>
    public SpanWriter(Span<byte> buffer)
    {
      m_Buffer = buffer;
      Position = 0;
    }

    /// <summary>
    ///   Gets the total stream length.
    /// </summary>
    public int Length => m_Buffer.Length;

    /// <summary>
    ///   Gets or sets the current stream position.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    ///   Writes a 1-byte boolean value to the underlying stream. False is represented by 0, true by 1.
    /// </summary>
    public unsafe void Write(bool value)
    {
      Write(*(byte*)&value);
    }

    /// <summary>
    ///   Writes a 1-byte unsigned integer value to the underlying stream.
    /// </summary>
    public void Write(byte value)
    {
      m_Buffer[Position++] = value;
    }

    /// <summary>
    ///   Writes a 1-byte signed integer value to the underlying stream.
    /// </summary>
    public void Write(sbyte value)
    {
      Write((byte)value);
    }

    /// <summary>
    ///   Writes a 2-byte signed integer value to the underlying stream.
    /// </summary>
    public void Write(short value)
    {
      Write((byte)(value >> 8));
      Write((byte)value);
    }

    /// <summary>
    ///   Writes a 2-byte unsigned integer value to the underlying stream.
    /// </summary>
    public void Write(ushort value)
    {
      Write((byte)(value >> 8));
      Write((byte)value);
    }

    /// <summary>
    ///   Writes a 4-byte signed integer value to the underlying stream.
    /// </summary>
    public void Write(int value)
    {
      Write((byte)(value >> 24));
      Write((byte)(value >> 16));
      Write((byte)(value >> 8));
      Write((byte)value);
    }

    /// <summary>
    ///   Writes a 4-byte unsigned integer value to the underlying stream.
    /// </summary>
    public void Write(uint value)
    {
      Write((byte)(value >> 24));
      Write((byte)(value >> 16));
      Write((byte)(value >> 8));
      Write((byte)value);
    }

    /// <summary>
    ///   Writes a sequence of bytes to the underlying stream
    /// </summary>
    public void Write(byte[] buffer, int offset, int size)
    {
      if (size < Length - Position)
      {
        Console.WriteLine("Network: Attempted to Write buffer with not enough room");
        return;
      }

      buffer.AsSpan().Slice(0, offset).CopyTo(m_Buffer.Slice(Position, size));
      Position += size;
    }

    /// <summary>
    ///   Writes a sequence of bytes to the underlying stream
    /// </summary>
    public void Write(Span<byte> buffer)
    {
      int size = buffer.Length;

      if (size < Length - Position)
      {
        Console.WriteLine("Network: Attempted to Write buffer with not enough room");
        return;
      }

      buffer.CopyTo(m_Buffer.Slice(Position, size));
      Position += size;
    }

    /// <summary>
    ///   Writes a fixed-length ASCII-encoded string value to the underlying stream. To fit (size), the string content is either
    ///   truncated or padded with null characters.
    /// </summary>
    public void WriteAsciiFixed(string value, int size)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteAsciiFixed() with null value");
        value = string.Empty;
      }

      Encoding.ASCII.GetBytes(value, m_Buffer.Slice(Position, Math.Min(size, value.Length)));
      Position += size;
    }

    /// <summary>
    ///   Writes a dynamic-length ASCII-encoded string value to the underlying stream, followed by a 1-byte null character.
    /// </summary>
    public void WriteAsciiNull(string value)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteAsciiNull() with null value");
        value = string.Empty;
      }

      Encoding.ASCII.GetBytes(value, m_Buffer.Slice(Position, value.Length));
      Position += value.Length + 1;
    }

    /// <summary>
    ///   Writes a dynamic-length little-endian unicode string value to the underlying stream, followed by a 2-byte null character.
    /// </summary>
    public void WriteLittleUniNull(string value)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteLittleUniNull() with null value");
        value = string.Empty;
      }

      int length = value.Length * 2;

      Encoding.Unicode.GetBytes(value, m_Buffer.Slice(Position, length));
      Position += length + 2;
    }

    /// <summary>
    ///   Writes a fixed-length little-endian unicode string value to the underlying stream. To fit (size), the string content is
    ///   either truncated or padded with null characters.
    /// </summary>
    public void WriteLittleUniFixed(string value, int size)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteLittleUniFixed() with null value");
        value = string.Empty;
      }

      int length = value.Length * 2;
      size *= 2;

      Encoding.Unicode.GetBytes(value, m_Buffer.Slice(Position, Math.Min(size, length)));
      Position += size;
    }

    /// <summary>
    ///   Writes a dynamic-length big-endian unicode string value to the underlying stream, followed by a 2-byte null character.
    /// </summary>
    public void WriteBigUniNull(string value)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteBigUniNull() with null value");
        value = string.Empty;
      }

      int length = value.Length * 2;

      Encoding.BigEndianUnicode.GetBytes(value, m_Buffer.Slice(Position, length));
      Position += length + 2;
    }

    /// <summary>
    ///   Writes a fixed-length big-endian unicode string value to the underlying stream. To fit (size), the string content is
    ///   either truncated or padded with null characters.
    /// </summary>
    public void WriteBigUniFixed(string value, int size)
    {
      if (value == null)
      {
        Console.WriteLine("Network: Attempted to WriteBigUniFixed() with null value");
        value = string.Empty;
      }

      int length = value.Length * 2;
      size *= 2;

      Encoding.BigEndianUnicode.GetBytes(value, m_Buffer.Slice(Position, Math.Min(size, length)));
      Position += size;
    }
  }
}
