using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class ConditionNode : LeafNode
    {
        private Predicate<BaseCreature> predicate;
        public ConditionNode(BehaviorTree tree, Predicate<BaseCreature> fn) : base(tree)
        {
            predicate = fn;
        }
        public override Result Execute()
        {
            if (predicate(Tree.Mobile))
            {
                return Result.Success;
            }
            return Result.Failure;
        }
    }
}
