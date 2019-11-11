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
using Server.Collections;

namespace Server.Gumps
{
  public class GumpBackground : GumpEntry
  {
    private int m_GumpID;

    public GumpBackground(int x, int y, int width, int height, int gumpID)
    {
      X = x;
      Y = y;
      Width = width;
      Height = height;
      m_GumpID = gumpID;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public override string Compile(ArraySet<string> strings) => $"{{ resizepic {X} {Y} {m_GumpID} {Width} {Height} }}";

    public override string Compile(NetState ns) => $"{{ resizepic {m_X} {m_Y} {m_GumpID} {m_Width} {m_Height} }}";

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(68));
      writer.Write(m_LayoutName);
      writer.WriteAscii(X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_GumpID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Width.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Height.ToString());
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}
