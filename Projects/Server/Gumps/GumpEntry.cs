using System.Buffers;
using Server.Collections;

namespace Server.Gumps;

public abstract class GumpEntry
{
    private Gump m_Parent;

    public Gump Parent
    {
        get => m_Parent;
        set
        {
            if (m_Parent != value)
            {
                m_Parent?.Remove(this);

                m_Parent = value;

                m_Parent?.Add(this);
            }
        }
    }

    // TODO: Replace OrderedHashSet with InsertOnlyHashSet, a copy of HashSet that is ReadOnly compatible, but includes
    // a public AddIfNotPresent function that returns the index of the element
#if NET7_SDK
    public abstract void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, scoped ref int entries, scoped ref int switches);
#else
    public abstract void AppendTo(ref SpanWriter writer, OrderedHashSet<string> strings, ref int entries, ref int switches);
#endif
}
