using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public class WaitForTarget : Behavior
    {
        public WaitForTarget(BehaviorTree tree) : base(tree)
        {
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if (context.Mobile.Target != null)
            {
                SetResult(context, Result.Success);
                return;
            }
            SetResult(context, Result.Running);
        }
    }
}
