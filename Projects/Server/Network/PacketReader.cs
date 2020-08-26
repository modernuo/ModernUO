/***************************************************************************
 *                              PacketReader.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
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
using System.Buffers;
using System.IO;
using System.Text;

namespace Server.Network
{
    public ref struct PacketReader
    {
        private SequenceReader<byte> m_Reader;

        public SequencePosition Position => m_Reader.Position;
        public long Length => m_Reader.Length;
        public long Consumed => m_Reader.Consumed;
        public long Remaining => m_Reader.Remaining;

        public PacketReader(ReadOnlySequence<byte> seq) => m_Reader = new SequenceReader<byte>(seq);

        public byte Peek() => m_Reader.TryPeek(out var value) ? value : (byte)0;

        public void Trace(NetState state)
        {
            try
            {
                using var sw = new StreamWriter("Packets.log", true);
                var buffer = m_Reader.Sequence.ToArray();

                if (buffer.Length > 0)
                    sw.WriteLine("Client: {0}: Unhandled packet 0x{1:X2}", state, buffer[0]);

                using (var ms = new MemoryStream(buffer))
                {
                    Utility.FormatBuffer(sw, ms, buffer.Length);
                }

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

        public string ReadUnicodeStringLE()
        {
            var sb = new StringBuilder();

            while (m_Reader.TryReadLittleEndian(out short c) && c != 0)
                sb.Append((char)c);

            return sb.ToString();
        }

        public string ReadUnicodeStringLE(int fixedLength)
        {
            var sb = new StringBuilder();

            while (fixedLength-- > 0 && m_Reader.TryReadLittleEndian(out short c) && c != 0)
                sb.Append((char)c);

            if (fixedLength > 0)
                m_Reader.Advance(fixedLength);

            return sb.ToString();
        }

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

        private static bool IsSafeChar(int c) => c >= 0x20 && c < 0xFFFE;

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

        public string ReadUTF8StringSafe()
        {
            string s;

            if (m_Reader.TryReadTo(out ReadOnlySpan<byte> span, (byte)'\0'))
            {
                s = Utility.UTF8.GetString(span);
            }
            else
            {
                s = Utility.UTF8.GetString(m_Reader.Sequence.Slice(m_Reader.Position, m_Reader.Remaining).ToArray());
                m_Reader.Advance(m_Reader.Remaining);
            }

            var sb = new StringBuilder(s.Length);

            for (var i = 0; i < s.Length; ++i)
                if (IsSafeChar(s[i]))
                    sb.Append(s[i]);

            return sb.ToString();
        }

        public string ReadUTF8String() =>
            Utility.UTF8.GetString(
                m_Reader.TryReadTo(out ReadOnlySpan<byte> span, (byte)'\0')
                    ? span
                    : m_Reader.Sequence.Slice(m_Reader.Position, m_Reader.Remaining).ToArray()
            );

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
