using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class ConditionalLoopNode : DecoratorNode
    {
        public delegate bool ConditionalLoopPredicate(BaseCreature mob, Blackboard blackboard);
        private ConditionalLoopPredicate predicate;
        public ConditionalLoopNode(BehaviorTree tree, ConditionalLoopPredicate fn) : base(tree)
        {
            predicate = fn;
        }

        public ConditionalLoopNode(BehaviorTree tree, ConditionalLoopPredicate fn, BehaviorTreeNode child) : base(tree, child)
        {
            predicate = fn;
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (predicate(mob, blackboard))
            {
                if (Child != null)
                {
                    Child.Execute(mob, blackboard);
                    return Result.Running;
                }
            }

            return Result.Failure;
        }
    }
}
