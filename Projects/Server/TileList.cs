using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Server;

public class TileList
{
    private static readonly StaticTile[] m_EmptyTiles = Array.Empty<StaticTile>();
    private StaticTile[] m_Tiles;

    public TileList()
    {
        m_Tiles = new StaticTile[8];
        Count = 0;
    }

    public int Count { get; private set; }

    public void AddRange(StaticTile[] tiles)
    {
        if (Count + tiles.Length > m_Tiles.Length)
        {
            var old = m_Tiles;
            m_Tiles = new StaticTile[(Count + tiles.Length) * 2];

            for (var i = 0; i < old.Length; ++i)
            {
                m_Tiles[i] = old[i];
            }
        }

        for (var i = 0; i < tiles.Length; ++i)
        {
            m_Tiles[Count++] = tiles[i];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void Add(StaticTile* ptr) => Add(Marshal.PtrToStructure<StaticTile>((nint)ptr));

    public void Add(StaticTile tile)
    {
        TryResize();
        m_Tiles[Count] = tile;
        ++Count;
    }

    public void Add(ushort id, byte x, byte y, sbyte z, short hue = 0)
    {
        TryResize();
        ref var tile = ref m_Tiles[Count];
        tile.m_ID = id;
        tile.m_X = x;
        tile.m_Y = y;
        tile.m_Z = z;
        tile.m_Hue = hue;
        ++Count;
    }

    public void Add(ushort id, sbyte z)
    {
        TryResize();
        m_Tiles[Count].m_ID = id;
        m_Tiles[Count].m_Z = z;
        ++Count;
    }

    private void TryResize()
    {
        if (Count + 1 > m_Tiles.Length)
        {
            var old = m_Tiles;
            m_Tiles = new StaticTile[old.Length * 2];

            for (var i = 0; i < old.Length; ++i)
            {
                m_Tiles[i] = old[i];
            }
        }
    }

    public StaticTile[] ToArray()
    {
        if (Count == 0)
        {
            return m_EmptyTiles;
        }

        var tiles = new StaticTile[Count];

        for (var i = 0; i < Count; ++i)
        {
            tiles[i] = m_Tiles[i];
        }

        Count = 0;

        return tiles;
    }
}
