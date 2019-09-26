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
using Server.Collections;

namespace Server.Gumps
{
  public enum GumpButtonType
  {
    Page = 0,
    Reply = 1
  }

  public class GumpButton : GumpEntry
  {
    public GumpButton(int x, int y, int normalID, int pressedID, int buttonID,
      GumpButtonType type = GumpButtonType.Reply, int param = 0)
    {
      X = x;
      Y = y;
      NormalID = normalID;
      PressedID = pressedID;
      ButtonID = buttonID;
      Type = type;
      Param = param;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int NormalID { get; set; }

    public int PressedID { get; set; }

    public int ButtonID { get; set; }

    public GumpButtonType Type { get; set; }

    public int Param { get; set; }

    public override string Compile(ArraySet<string> strings) => $"{{ button {X} {Y} {NormalID} {PressedID} {(int)Type} {Param} {ButtonID} }}";

    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("{ button ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(68));
      writer.Write(m_LayoutName);
      writer.WriteAscii(X.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Y.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(NormalID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(PressedID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(((int)Type).ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Param.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(ButtonID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}
