/***************************************************************************
 *                                GumpItem.cs
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
using Server.Collections;

namespace Server.Gumps
{
  public class GumpItem : GumpEntry
  {
    private int m_Hue;
    private int m_ItemID;
    private int m_X, m_Y;

    public GumpItem(int x, int y, int itemID, int hue = 0)
    {
      m_X = x;
      m_Y = y;
      m_ItemID = itemID;
      m_Hue = hue;
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

    public int ItemID
    {
      get => m_ItemID;
      set => Delta(ref m_ItemID, value);
    }

    public int Hue
    {
      get => m_Hue;
      set => Delta(ref m_Hue, value);
    }

    public override string Compile(ArraySet<string> strings) => m_Hue == 0 ?
      $"{{ tilepic {m_X} {m_Y} {m_ItemID} }}" :
      $"{{ tilepichue {m_X} {m_Y} {m_ItemID} {m_Hue} }}";

    private static byte[] m_LayoutName = Gump.StringToBuffer("{ tilepic ");
    private static byte[] m_LayoutNameHue = Gump.StringToBuffer("{ tilepichue ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(57));
      writer.Write(m_Hue == 0 ? m_LayoutName : m_LayoutNameHue);
      writer.WriteAscii(m_X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_ItemID.ToString());
      writer.Write((byte)0x20); // ' '

      if (m_Hue != 0)
      {
        writer.WriteAscii(m_Hue.ToString());
        writer.Write((byte)0x20); // ' '
      }

      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'

      buffer.Advance(writer.WrittenCount);
    }
  }
}
