/***************************************************************************
 *                                Packets.cs
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Server.Gumps;

namespace Server.Network
{
  public static partial class Packets
  {
    public static readonly int MaxPacketSize = 0x10000;
    public static bool UseCompression { get; set; } = true;
  }

  public sealed class DisplayGumpFast : Packet, IGumpWriter
  {
    private static byte[] m_True = Gump.StringToBuffer(" 1");
    private static byte[] m_False = Gump.StringToBuffer(" 0");

    private static byte[] m_BeginTextSeparator = Gump.StringToBuffer(" @");
    private static byte[] m_EndTextSeparator = Gump.StringToBuffer("@");

    private byte[] m_Buffer = new byte[48];
    private int m_LayoutLength;

    public DisplayGumpFast(Gump g) : base(0xB0)
    {
      m_Buffer[0] = (byte)' ';

      EnsureCapacity(4096);

      m_Stream.Write(g.Serial);
      m_Stream.Write(g.TypeID);
      m_Stream.Write(g.X);
      m_Stream.Write(g.Y);
      m_Stream.Write((ushort)0xFFFF);
    }

    public int TextEntries { get; set; }

    public int Switches { get; set; }

    public void AppendLayout(bool val)
    {
      AppendLayout(val ? m_True : m_False);
    }

    public void AppendLayout(int val)
    {
      string toString = val.ToString();
      int bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

      m_Stream.Write(m_Buffer, 0, bytes);
      m_LayoutLength += bytes;
    }

    public void AppendLayout(uint val)
    {
      string toString = val.ToString();
      int bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1) + 1;

      m_Stream.Write(m_Buffer, 0, bytes);
      m_LayoutLength += bytes;
    }

    public void AppendLayoutNS(int val)
    {
      string toString = val.ToString();
      int bytes = Encoding.ASCII.GetBytes(toString, 0, toString.Length, m_Buffer, 1);

      m_Stream.Write(m_Buffer, 1, bytes);
      m_LayoutLength += bytes;
    }

    public void AppendLayout(string text)
    {
      AppendLayout(m_BeginTextSeparator);

      int length = text.Length;
      m_Stream.WriteAsciiFixed(text, length);
      m_LayoutLength += length;

      AppendLayout(m_EndTextSeparator);
    }

    public void AppendLayout(byte[] buffer)
    {
      int length = buffer.Length;
      m_Stream.Write(buffer, 0, length);
      m_LayoutLength += length;
    }

    public void WriteStrings(List<string> text)
    {
      m_Stream.Seek(19, SeekOrigin.Begin);
      m_Stream.Write((ushort)m_LayoutLength);
      m_Stream.Seek(0, SeekOrigin.End);

      m_Stream.Write((ushort)text.Count);

      for (int i = 0; i < text.Count; ++i)
      {
        string v = text[i] ?? "";

        int length = (ushort)v.Length;

        m_Stream.Write((ushort)length);
        m_Stream.WriteBigUniFixed(v, length);
      }
    }

    public void Flush()
    {
    }
  }
}
