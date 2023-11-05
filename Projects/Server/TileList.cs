/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2023 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TileList.cs                                                     *
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server;

public class TileList
{
    private static readonly StaticTile[] _emptyTiles = Array.Empty<StaticTile>();
    private StaticTile[] _tiles;

    public int Count { get; private set; }

    public void AddRange(ReadOnlySpan<StaticTile> tiles)
    {
        TryResize(tiles.Length);
        for (var i = 0; i < tiles.Length; ++i)
        {
            _tiles[Count++] = tiles[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Add(StaticTile* ptr) => Add(Marshal.PtrToStructure<StaticTile>((nint)ptr));

    public void Add(StaticTile tile)
    {
        TryResize(1);
        _tiles[Count] = tile;
        ++Count;
    }

    public void Add(ushort id, byte x, byte y, sbyte z, short hue = 0)
    {
        TryResize(1);
        ref var tile = ref _tiles[Count];
        tile.m_ID = id;
        tile.m_X = x;
        tile.m_Y = y;
        tile.m_Z = z;
        tile.m_Hue = hue;
        ++Count;
    }

    public void Add(ushort id, sbyte z)
    {
        TryResize(1);
        _tiles[Count].m_ID = id;
        _tiles[Count].m_Z = z;
        ++Count;
    }

    private void TryResize(int length)
    {
        _tiles ??= new StaticTile[length];

        if (Count + length > _tiles.Length)
        {
            var old = _tiles;
            _tiles = new StaticTile[old.Length * 2];

            for (var i = 0; i < old.Length; ++i)
            {
                _tiles[i] = old[i];
            }
        }
    }

    public StaticTile[] ToArray()
    {
        if (Count == 0)
        {
            return _emptyTiles;
        }

        Array.Resize(ref _tiles, Count);
        var tiles = _tiles;

        _tiles = null;
        Count = 0;

        return tiles;
    }
}
