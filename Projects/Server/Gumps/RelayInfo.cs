/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: RelayInfo.cs                                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;

namespace Server.Gumps;

public readonly struct TextRelay
{
    public TextRelay(int entryId, string text)
    {
        EntryId = entryId;
        Text = text;
    }

    public int EntryId { get; }

    public string Text { get; }
}

public ref struct RelayInfo
{
    public RelayInfo(int buttonID, int[] switches, TextRelay[] textEntries)
    {
        ButtonID = buttonID;
        Switches = switches;
        TextEntries = textEntries;
    }

    public int ButtonID { get; }

    public ReadOnlySpan<int> Switches { get; }

    public ReadOnlySpan<TextRelay> TextEntries { get; }

    public bool IsSwitched(int switchID)
    {
        for (var i = 0; i < Switches.Length; ++i)
        {
            if (Switches[i] == switchID)
            {
                return true;
            }
        }

        return false;
    }

    public string GetTextEntry(int entryId)
    {
        for (var i = 0; i < TextEntries.Length; ++i)
        {
            if (TextEntries[i].EntryId == entryId)
            {
                return TextEntries[i].Text;
            }
        }

        return default;
    }
}
