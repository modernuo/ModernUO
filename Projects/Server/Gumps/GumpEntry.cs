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
        public abstract void AppendTo(NetState ns, IGumpWriter disp);
    }
}
