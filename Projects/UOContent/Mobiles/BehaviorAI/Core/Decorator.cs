using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public class Decorator : Behavior
    {
        public Behavior Child { get; private set; }
        public Decorator(BehaviorTree tree) : base(tree)
        {
        }
        public Decorator(BehaviorTree tree, Behavior child) : base(tree)
        {
            Child = child;
        }
        public virtual bool AddChild(Behavior child)
        {
            if (Child == null)
            {
                Child = child;
                return true;
            }
            return false;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if (Child != null && GetResult(context) != Result.Running)
            {
                Tree.Enqueue(context, Child, OnChildComplete);
                SetResult(context, Result.Running);
                return;
            }
        }
        public virtual void OnChildComplete(BehaviorTreeContext context, Result result)
        {
            SetResult(context, Child.GetResult(context));
        }
    }
}
