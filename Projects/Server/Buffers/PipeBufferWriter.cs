using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Buffers
{
  public ref struct PipeBufferWriter
  {
    public Span<byte> First;
    public Span<byte> Second;

    /// <summary>
    /// Gets or sets the current stream position.
    /// </summary>
    public int Position { get; private set; }

    /// <summary>
    /// Writes a 1-byte unsigned integer value to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value)
    {
      if (Position < First.Length)
        First[Position++] = value;
      else if (Position < Length)
      {
        Second[Position - First.Length] = value;
        Position++;
      }
      else
        throw new OutOfMemoryException();
    }

    /// <summary>
    /// Writes a 1-byte boolean value to the underlying stream. False is represented by 0, true by 1.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(bool value)
    {
      Write((byte)(value ? 1 : 0));
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
      if (Position < First.Length)
      {
        if (!BinaryPrimitives.TryWriteInt16BigEndian(First.Slice(Position), value))
        {
          // Not enough space. Split the spans
          Write((byte)(value >> 8));
          Write((byte)value);
        }
        else
          Position += 2;
      }
      else if (BinaryPrimitives.TryWriteInt16BigEndian(Second.Slice(Position - First.Length), value))
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
      if (Position < First.Length)
      {
        if (!BinaryPrimitives.TryWriteUInt16BigEndian(First.Slice(Position), value))
        {
          // Not enough space. Split the spans
          Write((byte)(value >> 8));
          Write((byte)value);
        }
        else
          Position += 2;
      }
      else if (BinaryPrimitives.TryWriteUInt16BigEndian(Second.Slice(Position - First.Length), value))
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
      if (Position < First.Length)
      {
        if (!BinaryPrimitives.TryWriteInt32BigEndian(First.Slice(Position), value))
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
      else if (BinaryPrimitives.TryWriteInt32BigEndian(Second.Slice(Position - First.Length), value))
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
      if (Position < First.Length)
      {
        if (!BinaryPrimitives.TryWriteUInt32BigEndian(First.Slice(Position), value))
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
      else if (BinaryPrimitives.TryWriteUInt32BigEndian(Second.Slice(Position - First.Length), value))
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
      if (Position < First.Length)
      {
        var sz = Math.Min(buffer.Length, First.Length - Position);
        buffer.CopyTo(First.Slice(Position));
        buffer.Slice(sz).CopyTo(Second);
        Position += buffer.Length;
      }
      else if (Position < Length)
      {
        buffer.CopyTo(Second.Slice(Position - First.Length));
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

      if (Position + size > Length)
        throw new OutOfMemoryException();

      int sz;

      if (Position < First.Length)
      {
        sz = Math.Min(Encoding.ASCII.GetByteCount(src), First.Length - Position);

        size -= Encoding.ASCII.GetBytes(src.Slice(0, sz), First.Slice(Position));
        if (size > 0)
        {
          Position += src.Length;
          First.Slice(Position, size).Clear();
          return;
        }

        Position += sz;
      }
      else
      {
        sz = 0;
      }

      size -= Encoding.ASCII.GetBytes(src.Slice(sz), Second.Slice(Position - First.Length));
      Position += src.Length - sz;
      Second.Slice(Position - First.Length, size).Clear();
    }

    /// <summary>
    /// Writes a dynamic-length ASCII-encoded string value to the underlying stream, followed by a 1-byte null character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteAsciiNull(string value)
    {
      value ??= string.Empty;

      var src = value.AsSpan();
      var byteCount = Encoding.ASCII.GetByteCount(src);

      if (Position + byteCount + 1 > Length)
        throw new OutOfMemoryException();

      if (Position < First.Length)
      {
        var sz = Math.Min(byteCount, First.Length - Position);

        Position += Encoding.ASCII.GetBytes(src.Slice(0, sz), First.Slice(Position));
        Position += Encoding.ASCII.GetBytes(src.Slice(sz), Second);
      }
      else
        Position += Encoding.ASCII.GetBytes(src, Second.Slice(Position - First.Length));

      Write((byte)0);
    }

    /// <summary>
    /// Writes a variable-length unicode string followed by a 2-byte null character.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void WriteVariableLengthUnicode(string value, Encoding encoding)
    {
      value ??= string.Empty;

      var src = value.AsSpan();
      var byteCount = Encoding.Unicode.GetByteCount(value);

      if (Position + byteCount + 2 > Length)
        throw new OutOfMemoryException();

      if (Position < First.Length)
      {
        if (Position + byteCount > First.Length)
        {
          var remaining = First.Length - Position;
          var sz = Math.DivRem(remaining, 2, out int offset);
          Position += encoding.GetBytes(src.Slice(0, sz), First.Slice(Position));

          if (offset == 0)
          {
            Span<byte> chr = stackalloc byte[2];
            encoding.GetBytes(src.Slice(sz++, 1), chr);
            First[Position] = chr[0];
            Second[0] = chr[1];
            Position += 2;
          }

          Position += encoding.GetBytes(src.Slice(sz), Second.Slice(offset));
        }
        else
        {
          Position += encoding.GetBytes(src, First.Slice(Position));
        }
      }
      else
        Position += encoding.GetBytes(src, Second.Slice(Position - First.Length));

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

      if (Position + size * 2 > Length)
        throw new OutOfMemoryException();

      int offset;
      int sz;

      if (Position < First.Length)
      {
        if (Position + byteCount > First.Length)
        {
          var remaining = First.Length - Position;
          sz = Math.DivRem(remaining, 2, out offset);
          Position += encoding.GetBytes(src.Slice(0, sz), First.Slice(Position));
          size -= sz;

          if (offset == 0)
          {
            Span<byte> chr = stackalloc byte[2];
            encoding.GetBytes(src.Slice(sz++, 1), chr);
            First[Position] = chr[0];
            Second[0] = chr[1];
            Position += 2;
            size--;
          }
        }
        else
        {
          Position += encoding.GetBytes(src, First.Slice(Position));
          size -= src.Length;

          if (size > 0)
          {
            Position += src.Length;
            First.Slice(Position, size).Clear();
          }

          return;
        }
      }
      else
      {
        offset = Position - First.Length;
        sz = 0;
      }

      size -= Encoding.ASCII.GetBytes(src.Slice(sz), Second.Slice(offset));
      Position += src.Length - sz;
      Second.Slice(Position - First.Length, size).Clear();
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
    /// Fills the stream from the current position up to (capacity) with 0x00's
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fill()
    {
      if (Position < First.Length)
      {
        First.Slice(Position).Fill(0);
        Second.Fill(0);
      }
      else
        Second.Slice(Position - First.Length).Fill(0);

      Position = Length;
    }

    /// <summary>
    /// Writes a number of 0x00 byte values to the underlying stream.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fill(int length)
    {
      if (Position < First.Length)
      {
        var sz = Math.Min(length, First.Length - Position);

        First.Slice(Position, sz).Fill(0);
        Second.Slice(0, length - sz).Fill(0);
      }
      else
        Second.Slice(Position - First.Length, length).Fill(0);

      Position += length;
    }

    /// <summary>
    /// Gets the total stream length.
    /// </summary>
    public int Length => First.Length + Second.Length;

    /// <summary>
    /// Offsets the current position from an origin.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Seek(int offset, SeekOrigin origin) =>
      Position = origin switch
      {
        SeekOrigin.Begin => offset,
        SeekOrigin.End => Length - offset,
        _ => Position + offset // Current
      };
  }
}
