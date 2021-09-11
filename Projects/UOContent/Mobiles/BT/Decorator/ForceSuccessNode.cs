using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class ForceSuccessNode : DecoratorNode
    {
        public ForceSuccessNode(BehaviorTree tree) : base(tree)
        {
        }
        public ForceSuccessNode(BehaviorTree tree, BehaviorTreeNode child) : base(tree, child)
        {
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (Child != null)
            {
                if (Child.Execute(mob, blackboard) == Result.Running)
                {
                    return Result.Running;
                }

                return Result.Success;
            }
            return Result.Failure;
        }
    }
}
