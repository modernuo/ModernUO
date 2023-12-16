/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: LayoutEntry.cs                                                  *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Gumps.Components;

public readonly struct LayoutEntry
{
    public readonly byte[] Data;
    public readonly int UncompressedLength;
    public readonly int Switches;
    public readonly int TextEntries;

    public bool IsEmpty => UncompressedLength == 0;

    public LayoutEntry(byte[] data, int uncompressedLength, int switches, int textEntries)
    {
        Data = data;
        UncompressedLength = uncompressedLength;
        Switches = switches;
        TextEntries = textEntries;
    }
}
