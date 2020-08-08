/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: BufferWriter.cs                                                 *
 * Created: 2020/08/05 - Updated: 2020/08/07                             *
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

using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Buffers
{
  public ref struct BufferWriter
  {
    private readonly Span<byte> m_First;
    private readonly Span<byte> m_Second;
    private readonly int m_Length;

    public BufferWriter(Span<byte> first, Span<byte> second)
    {
      m_First = first;
      m_Second = second;
      Position = 0;
      m_Length = first.Length + second.Length;
    }

    /// <summary>
    /// Gets or sets the current stream m_Position.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Writes a 1-byte unsigned integer value to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value)
    {
      if (Position < m_First.Length)
        m_First[Position++] = value;
      else if (Position < m_Length)
        m_Second[Position++ - m_First.Length] = value;
      else
        throw new OutOfMemoryException();
    }

    /// <summary>
    /// Writes a 1-byte boolean value to the underlying stream. False is represented by 0, true by 1.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(bool value)
    {
      if (value)
        Write((byte)1);
      else
        Write((byte)0);
    }

    /// <summary>
    /// Writes a 1-byte signed integer value to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(sbyte value)
    {
      Write((byte)value);
    }

    /// <summary>
    /// Writes a 2-byte signed integer value to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(short value)
    {
      if (Position < m_First.Length)
      {
        if (!BinaryPrimitives.TryWriteInt16BigEndian(m_First.Slice(Position), value))
        {
          // Not enough space. Split the spans
          Write((byte)(value >> 8));
          Write((byte)value);
        }
        else
          Position += 2;
      }
      else if (BinaryPrimitives.TryWriteInt16BigEndian(m_Second.Slice(Position - m_First.Length), value))
        Position += 2;
      else
        throw new OutOfMemoryException();
    }

    /// <summary>
    /// Writes a 2-byte unsigned integer value to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ushort value)
    {
      if (Position < m_First.Length)
      {
        if (!BinaryPrimitives.TryWriteUInt16BigEndian(m_First.Slice(Position), value))
        {
          // Not enough space. Split the spans
          Write((byte)(value >> 8));
          Write((byte)value);
        }
        else
          Position += 2;
      }
      else if (BinaryPrimitives.TryWriteUInt16BigEndian(m_Second.Slice(Position - m_First.Length), value))
        Position += 2;
      else
        throw new OutOfMemoryException();
    }

    /// <summary>
    /// Writes a 4-byte signed integer value to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(int value)
    {
      if (Position < m_First.Length)
      {
        if (!BinaryPrimitives.TryWriteInt32BigEndian(m_First.Slice(Position), value))
        {
          // Not enough space. Split the spans
          Write((byte)(value >> 24));
          Write((byte)(value >> 16));
          Write((byte)(value >> 8));
          Write((byte)value);
        }
        else
          Position += 4;
      }
      else if (BinaryPrimitives.TryWriteInt32BigEndian(m_Second.Slice(Position - m_First.Length), value))
        Position += 4;
      else
        throw new OutOfMemoryException();
    }

    /// <summary>
    /// Writes a 4-byte unsigned integer value to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(uint value)
    {
      if (Position < m_First.Length)
      {
        if (!BinaryPrimitives.TryWriteUInt32BigEndian(m_First.Slice(Position), value))
        {
          // Not enough space. Split the spans
          Write((byte)(value >> 24));
          Write((byte)(value >> 16));
          Write((byte)(value >> 8));
          Write((byte)value);
        }
        else
          Position += 4;
      }
      else if (BinaryPrimitives.TryWriteUInt32BigEndian(m_Second.Slice(Position - m_First.Length), value))
        Position += 4;
      else
        throw new OutOfMemoryException();
    }

    /// <summary>
    /// Writes a sequence of bytes to the underlying stream
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Span<byte> buffer)
    {
      if (Position < m_First.Length)
      {
        var sz = Math.Min(buffer.Length, m_First.Length - Position);
        buffer.CopyTo(m_First.Slice(Position));
        buffer.Slice(sz).CopyTo(m_Second);
        Position += buffer.Length;
      }
      else if (Position < m_Length)
      {
        buffer.CopyTo(m_Second.Slice(Position - m_First.Length));
      }
      else
        throw new OutOfMemoryException();
    }

    /// <summary>
    /// Writes a fixed-length ASCII-encoded string value to the underlying stream. To fit (size), the string content is either truncated or padded with null characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAsciiFixed(string value, int size)
    {
      value ??= string.Empty;

      var src = value.AsSpan(0, Math.Min(size, value.Length));

      if (Position + size > m_Length)
        throw new OutOfMemoryException();

      int sz;

      if (Position < m_First.Length)
      {
        sz = Math.Min(Encoding.ASCII.GetByteCount(src), m_First.Length - Position);

        size -= Encoding.ASCII.GetBytes(src.Slice(0, sz), m_First.Slice(Position));
        if (size > 0)
        {
          Position += src.Length;
          m_First.Slice(Position, size).Clear();
          return;
        }

        Position += sz;
      }
      else
      {
        sz = 0;
      }

      size -= Encoding.ASCII.GetBytes(src.Slice(sz), m_Second.Slice(Position - m_First.Length));
      Position += src.Length - sz;
      m_Second.Slice(Position - m_First.Length, size).Clear();
    }

    /// <summary>
    /// Writes a dynamic-Length ASCII-encoded string value to the underlying stream, followed by a 1-byte null character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAsciiNull(string value)
    {
      value ??= string.Empty;

      var src = value.AsSpan();
      var byteCount = Encoding.ASCII.GetByteCount(src);

      if (Position + byteCount + 1 > m_Length)
        throw new OutOfMemoryException();

      if (Position < m_First.Length)
      {
        var sz = Math.Min(byteCount, m_First.Length - Position);

        Position += Encoding.ASCII.GetBytes(src.Slice(0, sz), m_First.Slice(Position));
        Position += Encoding.ASCII.GetBytes(src.Slice(sz), m_Second);
      }
      else
        Position += Encoding.ASCII.GetBytes(src, m_Second.Slice(Position - m_First.Length));

      Write((byte)0);
    }

    /// <summary>
    /// Writes a variable-length unicode string followed by a 2-byte null character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVariableLengthUnicode(string value, Encoding encoding)
    {
      value ??= string.Empty;

      var src = value.AsSpan();
      var byteCount = Encoding.Unicode.GetByteCount(value);

      if (Position + byteCount + 2 > m_Length)
        throw new OutOfMemoryException();

      if (Position < m_First.Length)
      {
        if (Position + byteCount > m_First.Length)
        {
          var remaining = m_First.Length - Position;
          var sz = Math.DivRem(remaining, 2, out int offset);
          Position += encoding.GetBytes(src.Slice(0, sz), m_First.Slice(Position));

          if (offset == 0)
          {
            Span<byte> chr = stackalloc byte[2];
            encoding.GetBytes(src.Slice(sz++, 1), chr);
            m_First[Position] = chr[0];
            m_Second[0] = chr[1];
            Position += 2;
          }

          Position += encoding.GetBytes(src.Slice(sz), m_Second.Slice(offset));
        }
        else
        {
          Position += encoding.GetBytes(src, m_First.Slice(Position));
        }
      }
      else
        Position += encoding.GetBytes(src, m_Second.Slice(Position - m_First.Length));

      Write((ushort)0);
    }

    /// <summary>
    /// Writes a fixed-length unicode string value. To fit (size), the string content is either truncated or padded with null characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteFixedLengthUnicode(string value, int size, Encoding encoding)
    {
      value ??= string.Empty;

      var src = value.AsSpan(0, Math.Min(size, value.Length));

      var byteCount = encoding.GetByteCount(value);

      if (Position + size * 2 > m_Length)
        throw new OutOfMemoryException();

      int offset;
      int sz;

      if (Position < m_First.Length)
      {
        if (Position + byteCount > m_First.Length)
        {
          var remaining = m_First.Length - Position;
          sz = Math.DivRem(remaining, 2, out offset);
          Position += encoding.GetBytes(src.Slice(0, sz), m_First.Slice(Position));
          size -= sz;

          if (offset == 0)
          {
            Span<byte> chr = stackalloc byte[2];
            encoding.GetBytes(src.Slice(sz++, 1), chr);
            m_First[Position] = chr[0];
            m_Second[0] = chr[1];
            Position += 2;
            size--;
          }
        }
        else
        {
          Position += encoding.GetBytes(src, m_First.Slice(Position));
          size -= src.Length;

          if (size > 0)
          {
            Position += src.Length;
            m_First.Slice(Position, size).Clear();
          }

          return;
        }
      }
      else
      {
        offset = Position - m_First.Length;
        sz = 0;
      }

      size -= Encoding.ASCII.GetBytes(src.Slice(sz), m_Second.Slice(offset));
      Position += src.Length - sz;
      m_Second.Slice(Position - m_First.Length, size).Clear();
    }

    /// <summary>
    /// Writes a dynamic-length little-endian unicode string value to the underlying stream, followed by a 2-byte null character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLittleUniNull(string value)
    {
      WriteVariableLengthUnicode(value, Encoding.Unicode);
    }

    /// <summary>
    /// Writes a fixed-length little-endian unicode string value to the underlying stream. To fit (size), the string content is either truncated or padded with null characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteLittleUniFixed(string value, int size)
    {
      WriteFixedLengthUnicode(value, size, Encoding.Unicode);
    }

    /// <summary>
    /// Writes a dynamic-length big-endian unicode string value to the underlying stream, followed by a 2-byte null character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBigUniNull(string value)
    {
      WriteVariableLengthUnicode(value, Encoding.BigEndianUnicode);
    }

    /// <summary>
    /// Writes a fixed-length big-endian unicode string value to the underlying stream. To fit (size), the string content is either truncated or padded with null characters.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBigUniFixed(string value, int size)
    {
      WriteFixedLengthUnicode(value, size, Encoding.BigEndianUnicode);
    }

    /// <summary>
    /// Fills the stream from the current m_Position up to (capacity) with 0x00's
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fill()
    {
      if (Position < m_First.Length)
      {
        m_First.Slice(Position).Fill(0);
        m_Second.Fill(0);
      }
      else
        m_Second.Slice(Position - m_First.Length).Fill(0);

      Position = m_Length;
    }

    /// <summary>
    /// Writes a number of 0x00 byte values to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fill(int amount)
    {
      if (Position < m_First.Length)
      {
        var sz = Math.Min(amount, m_First.Length - Position);

        m_First.Slice(Position, sz).Fill(0);
        m_Second.Slice(0, m_Length - sz).Fill(0);
      }
      else
        m_Second.Slice(Position - m_First.Length, amount).Fill(0);

      Position += amount;
    }

    /// <summary>
    /// Offsets the current m_Position from an origin.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Seek(int offset, SeekOrigin origin) =>
      Position = origin switch
      {
        SeekOrigin.Begin => offset,
        SeekOrigin.End => m_Length - offset,
        _ => Position + offset // Current
      };
  }
}
