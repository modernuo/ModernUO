using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Mobiles.BT;
using Server.Targeting;

namespace Server.Mobiles.AI
{
    public class BehaviorTreeAI : BaseAI
    {
        private Blackboard blackboard;
        public BehaviorTreeAI(BaseCreature m) : base(m)
        {
            blackboard = new Blackboard();
        }

        public override bool Think()
        {
            if (m_Mobile.Deleted)
            {
                return false;
            }

            MageBehaviorTree.Instance.RunBehavior(m_Mobile, blackboard);

            return true;
        }

        public override bool DoActionWander()
        {
            return true;
        }
    }
}
