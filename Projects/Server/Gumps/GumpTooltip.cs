/***************************************************************************
 *                               GumpTooltip.cs
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
  public class GumpTooltip : GumpEntry
  {
    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("tooltip");
    private int m_Number;

    public GumpTooltip(int number) => m_Number = number;

    public int Number
    {
      get => m_Number;
      set => Delta(ref m_Number, value);
    }

    public override string Compile(NetState ns) => $"{{ tooltip {m_Number} }}";

    public override void AppendTo(NetState ns, IGumpWriter disp)
    {
      disp.AppendLayout(m_LayoutName);
      disp.AppendLayout(m_Number);
    }
  }
}