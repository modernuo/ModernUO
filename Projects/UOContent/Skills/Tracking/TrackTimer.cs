using System;

namespace Server.SkillHandlers
{
    public class TrackTimer : Timer
    {
        private readonly QuestArrow m_Arrow;
        private readonly Mobile m_From;
        private readonly int m_Range;
        private readonly Mobile m_Target;
        private int m_LastX, m_LastY;

        public TrackTimer(Mobile from, Mobile target, int range, QuestArrow arrow) : base(
            TimeSpan.FromSeconds(0.25),
            TimeSpan.FromSeconds(2.5)
        )
        {
            m_From = from;
            m_Target = target;
            m_Range = range;

            m_Arrow = arrow;
        }

        protected override void OnTick()
        {
            if (!m_Arrow.Running)
            {
                Stop();
                return;
            }

            if (m_From.NetState == null || m_From.Deleted || m_Target.Deleted || m_From.Map != m_Target.Map ||
                !m_From.InRange(m_Target, m_Range) || m_Target.Hidden && m_Target.AccessLevel > m_From.AccessLevel)
            {
                m_Arrow.Stop();
                Stop();
                return;
            }

            if (m_LastX != m_Target.X || m_LastY != m_Target.Y)
            {
                m_LastX = m_Target.X;
                m_LastY = m_Target.Y;

                m_Arrow.Update();
            }
        }
    }
}
