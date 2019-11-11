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
using Server.Collections;

namespace Server.Gumps
{
  public class GumpCheck : GumpEntry
  {
    public GumpCheck(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
    {
      X = x;
      Y = y;
      InactiveID = inactiveID;
      ActiveID = activeID;
      InitialState = initialState;
      SwitchID = switchID;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int InactiveID { get; set; }

    public int ActiveID { get; set; }

    public bool InitialState { get; set; }

    public int SwitchID { get; set; }

    public override string Compile(NetState ns) => $"{{ checkbox {m_X} {m_Y} {m_ID1} {m_ID2} {(m_InitialState ? 1 : 0)} {m_SwitchID} }}";

    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("{ checkbox ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(69));
      writer.Write(m_LayoutName);
      writer.WriteAscii(X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(InactiveID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(ActiveID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(InitialState ? "1" : "0");
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(SwitchID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}
