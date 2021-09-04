using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.QuestSystemAdvanced
{
    public class RewardDefinition
    {
        public string ItemName { get; set; } = "";
        public int Amount { get; set; } = 0;
        public bool DropToBackpack { get; set; } = false;
    }
}
