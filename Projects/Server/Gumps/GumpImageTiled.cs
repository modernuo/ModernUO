/***************************************************************************
 *                             GumpImageTiled.cs
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
  public class GumpImageTiled : GumpEntry
  {
    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("gumppictiled");
    private int m_Width, m_Height;
    private int m_X, m_Y;

    public GumpImageTiled(int x, int y, int width, int height, int gumpID)
    {
      m_X = x;
      m_Y = y;
      m_Width = width;
      m_Height = height;
      GumpID = gumpID;
    }

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int GumpID { get; set; }

    public override string Compile(NetState ns) => $"{{ gumppictiled {X} {Y} {Width} {Height} {GumpID} }}";

    public override void AppendTo(NetState ns, IGumpWriter disp)
    {
      disp.AppendLayout(m_LayoutName);
      disp.AppendLayout(m_X);
      disp.AppendLayout(m_Y);
      disp.AppendLayout(m_Width);
      disp.AppendLayout(m_Height);
      disp.AppendLayout(GumpID);
    }
  }
}
