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

    public abstract void AppendTo(ref SpanWriter writer, OrderedSet<string> strings, ref int entries, ref int switches);
}
