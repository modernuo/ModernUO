using Server.Targeting;

namespace Server.Mobiles.BehaviorAI
{
    public delegate object DynamicTargetCallback(BehaviorTreeContext context);
    public class DynamicTarget : Behavior
    {
        public DynamicTargetCallback Callback { get; private set; }
        public DynamicTarget(BehaviorTree tree, DynamicTargetCallback transformation) : base(tree)
        {
            Callback = transformation;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if (Callback == null)
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

            object targeted = Callback(context);

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
