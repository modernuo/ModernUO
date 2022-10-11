using System;

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

    public void Add(ushort id, sbyte z)
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

        m_Tiles[Count].m_ID = id;
        m_Tiles[Count].m_Z = z;
        ++Count;
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
