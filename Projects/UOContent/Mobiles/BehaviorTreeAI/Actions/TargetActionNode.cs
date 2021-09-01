using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Spells;
using Server.Targeting;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class TargetActionNode : ActionNode
    {
        public TargetActionNode(BehaviorTree tree) : base(tree)
        {
        }

        public override Result Execute()
        {
            Target target = Tree.Mobile.Target;

            if (target == null)
            {
                Tree.Mobile.DebugSay("Waiting for target...");
                return Result.Running;
            }

            Mobile mobTarget = Tree.Mobile.Combatant;

            if ((target.Flags & TargetFlags.Harmful) != 0 && mobTarget != null)
            {
                if (mobTarget.Deleted || !mobTarget.Alive)
                {
                    Tree.Mobile.DebugSay("Canceling my target because my target is dead or does not exist");
                    target.Cancel(Tree.Mobile, TargetCancelType.Canceled);
                    return Result.Success;
                }

                if ((target.Range == -1 ||
                    Tree.Mobile.InRange(mobTarget, target.Range)) &&
                    Tree.Mobile.CanSee(mobTarget) &&
                    Tree.Mobile.InLOS(mobTarget)
                )
                {
                    Tree.Mobile.DebugSay("Targeting my combatant");
                    target.Invoke(Tree.Mobile, mobTarget);
                    return Result.Success;
                }
            }
            else if ((target.Flags & TargetFlags.Beneficial) != 0)
            {
                Tree.Mobile.DebugSay("Targeting myself");
                target.Invoke(Tree.Mobile, Tree.Mobile);
                return Result.Success;
            }

            return Result.Failure;
        }
    }
}
