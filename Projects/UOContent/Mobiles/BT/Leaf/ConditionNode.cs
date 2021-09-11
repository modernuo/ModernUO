using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public delegate bool ConditionNodePredicate(BaseCreature mob, Blackboard blackboard);
    public class ConditionNode : LeafNode
    {
        private ConditionNodePredicate predicate;
        public ConditionNode(BehaviorTree tree, ConditionNodePredicate fn) : base(tree)
        {
            predicate = fn;
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (predicate(mob, blackboard))
            {
                return Result.Success;
            }
            return Result.Failure;
        }
    }
}
