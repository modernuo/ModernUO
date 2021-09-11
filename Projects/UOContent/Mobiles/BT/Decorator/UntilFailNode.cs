using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class UntilFailNode : DecoratorNode
    {
        public UntilFailNode(BehaviorTree tree) : base(tree)
        {
        }
        public UntilFailNode(BehaviorTree tree, BehaviorTreeNode child) : base(tree, child)
        {
        }

        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (Child != null)
            {
                if(Child.Execute(mob, blackboard) == Result.Failure)
                {
                    return Result.Failure;
                }
                return Result.Running;
            }
            return Result.Failure;
        }
    }
}
