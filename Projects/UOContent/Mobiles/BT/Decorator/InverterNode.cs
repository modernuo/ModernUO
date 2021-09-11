using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class InverterNode : DecoratorNode
    {
        public InverterNode(BehaviorTree tree) : base(tree)
        {
        }
        public InverterNode(BehaviorTree tree, BehaviorTreeNode child) : base(tree, child)
        {
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (Child != null)
            {
                var result = Child.Execute(mob, blackboard);
                switch (result)
                {
                    case Result.Failure:
                        return Result.Success;
                    case Result.Success:
                        return Result.Failure;
                    default:
                        return Result.Running;
                }
            }

            return Result.Failure;
        }
    }
}
