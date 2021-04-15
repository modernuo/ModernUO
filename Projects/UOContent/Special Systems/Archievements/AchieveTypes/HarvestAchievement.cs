using Server;
using Server.Mobiles;
using System;

namespace Scripts.Systems.Achievements
{
    public class HarvestAchievement : BaseAchievement
    {
        private Type m_Item;
        public HarvestAchievement(int id, int catid, int itemIcon, bool hiddenTillComplete, BaseAchievement prereq, int total, string title, string desc, short RewardPoints, Type targets, params Type[] rewards)
            : base(id, catid, itemIcon, hiddenTillComplete, prereq, title, desc, RewardPoints, total, rewards)
        {
            m_Item = targets;
            EventSink.ResourceHarvestSuccess += EventSink_ResourceHarvestSuccess;
        }

        private void EventSink_ResourceHarvestSuccess(Mobile m, Item tool, Item item, Item bonusItem)
        {
            var player = m as PlayerMobile;
            if (item.GetType() == m_Item)
            {
                AchievementSystem.SetAchievementStatus(player, this, item.Amount);
            }
        }
    }
}
