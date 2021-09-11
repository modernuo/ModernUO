using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class UseSkillNode : ActionNode
    {
        private SkillName skillToUse;
        public UseSkillNode(BehaviorTree tree, SkillName skill) : base(tree)
        {
            skillToUse = skill;
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (mob.UseSkill(skillToUse))
            {
                mob.Say("{0}", skillToUse.ToString());
                return Result.Success;
            }
            return Result.Failure;
        }
    }
}
