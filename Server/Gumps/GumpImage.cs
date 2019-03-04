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

using Server.Network;

namespace Server.Gumps
{
  public class GumpImage : GumpEntry
  {
    private static byte[] m_LayoutName = Gump.StringToBuffer("gumppic");
    private static byte[] m_HueEquals = Gump.StringToBuffer(" hue=");
    private int m_GumpID;
    private int m_Hue;
    private int m_X, m_Y;

    public GumpImage(int x, int y, int gumpID, int hue= 0)
    {
      m_X = x;
      m_Y = y;
      m_GumpID = gumpID;
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

    public override string Compile(NetState ns)
    {
      if (m_Hue == 0)
        return $"{{ gumppic {m_X} {m_Y} {m_GumpID} }}";
      return $"{{ gumppic {m_X} {m_Y} {m_GumpID} hue={m_Hue} }}";
    }

    public override void AppendTo(NetState ns, IGumpWriter disp)
    {
      disp.AppendLayout(m_LayoutName);
      disp.AppendLayout(m_X);
      disp.AppendLayout(m_Y);
      disp.AppendLayout(m_GumpID);

      if (m_Hue != 0)
      {
        disp.AppendLayout(m_HueEquals);
        disp.AppendLayoutNS(m_Hue);
      }
    }
  }
}
