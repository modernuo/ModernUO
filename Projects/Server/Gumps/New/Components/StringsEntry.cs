/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: StringsEntry.cs                                                 *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

namespace Server.Gumps.Components
{
    public readonly struct StringsEntry
    {
        public readonly byte[] Data;
        public readonly int UncompressedLength;
        public readonly int Count;
        public readonly bool Compressed;

        public StringsEntry(byte[] data, int stringsCount, bool compressed, int uncompressedLength)
        {
            Data = data;
            Count = stringsCount;
            Compressed = compressed;
            UncompressedLength = uncompressedLength;
        }
    }
}
