using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public delegate void TapAction(BehaviorTreeContext context);
    public class Tap : Behavior
    {
        public TapAction Action { get; private set; }
        public Tap(BehaviorTree tree, TapAction action) : base(tree)
        {
            Action = action;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            Action(context);
            SetResult(context, Result.Failure);
        }
    }
}
