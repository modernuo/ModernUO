using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class KeepRangeNode : ActionNode
    {
        private int minRange;
        private int maxRange;
        public KeepRangeNode(BehaviorTree tree, int min, int max) : base(tree)
        {
            minRange = min;
            maxRange = max;
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (mob.Combatant != null && !mob.Combatant.Deleted)
            {
                BehaviorTree.WalkMobileRange(mob, (BaseCreature)mob.Combatant, 4, true, minRange, maxRange);
                return Result.Success;
            }

            return Result.Failure;
        }
    }
}
