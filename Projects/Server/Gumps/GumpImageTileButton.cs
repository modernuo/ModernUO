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
using Server.Network;

namespace Server.Gumps
{
  public class GumpImageTileButton : GumpEntry
  {
    private int m_ButtonID;
    private int m_Height;
    private int m_Hue;
    private int m_ID1, m_ID2;

    private int m_ItemID;
    private int m_Param;
    private int m_Width;

    //Note, on OSI, The tooltip supports ONLY clilocs as far as I can figure out, and the tooltip ONLY works after the buttonTileArt (as far as I can tell from testing)
    private int m_X, m_Y;

    public GumpImageTileButton(int x, int y, int normalID, int pressedID, int buttonID, GumpButtonType type, int param,
      int itemID, int hue, int width, int height, int localizedTooltip = -1)
    {
      m_X = x;
      m_Y = y;
      m_ID1 = normalID;
      m_ID2 = pressedID;
      m_ButtonID = buttonID;
      Type = type;
      m_Param = param;

      m_ItemID = itemID;
      m_Hue = hue;
      m_Width = width;
      m_Height = height;

      LocalizedTooltip = localizedTooltip;
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

    public int ItemID
    {
      get => m_ItemID;
      set => Delta(ref m_ItemID, value);
    }

    public int Hue
    {
      get => m_Hue;
      set => Delta(ref m_Hue, value);
    }

    public int Width
    {
      get => m_Width;
      set => Delta(ref m_Width, value);
    }

    public int Height
    {
      get => m_Height;
      set => Delta(ref m_Height, value);
    }

    public int LocalizedTooltip{ get; set; }

    public override string Compile() =>
      $"{{ buttontileart {m_X} {m_Y} {m_ID1} {m_ID2} {(int)Type} {m_Param} {m_ButtonID} {m_ItemID} {m_Hue} {m_Width} {m_Height} }}{(LocalizedTooltip > 0 ? $"{{ tooltip {LocalizedTooltip} }}" : "")}";

    private static byte[] m_LayoutName = Gump.StringToBuffer("{ buttontileart ");
    private static byte[] m_LayoutTooltip = Gump.StringToBuffer(" }{ tooltip ");

    public override void AppendTo(ArrayBufferWriter<byte> buffer, ArraySet<string> strings, ref int entries, ref int switches)
    {
      SpanWriter writer = new SpanWriter(buffer.GetSpan(160));
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
      writer.WriteAscii(m_ItemID.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Hue.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Width.ToString());
      writer.Write((byte)0x20); // ' '
      writer.WriteAscii(m_Height.ToString());
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
