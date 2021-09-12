using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public delegate bool OwnerConditionPredicate(BehaviorTreeContext context);
    public class OwnerCondition : Decorator
    {
        public OwnerConditionPredicate Predicate { get; }
        public OwnerCondition(BehaviorTree tree, OwnerConditionPredicate predicate) : base(tree)
        {
            Predicate = predicate;
        }
        public OwnerCondition(BehaviorTree tree, OwnerConditionPredicate predicate, Behavior child) : base(tree, child)
        {
            Predicate = predicate;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if (Predicate(context))
            {
                base.Tick(context);
            }
        }
    }
}
