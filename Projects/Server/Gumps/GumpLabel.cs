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
    private int m_Hue;
    private string m_Text;
    private int m_X, m_Y;

    public GumpLabel(int x, int y, int hue, string text)
    {
      m_X = x;
      m_Y = y;
      m_Hue = hue;
      m_Text = text;
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

    public int Hue
    {
      get => m_Hue;
      set => Delta(ref m_Hue, value);
    }

    public string Text
    {
      get => m_Text;
      set => Delta(ref m_Text, value);
    }

    public override string Compile(ArraySet<string> strings) => $"{{ text {m_X} {m_Y} {m_Hue} {strings.Add(m_Text)} }}";

    private static byte[] m_LayoutName = Gump.StringToBuffer("{ text ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(52));
      writer.Write(m_LayoutName);
      writer.WriteAscii(m_X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Hue.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(strings.Add(m_Text).ToString());
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'

      buffer.Advance(writer.WrittenCount);
    }
  }
}
