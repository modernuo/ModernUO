/***************************************************************************
 *                                GumpCheck.cs
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
  public class GumpCheck : GumpEntry
  {
    private int m_ID1, m_ID2;
    private bool m_InitialState;
    private int m_SwitchID;
    private int m_X, m_Y;

    public GumpCheck(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
    {
      m_X = x;
      m_Y = y;
      m_ID1 = inactiveID;
      m_ID2 = activeID;
      m_InitialState = initialState;
      m_SwitchID = switchID;
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

    public int InactiveID
    {
      get => m_ID1;
      set => Delta(ref m_ID1, value);
    }

    public int ActiveID
    {
      get => m_ID2;
      set => Delta(ref m_ID2, value);
    }

    public bool InitialState
    {
      get => m_InitialState;
      set => Delta(ref m_InitialState, value);
    }

    public int SwitchID
    {
      get => m_SwitchID;
      set => Delta(ref m_SwitchID, value);
    }

    public override string Compile() => $"{{ checkbox {m_X} {m_Y} {m_ID1} {m_ID2} {(m_InitialState ? 1 : 0)} {m_SwitchID} }}";

    private static byte[] m_LayoutName = Gump.StringToBuffer("{ checkbox ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(69));
      writer.Write(m_LayoutName);
      writer.WriteAscii(m_X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_ID1.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_ID2.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_InitialState ? "1" : "0");
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_SwitchID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}