using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public abstract class BehaviorTreeNode
    {
        public enum Result { Running, Failure, Success }
        public BehaviorTreeNode Parent;
        public BehaviorTree Tree { get; private set; }
        public BehaviorTreeNode(BehaviorTree tree) : this(tree, null)
        {
        }
        public BehaviorTreeNode(BehaviorTree tree, BehaviorTreeNode parent)
        {
            Parent = parent;
            Tree = tree;
        }
        public virtual Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            return Result.Failure;
        }
    }
}
