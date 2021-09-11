using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class OwnerConditionNode : DecoratorNode
    {
        public delegate bool OwnerConditionPredicate(BaseCreature mob, Blackboard blackboard);
        private OwnerConditionPredicate predicate;
        public OwnerConditionNode(BehaviorTree tree, OwnerConditionPredicate fn) : base(tree)
        {
            predicate = fn;
        }
        public OwnerConditionNode(BehaviorTree tree, OwnerConditionPredicate fn, BehaviorTreeNode child) : base(tree, child)
        {
            predicate = fn;
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (predicate(mob, blackboard))
            {
                if (Child != null)
                {
                    return Child.Execute(mob, blackboard);
                }
                return Result.Success;
            }
            return Result.Failure;
        }
    }
}
