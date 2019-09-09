/***************************************************************************
 *                             GumpBackground.cs
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

using System.Buffers;
using Server.Buffers;
using Server.Network;

namespace Server.Gumps
{
  public class GumpBackground : GumpEntry
  {
    private int m_GumpID;
    private int m_Width, m_Height;
    private int m_X, m_Y;

    public GumpBackground(int x, int y, int width, int height, int gumpID)
    {
      m_X = x;
      m_Y = y;
      m_Width = width;
      m_Height = height;
      m_GumpID = gumpID;
    }

    public int X
    {
      get => m_X;
      set => Delta(ref m_X, value);
    }

    public int Y
    {
      get => m_Y;
      set => Delta(ref m_Y, value);
    }

    public int Width
    {
      get => m_Width;
      set => Delta(ref m_Width, value);
    }

    public int Height
    {
      get => m_Height;
      set => Delta(ref m_Height, value);
    }

    public int GumpID
    {
      get => m_GumpID;
      set => Delta(ref m_GumpID, value);
    }

    public override string Compile(ArraySet<string> strings) => $"{{ resizepic {m_X} {m_Y} {m_GumpID} {m_Width} {m_Height} }}";

    private static byte[] m_LayoutName = Gump.StringToBuffer("{ resizepic ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(68));
      writer.Write(m_LayoutName);
      writer.WriteAscii(m_X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_GumpID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Width.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Height.ToString());
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}