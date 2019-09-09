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

namespace Server.Gumps
{
  public class GumpImage : GumpEntry
  {
    private int m_GumpID;
    private int m_Hue;
    private int m_X, m_Y;
    private string m_Class;

    public GumpImage(int x, int y, int gumpID, int hue = 0)
    {
      m_X = x;
      m_Y = y;
      m_GumpID = gumpID;
      m_Hue = hue;
    }

    public GumpImage(int x, int y, int gumpID, int hue = 0, string cls = null)
    {
      m_X = x;
      m_Y = y;
      m_GumpID = gumpID;
      m_Hue = hue;
      m_Class = cls;
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

    public int GumpID
    {
      get => m_GumpID;
      set => Delta(ref m_GumpID, value);
    }

    public int Hue
    {
      get => m_Hue;
      set => Delta(ref m_Hue, value);
    }

    public string Class
    {
      get => m_Class;
      set => Delta(ref m_Class, value);
    }

    public override string Compile(ArraySet<string> strings) => m_Hue == 0 ?
      $"{{ gumppic {m_X} {m_Y} {m_GumpID} }}" :
      $"{{ gumppic {m_X} {m_Y} {m_GumpID} hue={m_Hue} }}";

    private static byte[] m_LayoutName = Gump.StringToBuffer("{ gumppic ");
    private static byte[] m_HueEquals = Gump.StringToBuffer(" hue=");
    private static byte[] m_ClassEquals = Gump.StringToBuffer(" class=");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(66 + m_Class?.Length ?? 0));
      writer.Write(m_LayoutName);
      writer.WriteAscii(m_X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_GumpID.ToString());
      writer.Write((byte)0x20); // ' '

      if (m_Hue != 0)
      {
        writer.Write(m_HueEquals);
        writer.WriteAscii(m_Hue.ToString());
        writer.Write((byte)0x20); // ' '
      }

      if (!string.IsNullOrWhiteSpace(m_Class))
      {
        writer.Write(m_ClassEquals);
        writer.WriteAscii(m_Class);
        writer.Write((byte)0x20); // ' '
      }

      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}
