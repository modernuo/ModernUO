using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class RepeaterNode : DecoratorNode
    {
        public RepeaterNode(BehaviorTree tree) : base(tree)
        {
        }

        public RepeaterNode(BehaviorTree tree, BehaviorTreeNode child) : base(tree, child)
        {
        }

        public override Result Execute()
        {
            if (Child != null)
            {
                Child.Execute();
            }
            return Result.Running;
        }
    }
}
