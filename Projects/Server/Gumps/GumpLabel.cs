/***************************************************************************
 *                               GumpLabel.cs
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
  public class GumpLabel : GumpEntry
  {
    public GumpLabel(int x, int y, int hue, string text)
    {
      X = x;
      Y = y;
      Hue = hue;
      Text = text;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int Hue { get; set; }

    public string Text { get; set; }

    public override string Compile(ArraySet<string> strings) => $"{{ text {X} {Y} {Hue} {strings.Add(Text)} }}";

    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("{ text ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(52));
      writer.Write(m_LayoutName);
      writer.WriteAscii(X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Hue.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(strings.Add(Text).ToString());
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'

      buffer.Advance(writer.WrittenCount);
    }
  }
}
