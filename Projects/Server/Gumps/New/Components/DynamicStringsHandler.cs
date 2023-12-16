/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: DynamicStringsHandler.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using Server.Gumps.Interfaces;
using Server.Text;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Server.Gumps.Components;

public readonly struct DynamicStringsHandler : IStringsHandler
{
    private static readonly byte[] _buffer = GumpBuilderExtensions.StringsBuffer;
    private static readonly Dictionary<int, int> _stringHashes = [];
    private static readonly List<int> _dynamicIndexes = [];
    private static int _position;
    internal static bool DynamicMode;

    public int BytesWritten => _position;
    public int Count => _stringHashes.Count;

    public int Internalize(ReadOnlySpan<char> value)
    {
        int hash = string.GetHashCode(value);

        if (!_stringHashes.TryGetValue(hash, out int index))
        {
            index = _stringHashes.Count;

            _stringHashes.Add(hash, index);

            if (DynamicMode)
            {
                _dynamicIndexes.Add(index);
                return index;
            }

            BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_position), (ushort)value.Length);
            _position += 2;

            if (BitConverter.IsLittleEndian)
            {
                _position += TextEncoding.Unicode.GetBytes(value, _buffer.AsSpan(_position));
            }
            else
            {
                ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(value);
                bytes.CopyTo(_buffer.AsSpan(_position));
                _position += bytes.Length;
            }
        }

        return index;
    }

    internal DynamicStringsEntry Finalize()
    {
        BitArray entries = new(_stringHashes.Count);

        for(int i = 0; i < _dynamicIndexes.Count; i++)
        {
            entries[_dynamicIndexes[i]] = true;
        }

        // Copies buffer
        return new DynamicStringsEntry([.._buffer[.._position]], entries);
    }

    public void Dispose()
    {
        _position = 0;
        _stringHashes.Clear();
        _dynamicIndexes.Clear();
        DynamicMode = false;
    }
}
