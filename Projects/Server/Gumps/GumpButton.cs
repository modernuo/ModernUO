/***************************************************************************
 *                               GumpButton.cs
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
  public enum GumpButtonType
  {
    Page = 0,
    Reply = 1
  }

  public class GumpButton : GumpEntry
  {
    private int m_ButtonID;
    private int m_ID1, m_ID2;
    private int m_Param;
    private int m_X, m_Y;

    public GumpButton(int x, int y, int normalID, int pressedID, int buttonID,
      GumpButtonType type = GumpButtonType.Reply, int param = 0)
    {
      m_X = x;
      m_Y = y;
      m_ID1 = normalID;
      m_ID2 = pressedID;
      m_ButtonID = buttonID;
      Type = type;
      m_Param = param;
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

    public int NormalID
    {
      get => m_ID1;
      set => Delta(ref m_ID1, value);
    }

    public int PressedID
    {
      get => m_ID2;
      set => Delta(ref m_ID2, value);
    }

    public int ButtonID
    {
      get => m_ButtonID;
      set => Delta(ref m_ButtonID, value);
    }

    public GumpButtonType Type { get; set; }

    public int Param
    {
      get => m_Param;
      set => Delta(ref m_Param, value);
    }

    public override string Compile() => $"{{ button {m_X} {m_Y} {m_ID1} {m_ID2} {(int)Type} {m_Param} {m_ButtonID} }}";

    private static byte[] m_LayoutName = Gump.StringToBuffer("{ button ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(68));
      writer.Write(m_LayoutName);
      writer.WriteAscii(m_X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_ID1.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_ID2.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(((int)Type).ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Param.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_ButtonID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}
