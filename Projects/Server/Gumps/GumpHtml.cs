/***************************************************************************
 *                                GumpHtml.cs
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
  public class GumpHtml : GumpEntry
  {
    public GumpHtml(int x, int y, int width, int height, string text, bool background, bool scrollbar)
    {
      X = x;
      Y = y;
      Width = width;
      Height = height;
      Text = text;
      Background = background;
      Scrollbar = scrollbar;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public string Text { get; set; }

    public bool Background { get; set; }

    public bool Scrollbar { get; set; }

    public override string Compile(ArraySet<string> strings) =>
      $"{{ htmlgump {X} {Y} {Width} {Height} {strings.Add(Text)} {(Background ? 1 : 0)} {(Scrollbar ? 1 : 0)} }}";

    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("{ htmlgump ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(71));
      writer.Write(m_LayoutName);
      writer.WriteAscii(X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Width.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Height.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(strings.Add(Text).ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Background ? "1" : "0");
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Scrollbar ? "1" : "0");
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}
