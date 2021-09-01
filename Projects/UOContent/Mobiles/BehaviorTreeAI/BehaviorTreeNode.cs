using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    public abstract class BehaviorTreeNode
    {
        public enum Result { Running, Failure, Success }

        public BehaviorTree Tree { get; private set; }

        public BehaviorTreeNode(BehaviorTree tree)
        {
            Tree = tree;
        }
        public abstract Result Execute();
    }
}
