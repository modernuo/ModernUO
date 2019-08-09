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
using System.IO;
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
    ///   Gets or sets the current stream capacity.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    ///   Instantiates a new PacketWriter instance.
    /// </summary>
    /// <param name="buffer">Internal buffer used for writing</param>
    public SpanWriter(Span<byte> buffer, int cap)
    {
      m_Buffer = buffer;
      Capacity = cap;
      Position = 0;
    }

    /// <summary>
    ///   Gets the total stream length.
    /// </summary>
    public long Length => m_Buffer.Length;

    /// <summary>
    ///   Gets or sets the current stream position.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    ///   Writes a 1-byte boolean value to the underlying stream. False is represented by 0, true by 1.
    /// </summary>
    public void Write(bool value)
    {
      Write((byte)(value ? 0 : 1));
    }

    /// <summary>
    ///   Writes a 1-byte boolean value to the underlying stream. False is represented by 0, true by 1.
    ///   Optimizes by skipping the write entirely if the value is false.
    ///   Do not use this if the data is not initialized to 0.
    /// </summary>
    public void WriteIfTrue(bool value)
    {
      if (value)
        m_Buffer[Position] = 1;

      Position++;
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

      // TODO: Performance sucks
      // https://github.com/dotnet/corefx/issues/30382
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

      // TODO: Performance sucks
      // https://github.com/dotnet/corefx/issues/30382
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

      // TODO: Performance sucks
      // https://github.com/dotnet/corefx/issues/30382
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

      // TODO: Performance sucks
      // https://github.com/dotnet/corefx/issues/30382
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

      // TODO: Performance sucks
      // https://github.com/dotnet/corefx/issues/30382
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

      // TODO: Performance sucks
      // https://github.com/dotnet/corefx/issues/30382
      Encoding.BigEndianUnicode.GetBytes(value, m_Buffer.Slice(Position, Math.Min(size, length)));
      Position += size;
    }

    /// <summary>
    ///   Fills the stream from the current position up to (capacity) with 0x00's
    /// </summary>
    public void Fill() => m_Buffer.Slice(Position, Capacity - Position).Fill(0);

    /// <summary>
    ///   Writes a number of 0x00 byte values to the underlying stream.
    /// </summary>
    public void Fill(int length) => m_Buffer.Slice(Position, Math.Min(length, Capacity - Position)).Fill(0);

    /// <summary>
    ///   Offsets the current position from an origin.
    /// </summary>
    public int Seek(int offset, SeekOrigin origin = SeekOrigin.Begin)
    {
      switch (origin)
      {
        default: Position = Math.Max(offset, 0); break;
        case SeekOrigin.Current:
          {
            Position += offset;
            if (Position < 0)
              Position = 0;
            else if (Position >= Capacity)
              Position = Capacity - 1;

              break;
          }
        case SeekOrigin.End:
          {
            Position -= Math.Max(0, offset);
            if (Position < 0)
              Position = 0;
            break;
          }
      }

      return Position;
    }
  }
}
