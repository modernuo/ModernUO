/***************************************************************************
 *                                GumpItem.cs
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

using Server.Network;

namespace Server.Gumps
{
  public class GumpItem : GumpEntry
  {
    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("tilepic");
    private static readonly byte[] m_LayoutNameHue = Gump.StringToBuffer("tilepichue");
    private int m_Hue;
    private int m_ItemID;
    private int m_X, m_Y;

    public GumpItem(int x, int y, int itemID, int hue = 0)
    {
      m_X = x;
      m_Y = y;
      m_ItemID = itemID;
      m_Hue = hue;
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

    public override string Compile(NetState ns) =>
      m_Hue == 0 ? $"{{ tilepic {m_X} {m_Y} {m_ItemID} }}" : $"{{ tilepichue {m_X} {m_Y} {m_ItemID} {m_Hue} }}";

    public override void AppendTo(NetState ns, IGumpWriter disp)
    {
      disp.AppendLayout(m_Hue == 0 ? m_LayoutName : m_LayoutNameHue);
      disp.AppendLayout(m_X);
      disp.AppendLayout(m_Y);
      disp.AppendLayout(m_ItemID);

      if (m_Hue != 0)
        disp.AppendLayout(m_Hue);
    }
  }
}
