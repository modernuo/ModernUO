using Server.Targeting;

namespace Server.Mobiles.BehaviorAI
{
    public class AutoTarget : Behavior
    {
        public AutoTarget(BehaviorTree tree) : base(tree)
        {
        }
        public override void Tick(BehaviorTreeContext context)
        {
            Target target = context.Mobile.Target;

            if (target == null)
            {
                SetResult(context, Result.Failure);
                return;
            }

            Mobile combatant = context.Mobile.Combatant;

            if ((target.Flags & TargetFlags.Harmful) != 0 && combatant != null)
            {
                if (combatant.Deleted)
                {
                    target.Cancel(context.Mobile, TargetCancelType.Canceled);
                    SetResult(context, Result.Failure);
                    return;
                }

                if ((target.Range == -1 || context.Mobile.InRange(combatant, target.Range)) &&
                    context.Mobile.CanSee(combatant) &&
                    context.Mobile.InLOS(combatant)
                )
                {
                    target.Invoke(context.Mobile, combatant);
                    SetResult(context, Result.Success);
                    return;
                }
            }
            else if ((target.Flags & TargetFlags.Beneficial) != 0)
            {
                target.Invoke(context.Mobile, context.Mobile);
                SetResult(context, Result.Success);
                return;
            }

            SetResult(context, Result.Failure);
        }
    }
}
