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

using Server.Text;
using System;

namespace Server.Gumps;

public readonly ref struct RelayInfo
{
    private readonly ReadOnlySpan<ushort> textIds;
    private readonly ReadOnlySpan<Range> textRanges;
    private readonly ReadOnlySpan<byte> rawTextData;

    public RelayInfo(
        int buttonID,
        int[] switches,
        ReadOnlySpan<ushort> textIds,
        ReadOnlySpan<Range> textRanges,
        ReadOnlySpan<byte> rawTextData
        )
    {
        ButtonID = buttonID;
        Switches = switches;
        this.textIds = textIds;
        this.textRanges = textRanges;
        this.rawTextData = rawTextData;
    }

    public int ButtonID { get; }

    public ReadOnlySpan<int> Switches { get; }

    public bool IsSwitched(int switchID)
    {
        return Switches.Contains(switchID);
    }

    public string GetTextEntry(int entryId)
    {
        int index = textIds.IndexOf((ushort)entryId);

        if (index == -1)
        {
            return default;
        }

        return TextEncoding.GetString(rawTextData[textRanges[index]], TextEncoding.Unicode, true);
    }
}
