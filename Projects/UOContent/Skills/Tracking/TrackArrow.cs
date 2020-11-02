using Server.Mobiles;

namespace Server.SkillHandlers
{
    public class TrackArrow : QuestArrow
    {
        private readonly Timer m_Timer;
        private Mobile m_From;

        public TrackArrow(PlayerMobile from, Mobile target, int range) : base(from, target)
        {
            m_From = from;
            m_Timer = new TrackTimer(from, target, range, this);
            m_Timer.Start();
        }

        public override void OnClick(bool rightClick)
        {
            if (rightClick)
            {
                Tracking.ClearTrackingInfo(m_From);

                m_From = null;

                Stop();
            }
        }

        public override void OnStop()
        {
            m_Timer.Stop();

            if (m_From != null)
            {
                Tracking.ClearTrackingInfo(m_From);

                m_From.SendLocalizedMessage(503177); // You have lost your quarry.
            }
        }
    }
}
