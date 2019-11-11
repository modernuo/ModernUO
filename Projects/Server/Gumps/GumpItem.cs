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
    public GumpItem(int x, int y, int itemID, int hue = 0)
    {
      X = x;
      Y = y;
      ItemID = itemID;
      Hue = hue;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int ItemID { get; set; }

    public int Hue { get; set; }

    public override string Compile(NetState ns) =>
      m_Hue == 0 ? $"{{ tilepic {m_X} {m_Y} {m_ItemID} }}" :
        $"{{ tilepichue {m_X} {m_Y} {m_ItemID} {m_Hue} }}";

    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("{ tilepic ");
    private static readonly byte[] m_LayoutNameHue = Gump.StringToBuffer("{ tilepichue ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(57));
      writer.Write(Hue == 0 ? m_LayoutName : m_LayoutNameHue);
      writer.WriteAscii(X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(ItemID.ToString());
      writer.Write((byte)0x20); // ' '

      if (Hue != 0)
      {
        writer.WriteAscii(Hue.ToString());
        writer.Write((byte)0x20); // ' '
      }

      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'

      buffer.Advance(writer.WrittenCount);
    }
  }
}
