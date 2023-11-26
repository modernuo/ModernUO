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
using Server.Buffers;

namespace Server;

public class TileList
{
    private static readonly StaticTile[] _emptyTiles = Array.Empty<StaticTile>();
    private StaticTile[] _tiles;

    public int Count { get; private set; }

    public void AddRange(ReadOnlySpan<StaticTile> tiles)
    {
        if (tiles.Length == 0)
        {
            return;
        }

        TryResize(tiles.Length);
        tiles.CopyTo(_tiles.AsSpan(Count));
        Count += tiles.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Add(StaticTile* ptr) => Add(Marshal.PtrToStructure<StaticTile>((nint)ptr));

    public void Add(StaticTile tile)
    {
        TryResize(1);
        _tiles[Count++] = tile;
    }

    public void Add(ushort id, byte x, byte y, sbyte z, short hue = 0)
    {
        TryResize(1);
        ref var tile = ref _tiles[Count++];
        tile.m_ID = id;
        tile.m_X = x;
        tile.m_Y = y;
        tile.m_Z = z;
        tile.m_Hue = hue;
    }

    public void Add(ushort id, sbyte z)
    {
        TryResize(1);
        ref var tile = ref _tiles[Count++];
        tile.m_ID = id;
        tile.m_X = 0;
        tile.m_Y = 0;
        tile.m_Z = z;
        tile.m_Hue = 0;
    }

    private void TryResize(int length)
    {
        _tiles ??= STArrayPool<StaticTile>.Shared.Rent(length);

        var newLength = Count + length;
        if (newLength > _tiles.Length)
        {
            var old = _tiles;
            _tiles = STArrayPool<StaticTile>.Shared.Rent(newLength);
            old.CopyTo(_tiles.AsSpan());
            STArrayPool<StaticTile>.Shared.Return(old);
        }
    }

    public StaticTile[] ToArray()
    {
        if (Count == 0)
        {
            return _emptyTiles;
        }

        var tiles = new StaticTile[Count];
        _tiles.AsSpan(0, Count).CopyTo(tiles);

        // Cleanup
        STArrayPool<StaticTile>.Shared.Return(_tiles);
        _tiles = null;
        Count = 0;

        return tiles;
    }
}
