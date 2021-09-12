using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public class BehaviorTreeContext
    {
        public BaseCreature Mobile { get; }
        public Blackboard Blackboard { get; }
        public BehaviorTreeContext(BaseCreature mob, Blackboard blackboard)
        {
            Mobile = mob;
            Blackboard = blackboard;
        }
    }
}
