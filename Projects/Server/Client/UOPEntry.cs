/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2024 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: UOPEntry.cs                                                     *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server;

public struct UOPEntry
{
    public readonly long Offset;
    public readonly int Size;
    public bool Compressed;
    public int CompressedSize;
    public int Extra;

    public UOPEntry(long offset, int length)
    {
        Offset = offset;
        Size = length;
        Extra = 0;
    }
}
