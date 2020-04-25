/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2020 - ModernUO Development Team                        *
 * Email: hi@modernuo.com                                                *
 * File: GumpTooltip.cs - Created: 2020/04/24 - Updated: 2020/04/24      *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * This program is distributed in the hope that it will be useful,       *
 * but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 * GNU General Public License for more details.                          *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Text;
using Server.Network;

namespace Server.Gumps
{
  public class GumpTooltip : GumpEntry
  {
    private static readonly byte[] m_LayoutName = Gump.StringToBuffer("tooltip");

    private string m_ArgsString;
    private TextDefinition[] m_Args;

    public GumpTooltip(int number, params TextDefinition[] args)
    {
      Number = number;
      Args = args;
    }

    public int Number { get; set; }

    public TextDefinition[] Args
    {
      get => m_Args;
      set
      {
        m_Args = value;
        // Implementor should not assign if m_Args hasn't changed.
        m_ArgsString = BuildStringArgs();
      }
    }

    private string BuildStringArgs()
    {
      StringBuilder builder = new StringBuilder();
      for (int i = 0; i < m_Args.Length; i++)
      {
        TextDefinition arg = m_Args[i];
        builder.AppendFormat("@{0}", arg);
      }

      return builder.ToString();
    }

    public override string Compile(NetState ns) => $"{{ tooltip {Number} {m_ArgsString} }}";

    public override void AppendTo(NetState ns, IGumpWriter disp)
    {
      disp.AppendLayout(m_LayoutName);
      disp.AppendLayout(Number);
      disp.AppendLayout(m_ArgsString);
    }
  }
}
