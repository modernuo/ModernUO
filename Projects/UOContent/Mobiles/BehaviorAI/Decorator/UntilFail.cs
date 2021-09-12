using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public class UntilFail : Decorator
    {
        public UntilFail(BehaviorTree tree): base(tree)
        {
        }
        public UntilFail(BehaviorTree tree, Behavior child) : base(tree, child)
        {
        }
        public override void OnChildComplete(BehaviorTreeContext context, Result result)
        {
            if (Child != null)
            {
                if (Child.GetResult(context) == Result.Failure)
                {
                    SetResult(context, Result.Success);
                    return;
                }
                Tree.Enqueue(context, Child, OnChildComplete);
            }
        }
    }
}
