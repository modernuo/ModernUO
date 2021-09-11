using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class CooldownNode : DecoratorNode
    {
        private TimeSpan cooldownDuration;
        private Dictionary<BaseCreature, DateTime> cooldowns;
        public CooldownNode(BehaviorTree tree, TimeSpan duration) : base(tree)
        {
            cooldownDuration = duration;
            cooldowns = new Dictionary<BaseCreature, DateTime>();
        }
        public CooldownNode(BehaviorTree tree, TimeSpan duration, BehaviorTreeNode child) : base(tree, child)
        {
            cooldownDuration = duration;
            cooldowns = new Dictionary<BaseCreature, DateTime>();
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (Child != null)
            {
                if (!cooldowns.TryGetValue(mob, out DateTime nextCooldown))
                {
                    nextCooldown = DateTime.Now;
                    cooldowns.Add(mob, nextCooldown);
                }

                if (DateTime.Now >= nextCooldown)
                {
                    Result result = Child.Execute(mob, blackboard);

                    if (result == Result.Running)
                    {
                        return Result.Running;
                    }
                    else
                    {
                        cooldowns[mob] = DateTime.Now + cooldownDuration;
                    }
                }
            }

            return Result.Failure;
        }
    }
}
