/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DynamicStringsEntry.cs                                          *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections;

namespace Server.Gumps.Components;

public readonly struct DynamicStringsEntry
{
    private readonly byte[] _data;
    private readonly BitArray _dynamicEntries;

    public DynamicStringsEntry(byte[] data, BitArray dynamicEntries)
    {
        _data = data;
        _dynamicEntries = dynamicEntries;
    }

    internal DynamicStringsFiller CreateFiller() => new(_dynamicEntries, _data);
}
