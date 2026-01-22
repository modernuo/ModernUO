/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2026 - ModernUO Development Team                       *
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
    private readonly ReadOnlySpan<byte> _textBlock;
    private readonly ReadOnlySpan<ushort> _textIds;
    private readonly ReadOnlySpan<Range> _textRanges;

    public RelayInfo(
        int buttonId,
        ReadOnlySpan<int> switches,
        ReadOnlySpan<ushort> textIds,
        ReadOnlySpan<Range> textRanges,
        ReadOnlySpan<byte> textBlock
    )
    {
        ButtonID = buttonId;
        Switches = switches;
        _textIds = textIds;
        _textRanges = textRanges;
        _textBlock = textBlock;
    }

    public int ButtonID { get; }

    public ReadOnlySpan<int> Switches { get; }

    public bool IsSwitched(int switchId) => Switches.Contains(switchId);

    public string GetTextEntry(int entryId)
    {
        var index = _textIds.IndexOf((ushort)entryId);
        return index == -1 ? null : TextEncoding.GetStringBigUni(_textBlock[_textRanges[index]], true);
    }
}
