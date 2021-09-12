using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public delegate bool ConditionalLoopPredicate(BehaviorTreeContext context);
    public class ConditionalLoop : Decorator
    {
        public ConditionalLoopPredicate Predicate { get; private set; }
        public ConditionalLoop(BehaviorTree tree, ConditionalLoopPredicate predicate) : base(tree)
        {
            Predicate = predicate;
        }
        public ConditionalLoop(BehaviorTree tree, ConditionalLoopPredicate predicate, Behavior child) : base(tree, child)
        {
            Predicate = predicate;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if (Predicate(context))
            {
                base.Tick(context);
                return;
            }
            SetResult(context, Result.Failure);
            return;
        }
        public override void OnChildComplete(BehaviorTreeContext context, Result result)
        {
            if (Predicate(context))
            {
                Tree.Enqueue(context, Child, OnChildComplete);
                return;
            }
            SetResult(context, Result.Success);
        }
    }
}
