using System;

namespace Server;

public class KeywordList
{
    private static readonly int[] m_EmptyInts = Array.Empty<int>();
    private int[] m_Keywords;

    public KeywordList()
    {
        m_Keywords = new int[8];
        Count = 0;
    }

    public int Count { get; private set; }

    public bool Contains(int keyword)
    {
        var contains = false;

        for (var i = 0; !contains && i < Count; ++i)
        {
            contains = keyword == m_Keywords[i];
        }

        return contains;
    }

    public void Add(int keyword)
    {
        if (Count + 1 > m_Keywords.Length)
        {
            var old = m_Keywords;
            m_Keywords = new int[old.Length * 2];

            for (var i = 0; i < old.Length; ++i)
            {
                m_Keywords[i] = old[i];
            }
        }

        m_Keywords[Count++] = keyword;
    }

    public int[] ToArray()
    {
        if (Count == 0)
        {
            return m_EmptyInts;
        }

        var keywords = new int[Count];

        for (var i = 0; i < Count; ++i)
        {
            keywords[i] = m_Keywords[i];
        }

        Count = 0;

        return keywords;
    }
}
