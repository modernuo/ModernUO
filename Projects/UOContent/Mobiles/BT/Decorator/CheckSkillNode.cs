using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class CheckSkillNode : DecoratorNode
    {
        private SkillName checkSkill;
        private double minSkillValue;
        private double maxSkillValue;

        public CheckSkillNode(BehaviorTree tree, SkillName skill, double minSkill, double maxSkill) : base(tree)
        {
            checkSkill = skill;
            minSkillValue = minSkill;
            maxSkillValue = maxSkill;
        }

        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if(mob.CheckSkill(checkSkill, minSkillValue, maxSkillValue) && Child != null)
            {
                return Child.Execute(mob, blackboard);
            }

            return Result.Failure;
        }
    }
}
