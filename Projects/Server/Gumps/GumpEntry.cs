using System.Buffers;
using Server.Collections;
using Server.Network;

namespace Server.Gumps
{
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

        public abstract string Compile(NetState ns);
        public abstract string Compile(IndexList<string> strings);
        public abstract void AppendTo(NetState ns, IGumpWriter disp);
        public abstract void AppendTo(ref SpanWriter writer, IndexList<string> strings, ref int entries, ref int switches);
    }
}
