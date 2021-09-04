using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.QuestSystemAdvanced
{
    public enum Type
    {
        Seek,
        Kill,
        Collect,
        Discover
    }
    public class QuestTaskDefinition
    {
        public int ID { get; set; } = 0;
        public string Type { get; set; } = "";
        public int EntityName { get; set; } = 0;
        public int Amount { get; set; } = 0;

              
        
    }
}
