/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2022 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: GumpTooltip.cs                                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Buffers;
using Server.Collections;

namespace Server.Gumps;

// Note, on OSI, the tooltip supports ONLY clilocs as far as I can figure out,
// and the tooltip ONLY works after the buttonTileArt (as far as I can tell from testing)
public class GumpTooltip : GumpEntry
{
    public GumpTooltip(int number, string args)
    {
        Number = number;
        Args = args;
    }

    public int Number { get; set; }

    public string Args { get; set; }

    public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, scoped ref int entries, scoped ref int switches)
    {
        writer.WriteAscii(string.IsNullOrEmpty(Args) ? $"{{ tooltip {Number} }}" : $"{{ tooltip {Number} @{Args}@ }}");
    }
}
