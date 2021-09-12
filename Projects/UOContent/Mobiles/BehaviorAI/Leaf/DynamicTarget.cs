using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Targeting;

namespace Server.Mobiles.BehaviorAI
{
    public delegate object DynamicTargetCallback(BehaviorTreeContext context);
    public class DynamicTarget : Behavior
    {
        public DynamicTargetCallback Transformation { get; private set; }
        public DynamicTarget(BehaviorTree tree, DynamicTargetCallback transformation) : base(tree)
        {
            Transformation = transformation;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if (Transformation != null)
            {
                SetResult(context, Result.Failure);
                return;
            }

            Target target  = context.Mobile.Target;

            if (target == null)
            {
                SetResult(context, Result.Failure);
                return;
            }

            object targeted = Transformation(context);

            if (targeted == null)
            {
                SetResult(context, Result.Failure);
                return;
            }

            target.Invoke(context.Mobile, targeted);
            SetResult(context, Result.Success);
        }
    }
}
