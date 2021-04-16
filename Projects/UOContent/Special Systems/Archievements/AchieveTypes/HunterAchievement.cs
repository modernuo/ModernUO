using Server;
using Server.Mobiles;
using System;


namespace Scripts.Systems.Achievements
{
    public class HunterAchievement : BaseAchievement
    {
        private Type m_Mobile;
        private BaseAchievement m_Prereq;

        public HunterAchievement(
            int id, int catid, int itemIcon, bool hiddenTillComplete, BaseAchievement prereq, int total, string title,
            string desc, short RewardPoints, Type targets, params Type[] rewards
        )
            : base(id, catid, itemIcon, hiddenTillComplete, prereq, title, desc, RewardPoints, total, rewards)
        {
            m_Mobile = targets;
            m_Prereq = prereq;
            EventSink.OnKilledBy += EventSink_OnKilledBy;
        }

        private void EventSink_OnKilledBy(Mobile killed, Mobile killedBy)
        {
            var player = killedBy as PlayerMobile;
            if (player != null && killed.GetType() == m_Mobile)
            {
                if (m_Prereq == null || AchievementSystem.GetArchievementPoints(player, this) >= m_Prereq.RewardPoints)
                {
                    AchievementSystem.SetAchievementStatus(player, this, 1);
                }
            }
        }
    }
}
