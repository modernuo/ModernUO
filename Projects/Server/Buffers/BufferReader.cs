/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2020 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: BufferReader.cs                                                 *
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

using System;
using System.Buffers.Binary;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Server.Network
{
  public ref struct BufferReader
  {
    private readonly Span<byte> m_First;
    private readonly Span<byte> m_Second;

    public int Length { get; }
    public int Position { get; private set; }
    public int Remaining => Length - Position;

    public BufferReader(ArraySegment<byte>[] buffers)
    {
      m_First = buffers[0];
      m_Second = buffers[1];
      Position = 0;
      Length = m_First.Length + m_Second.Length;
    }

    public BufferReader(Span<byte> first, Span<byte> second)
    {
      m_First = first;
      m_Second = second;
      Position = 0;
      Length = first.Length + second.Length;
    }

    public void Trace(NetState state)
    {
      // We don't have data, so nothing to trace
      if (m_First.Length == 0) return;

      try
      {
        using var sw = new StreamWriter("Packets.log", true);

        sw.WriteLine("Client: {0}: Unhandled packet 0x{1:X2}", state, m_First[0]);

        Utility.FormatBuffer(sw, m_First.ToArray(), new Memory<byte>(m_Second.ToArray()));

        sw.WriteLine();
        sw.WriteLine();
      }
      catch
      {
        // ignored
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
      if (Position < m_First.Length)
        return m_First[Position++];
      if (Position < Length)
        return m_Second[Position++ - m_First.Length];

      throw new OutOfMemoryException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBoolean() => ReadByte() > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)ReadByte();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16()
    {
      short value;

      if (Position < m_First.Length)
      {
        if (!BinaryPrimitives.TryReadInt16BigEndian(m_First.Slice(Position), out value))
          // Not enough space. Split the spans
          return (short)((ReadByte() >> 8) | ReadByte());

        Position += 2;
      }
      else if (BinaryPrimitives.TryReadInt16BigEndian(m_Second.Slice(Position - m_First.Length), out value))
        Position += 2;
      else
        throw new OutOfMemoryException();

      return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16()
    {
      ushort value;

      if (Position < m_First.Length)
      {
        if (!BinaryPrimitives.TryReadUInt16BigEndian(m_First.Slice(Position), out value))
          // Not enough space. Split the spans
          return (ushort)((ReadByte() >> 8) | ReadByte());

        Position += 2;
      }
      else if (BinaryPrimitives.TryReadUInt16BigEndian(m_Second.Slice(Position - m_First.Length), out value))
        Position += 2;
      else
        throw new OutOfMemoryException();

      return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32()
    {
      int value;

      if (Position < m_First.Length)
      {
        if (!BinaryPrimitives.TryReadInt32BigEndian(m_First.Slice(Position), out value))
          // Not enough space. Split the spans
          return (ReadByte() >> 24) | (ReadByte() >> 16) | (ReadByte() >> 8) | ReadByte();

        Position += 4;
      }
      else if (BinaryPrimitives.TryReadInt32BigEndian(m_Second.Slice(Position - m_First.Length), out value))
        Position += 4;
      else
        throw new OutOfMemoryException();

      return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32()
    {
      uint value;

      if (Position < m_First.Length)
      {
        if (!BinaryPrimitives.TryReadUInt32BigEndian(m_First.Slice(Position), out value))
          // Not enough space. Split the spans
          return (uint)((ReadByte() >> 24) | (ReadByte() >> 16) | (ReadByte() >> 8) | ReadByte());

        Position += 4;
      }
      else if (BinaryPrimitives.TryReadUInt32BigEndian(m_Second.Slice(Position - m_First.Length), out value))
        Position += 4;
      else
        throw new OutOfMemoryException();

      return value;
    }

    private static bool IsSafeChar(ushort c) => c >= 0x20 && c < 0xFFFE;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ReadString<T>(Encoding encoding, bool safeString = false, int fixedLength = 0x10000) where T : struct, IEquatable<T>
    {
      int sizeT = Unsafe.SizeOf<T>() - 1;

      if (sizeT > 1)
        throw new InvalidConstraintException("ReadString only accepts byte, sbyte, char, short, and ushort as a constraint");

      Span<byte> span;

      if (Position < m_First.Length)
      {
        var remaining = Remaining;
        var size = Math.Min(fixedLength << sizeT, remaining - (remaining & sizeT));

        // Find terminator
        var index = MemoryMarshal.Cast<byte, T>(m_First.Slice(Position, size))
          .IndexOf(default(T));

        // Split over both spans
        if (index < 0)
        {
          index = MemoryMarshal.Cast<byte, T>(m_Second.Slice(0, size - m_First.Length))
            .IndexOf(default(T));

          remaining = m_First.Length - Position;

          int length = index < 0 ? Remaining : remaining + index;

          Span<byte> bytes = stackalloc byte[length];
          m_First.Slice(Position).CopyTo(bytes);
          m_Second.Slice(0, length - remaining);

          Position += length;
          return GetString(bytes, encoding, safeString);
        }

        span = m_First.Slice(Position, index);
        Position += index;
      }
      else
      {
        var remaining = Position - m_First.Length;
        var size = Math.Min(fixedLength << sizeT, remaining - (remaining & sizeT));

        span = m_Second.Slice(remaining, size);
        var index = MemoryMarshal.Cast<byte, T>(span).IndexOf(default(T));

        if (index > -1)
        {
          span = span.Slice(0, index);
          Position += index;
        }
        else
        {
          Position += size;
        }
      }

      return GetString(span, encoding, safeString);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetString(Span<byte> span, Encoding encoding, bool safeString = false)
    {
      string s = encoding.GetString(span);
      if (!safeString) return s;

      ReadOnlySpan<char> chars = s.AsSpan();

      StringBuilder stringBuilder = null;

      for (int i = 0, last = 0; i < chars.Length; i++)
        if (!IsSafeChar(chars[i]) || stringBuilder != null && i == chars.Length - 1)
        {
          (stringBuilder ??= new StringBuilder()).Append(chars.Slice(last, i - last));
          last = i + 1; // Skip the unsafe char
        }

      return stringBuilder?.ToString() ?? s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUniSafe(int fixedLength) => ReadString<char>(Utility.UnicodeLE, true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUniSafe() => ReadString<char>(Utility.UnicodeLE, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUni(int fixedLength) => ReadString<char>(Utility.UnicodeLE, false, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadLittleUni() => ReadString<char>(Utility.UnicodeLE);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUniSafe(int fixedLength) => ReadString<char>(Utility.Unicode, true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUniSafe() => ReadString<char>(Utility.Unicode, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUni(int fixedLength) => ReadString<char>(Utility.Unicode, false, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadBigUni() => ReadString<char>(Utility.Unicode);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8Safe(int fixedLength) => ReadString<byte>(Utility.UTF8, true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8Safe() => ReadString<byte>(Utility.UTF8, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadUTF8() => ReadString<byte>(Utility.UTF8);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAsciiSafe(int fixedLength) => ReadString<byte>(Encoding.ASCII, true, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAsciiSafe() => ReadString<byte>(Encoding.ASCII, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAscii(int fixedLength) => ReadString<byte>(Encoding.ASCII, false, fixedLength);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadAscii() => ReadString<byte>(Encoding.ASCII);

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
