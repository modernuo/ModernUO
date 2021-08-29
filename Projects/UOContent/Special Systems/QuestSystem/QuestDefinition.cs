using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.QuestSystem
{
    public class QuestDefinition
    {
        public int ID { get; set; } = 0;
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public List<RewardDefinition> Rewards { get; set; } = new List<RewardDefinition>();
        public int XP { get; set; } = 0;
        public bool IsQuestChainElement { get; set; } = false;
        public int ParentQuestID { get; set; } = 0;
        public List<QuestTaskDefinition> QuestTaskList { get; set; } = new List<QuestTaskDefinition>();

       
    }
}
