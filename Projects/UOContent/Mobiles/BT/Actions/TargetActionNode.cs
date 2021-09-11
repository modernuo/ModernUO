using Server.Targeting;

namespace Server.Mobiles.BT
{
    public class TargetActionNode : ActionNode
    {
        public TargetActionNode(BehaviorTree tree) : base(tree)
        {
        }

        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            Target target = mob.Target;

            if (target == null)
            {
                mob.DebugSay("Waiting for target...");
                return Result.Running;
            }

            Mobile mobTarget = mob.Combatant;

            if ((target.Flags & TargetFlags.Harmful) != 0 && mobTarget != null)
            {
                if (mobTarget.Deleted || !mobTarget.Alive)
                {
                    mob.DebugSay("Canceling my target because my target is dead or does not exist");
                    target.Cancel(mob, TargetCancelType.Canceled);
                    return Result.Success;
                }

                if ((target.Range == -1 ||
                    mob.InRange(mobTarget, target.Range)) &&
                    mob.CanSee(mobTarget) &&
                    mob.InLOS(mobTarget)
                )
                {
                    mob.DebugSay("Targeting my combatant");
                    target.Invoke(mob, mobTarget);
                    return Result.Success;
                }
            }
            else if ((target.Flags & TargetFlags.Beneficial) != 0)
            {
                mob.DebugSay("Targeting myself");
                target.Invoke(mob, mob);
                return Result.Success;
            }

            return Result.Failure;
        }
    }
}
