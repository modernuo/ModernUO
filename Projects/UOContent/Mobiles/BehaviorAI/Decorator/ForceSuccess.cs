using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public class ForceSuccess : Decorator
    {
        public ForceSuccess(BehaviorTree tree) : base(tree)
        {
        }
        public ForceSuccess(BehaviorTree tree, Behavior child) : base(tree, child)
        {
        }
        public override void OnChildComplete(BehaviorTreeContext context, Result result)
        {
            SetResult(context, Result.Success);
        }
    }
}

