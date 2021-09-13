using Server.Targeting;

namespace Server.Mobiles.BehaviorAI
{
    public class CancelTarget : Behavior
    {
        public CancelTarget(BehaviorTree tree) : base(tree)
        {
        }
        public override void Tick(BehaviorTreeContext context)
        {
            Target target = context.Mobile.Target;

            if (target != null)
            {
                target.Cancel(context.Mobile, TargetCancelType.Canceled);
            }

            SetResult(context, Result.Success);
        }
    }
}
