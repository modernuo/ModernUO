using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Mythik.Systems.Achievements
{
    /// <summary>
    /// Base Achievement Class.
    /// </summary>
    public abstract class BaseAchievement
    {
        public BaseAchievement(int id, int catid, int itemIcon, bool hiddenTillComplete, BaseAchievement prereq, string title, string desc, short rewardPoints, int total, params Type[] rewards)
        {
            ID = id;
            CategoryID = catid;
            Title = title;
            Desc = desc;
            RewardPoints = rewardPoints;
            CompletionTotal = total;
            RewardItems = rewards;
            HiddenTillComplete = hiddenTillComplete;
            ItemIcon = itemIcon;
        }

        /// <summary>
        /// Achievement ID must be Unique
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// ID of this acheivments category
        /// </summary>
        public int CategoryID { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
        public int ItemIcon { get; set; }
        /// <summary>
        /// Number of Points rewarded for completion
        /// </summary>
        public short RewardPoints { get; set; }
        public Type[] RewardItems { get; set; }
        public int CompletionTotal { get; set; }
        public bool HiddenTillComplete { get; private set; }
        public BaseAchievement PreReq { get; set; }
    }
}
