/***************************************************************************
 *                               GumpImage.cs
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
  public class GumpImage : GumpEntry
  {
    public GumpImage(int x, int y, int gumpID, int hue = 0)
    {
      X = x;
      Y = y;
      GumpID = gumpID;
      Hue = hue;
    }

    public GumpImage(int x, int y, int gumpID, int hue = 0, string cls = null)
    {
      X = x;
      Y = y;
      GumpID = gumpID;
      Hue = hue;
      Class = cls;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int GumpID { get; set; }

    public int Hue { get; set; }

    public string Class { get; set; }

    public override string Compile(ArraySet<string> strings) => Hue == 0 ?
      $"{{ gumppic {X} {Y} {GumpID} }}" :
      $"{{ gumppic {X} {Y} {GumpID} hue={Hue} }}";

    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("{ gumppic ");
    private static readonly byte[] m_HueEquals = Gump.StringToBuffer(" hue=");
    private static readonly byte[] m_ClassEquals = Gump.StringToBuffer(" class=");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(66 + Class?.Length ?? 0));
      writer.Write(m_LayoutName);
      writer.WriteAscii(X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(GumpID.ToString());
      writer.Write((byte)0x20); // ' '

      if (Hue != 0)
      {
        writer.Write(m_HueEquals);
        writer.WriteAscii(Hue.ToString());
        writer.Write((byte)0x20); // ' '
      }

      if (!string.IsNullOrWhiteSpace(Class))
      {
        writer.Write(m_ClassEquals);
        writer.WriteAscii(Class);
        writer.Write((byte)0x20); // ' '
      }

      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}
