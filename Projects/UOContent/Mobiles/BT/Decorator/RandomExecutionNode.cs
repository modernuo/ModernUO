using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class RandomExecutionNode : DecoratorNode
    {
        private double activationChance;
        private Dictionary<BaseCreature, bool> executingCache = new Dictionary<BaseCreature, bool>();
        public RandomExecutionNode(BehaviorTree tree, double chance) : base(tree)
        {
            activationChance = chance;
        }
        public RandomExecutionNode(BehaviorTree tree, double chance, BehaviorTreeNode child) : base(tree, child)
        {
            activationChance = chance;
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (!executingCache.TryGetValue(mob, out bool executing))
            {
                executing = false;
                executingCache.Add(mob, executing);
            }

            if (Child != null && (Utility.RandomDouble() < activationChance || executing))
            {
                Result result = Child.Execute(mob, blackboard);

                if (result == Result.Running)
                {
                    executingCache[mob] = true;
                    return Result.Running;
                }

                executingCache[mob] = false;
                return result;
            }
            return Result.Failure;
        }
    }
}
