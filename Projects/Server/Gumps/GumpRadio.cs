/*************************************************************************
 * ModernUO                                                              *
 * Copyright (C) 2019-2022 - ModernUO Development Team                   *
 * Email: hi@modernuo.com                                                *
 * File: GumpRadio.cs                                                    *
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

public class GumpRadio : GumpEntry
{
    public GumpRadio(int x, int y, int inactiveID, int activeID, bool initialState, int switchID)
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

    public override void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, scoped ref int entries, scoped ref int switches)
    {
        var initialState = InitialState ? "1" : "0";
        writer.WriteAscii($"{{ radio {X} {Y} {InactiveID} {ActiveID} {initialState} {SwitchID} }}");
        switches++;
    }
}
