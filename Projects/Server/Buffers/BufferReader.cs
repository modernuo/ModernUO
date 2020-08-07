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
using System.Buffers;
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
    private readonly int m_Length;

    private readonly SequenceReader<byte> m_Reader;

    public int Position { get; private set; }
    public long Consumed { get; private set; }

    public BufferReader(Span<byte> first, Span<byte> second)
    {
      m_First = first;
      m_Second = second;
      Position = 0;
      Consumed = 0;
      m_Length = first.Length + second.Length;
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

    public SequencePosition Seek(long offset, SeekOrigin origin)
    {
      switch (origin)
      {
        case SeekOrigin.Begin:
          if (offset < m_Reader.Consumed)
            m_Reader.Rewind(m_Reader.Consumed - Math.Max(offset, 0L));
          else
            m_Reader.Advance(offset - m_Reader.Consumed);
          break;
        case SeekOrigin.Current:
          if (offset < 0)
            m_Reader.Rewind(Math.Min(m_Reader.Consumed, offset * -1));
          else
            m_Reader.Advance(Math.Min(m_Reader.Remaining, offset));
          break;
        case SeekOrigin.End:
          var count = m_Reader.Remaining - offset;
          if (count < 0)
            m_Reader.Rewind(count * -1);
          else if (count > 0)
            m_Reader.Advance(count);
          break;
      }

      return m_Reader.Position;
    }

    public bool TryReadByte(out byte value) => m_Reader.TryRead(out value);

    public int ReadInt32() => m_Reader.TryReadBigEndian(out int value) ? value : 0;

    public short ReadInt16() => m_Reader.TryReadBigEndian(out short value) ? value : (short)0;

    public byte ReadByte() => m_Reader.TryRead(out var value) ? value : (byte)0;

    public uint ReadUInt32() => (uint)ReadInt32();

    public ushort ReadUInt16() => (ushort)ReadInt16();

    public sbyte ReadSByte() => (sbyte)ReadByte();

    public bool ReadBoolean() => ReadByte() > 0;

    public string ReadUnicodeStringLESafe(int fixedLength)
    {
      var sb = new StringBuilder();

      while (fixedLength-- > 0 && m_Reader.TryReadLittleEndian(out short c) && c != 0)
        if (IsSafeChar(c))
          sb.Append((char)c);

      if (fixedLength > 0)
        m_Reader.Advance(fixedLength * 2);

      return sb.ToString();
    }

    public string ReadUnicodeStringLESafe()
    {
      var sb = new StringBuilder();

      while (m_Reader.TryReadLittleEndian(out short c) && c != 0)
        if (IsSafeChar(c))
          sb.Append((char)c);

      return sb.ToString();
    }

    public string ReadUnicodeStringSafe()
    {
      var sb = new StringBuilder();

      while (m_Reader.TryReadBigEndian(out short c) && c != 0)
        if (IsSafeChar(c))
          sb.Append((char)c);

      return sb.ToString();
    }

    public string ReadUnicodeString()
    {
      var sb = new StringBuilder();

      while (m_Reader.TryReadBigEndian(out short c) && c != 0)
        sb.Append((char)c);

      return sb.ToString();
    }

    private static bool IsSafeChar(ushort c) => c >= 0x20 && c < 0xFFFE;

    public string ReadUTF8StringSafe(int fixedLength)
    {
      string s;

      if (m_Reader.TryReadTo(out ReadOnlySpan<byte> span, (byte)'\0'))
      {
        s = Utility.UTF8.GetString(span.Length > fixedLength ? span.Slice(0, fixedLength) : span);
      }
      else
      {
        var size = Math.Min(m_Reader.Remaining, fixedLength);
        s = Utility.UTF8.GetString(m_Reader.Sequence.Slice(m_Reader.Position, size).ToArray());
        m_Reader.Advance(size);
      }

      var sb = new StringBuilder(s.Length);

      for (var i = 0; i < s.Length; ++i)
        if (IsSafeChar(s[i]))
          sb.Append(s[i]);

      return sb.ToString();
    }

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

    public string ReadUTF8String(bool safeString = false)
    {
      Span<byte> span;

      if (Position < m_First.Length)
      {
        var index = m_First.Slice(Position).IndexOf((byte)0); // Find terminator

        // Split over both spans
        if (index < 0)
        {
          index = m_Second.IndexOf((byte)0);
          var remaining = m_First.Length - Position;

          int length = index < 0 ? m_Length - Position : remaining + index;

          Span<byte> bytes = stackalloc byte[length];
          m_First.Slice(Position).CopyTo(bytes);
          m_Second.Slice(0, length - remaining);

          return GetString(bytes, Utility.UTF8, safeString);
        }

        span = m_First.Slice(Position, index);
      }
      else
      {
        var remaining = Position - m_First.Length;
        span = m_Second.Slice(remaining);
        var index = span.IndexOf((byte)0);
        if (index > -1)
          span = span.Slice(0, index);
      }

      return GetString(span, Utility.UTF8, safeString);
    }

    public string ReadUTF8String() =>
      Utility.UTF8.GetString(
        m_Reader.TryReadTo(out ReadOnlySpan<byte> span, (byte)'\0')
          ? span
          : m_Reader.Sequence.Slice(m_Reader.Position, m_Reader.Remaining).ToArray());

    public string ReadString()
    {
      var sb = new StringBuilder();

      while (m_Reader.TryRead(out var c))
        sb.Append((char)c);

      return sb.ToString();
    }

    public string ReadStringSafe()
    {
      var sb = new StringBuilder();

      while (m_Reader.TryRead(out var c))
        if (IsSafeChar(c))
          sb.Append((char)c);

      return sb.ToString();
    }

    public string ReadUnicodeStringSafe(int fixedLength)
    {
      var sb = new StringBuilder();

      while (fixedLength-- > 0 && m_Reader.TryReadBigEndian(out short c) && c != 0)
        if (IsSafeChar(c))
          sb.Append((char)c);

      if (fixedLength > 0)
        m_Reader.Advance(fixedLength * 2);

      return sb.ToString();
    }

    public string ReadUnicodeString(int fixedLength)
    {
      var sb = new StringBuilder();

      while (fixedLength-- > 0 && m_Reader.TryReadBigEndian(out short c) && c != 0)
        sb.Append((char)c);

      if (fixedLength > 0)
        m_Reader.Advance(fixedLength * 2);

      return sb.ToString();
    }

    public string ReadStringSafe(int fixedLength)
    {
      var sb = new StringBuilder();

      while (fixedLength-- > 0 && m_Reader.TryRead(out var c) && c != 0)
        if (IsSafeChar(c))
          sb.Append((char)c);

      if (fixedLength > 0)
        m_Reader.Advance(fixedLength);

      return sb.ToString();
    }

    public string ReadString(int fixedLength)
    {
      var sb = new StringBuilder();

      while (fixedLength-- > 0 && m_Reader.TryRead(out var c) && c != 0)
        sb.Append((char)c);

      if (fixedLength > 0)
        m_Reader.Advance(fixedLength);

      return sb.ToString();
    }
  }
}
