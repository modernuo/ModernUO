using Server;
using Server.Mobiles;
using System;


namespace Scripts.Systems.Achievements
{
    public class DiscoveryAchievement : BaseAchievement
    {
        private string m_Region;
        public DiscoveryAchievement(int id, int catid, int itemIcon, bool hiddenTillComplete, BaseAchievement prereq, string title, string desc, short RewardPoints, string region, params Type[] rewards)
            : base(id, catid, itemIcon, hiddenTillComplete, prereq, title, desc, RewardPoints, 1, rewards)
        {
            m_Region = region;
            CompletionTotal = 1;
            EventSink.OnEnterRegion += EventSink_OnEnterRegion;
        }

        private void EventSink_OnEnterRegion(Mobile m, Region oldRegion, Region newRegion)
        {
            if (m == null || newRegion == null || newRegion.Name == null)
                return;
            var player = m as PlayerMobile;
            if (newRegion.Name.Contains(m_Region) && player != null)
            {
                AchievementSystem.SetAchievementStatus(player, this, 1);

            }
        }
    }

}
