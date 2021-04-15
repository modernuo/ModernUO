using Server;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Systems.Achievements.AchieveTypes
{
    /// <summary>
    /// Achievement to handle hitting X in skill Y
    /// Comment out if you are missing the SkillGain eventsink
    /// </summary>
    class SkillProgressAchievement : BaseAchievement
    {
        private SkillName m_Skill;
        public SkillProgressAchievement(int id, int catid, int itemIcon, bool hiddenTillComplete, BaseAchievement prereq, int total, string title, string desc, SkillName skill, short RewardPoints, params Type[] rewards)
            : base(id, catid, itemIcon, hiddenTillComplete, prereq, title, desc, RewardPoints, total, rewards)
        {
            m_Skill = skill;
            //ServUO is missing this event sink, you can create it your self and reenable if you wish.

            //EventSink.SkillGain += EventSink_SkillGain;
           
        }

        /*private void EventSink_SkillGain(SkillGainEventArgs e)
        {
            if (e.From is PlayerMobile && e.Skill.SkillID == (int)m_Skill)
            {
                if (e.Skill.BaseFixedPoint >= CompletionTotal)
                    AchievmentSystem.SetAchievementStatus(e.From as PlayerMobile, this, e.Skill.BaseFixedPoint);
            }
        }*/


    }
}
