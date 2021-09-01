using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class AlwaysSucceedNode : DecoratorNode
    {
        public AlwaysSucceedNode(BehaviorTree tree, BehaviorTreeNode child) : base(tree, child)
        {
        }
        public AlwaysSucceedNode(BehaviorTree tree) : base(tree)
        {
        }

        public override Result Execute()
        {
            if (Child != null)
            {
                Child.Execute();
            }
            return Result.Success;
        }
    }
}
