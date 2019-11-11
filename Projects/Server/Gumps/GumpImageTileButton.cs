/***************************************************************************
 *                               GumpImageTileButton.cs
 *                            -------------------
 *   begin                : April 26, 2005
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
  public class GumpImageTileButton : GumpEntry
  {
    //Note, on OSI, The tooltip supports ONLY clilocs as far as I can figure out, and the tooltip ONLY works after the buttonTileArt (as far as I can tell from testing)

    public GumpImageTileButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param,
      int itemID, int hue, int width, int height, int localizedTooltip = -1)
    {
      X = x;
      Y = y;
      NormalID = normalID;
      PressedID = pressedID;
      ButtonID = buttonID;
      Type = type;
      Param = param;

      ItemID = itemID;
      Hue = hue;
      Width = width;
      Height = height;

      LocalizedTooltip = localizedTooltip;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int NormalID { get; set; }

    public int PressedID { get; set; }

    public int ButtonID { get; set; }

    public GumpButtonType Type { get; set; }

    public int Param { get; set; }

    public int ItemID { get; set; }

    public int Hue { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int LocalizedTooltip{ get; set; }

    public override string Compile(ArraySet<string> strings) =>
      $"{{ buttontileart {X} {Y} {NormalID} {PressedID} {(int)Type} {Param} {ButtonID} {ItemID} {Hue} {Width} {Height} }}{(LocalizedTooltip > 0 ? $"{{ tooltip {LocalizedTooltip} }}" : "")}";

    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("{ buttontileart ");
    private static readonly byte[] m_LayoutTooltip = Gump.StringToBuffer(" }{ tooltip ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(160));
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
      writer.WriteAscii(ItemID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Hue.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Width.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(Height.ToString());
      writer.Write((byte)0x20); // ' '

      if (LocalizedTooltip > 0)
      {
        writer.Write(m_LayoutTooltip);
        writer.WriteAscii(LocalizedTooltip.ToString());
        writer.Write((byte)0x20); // ' '
      }

      writer.Write((byte)0x7D); // '}'
      buffer.Advance(writer.WrittenCount);
    }
  }
}
