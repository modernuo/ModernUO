using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class RandomSelectorNode : CompositeNode
    {
        private Dictionary<BaseCreature, int> retryCache = new Dictionary<BaseCreature, int>();
        public RandomSelectorNode(BehaviorTree tree) : base(tree)
        {
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (!Tree.Claims.TryGetValue(mob, out BehaviorTreeNode node) || node != this)
            {
                Tree.Claim(mob, this);
            }

            if (!currentTaskCache.TryGetValue(mob, out int currentTask))
            {
                currentTask = Utility.RandomMinMax(0, Children.Count - 1);
                currentTaskCache.Add(mob, currentTask);
            }

            if (!retryCache.TryGetValue(mob, out int currentIteration))
            {
                currentIteration = 0;
                retryCache.Add(mob, currentIteration);
            }

            var result = Children[currentTask].Execute(mob, blackboard);

            if (result == Result.Running)
            {
                return Result.Running;
            }
            else if (result == Result.Failure)
            {
                currentIteration++;
                currentTaskCache[mob] = Utility.RandomMinMax(0, Children.Count - 1);
                if (currentIteration >= Children.Count)
                {
                    retryCache[mob] = 0;
                    Tree.Release(mob);
                    return Result.Failure;
                }
                retryCache[mob] = currentIteration;
                return Result.Running;
            }

            retryCache[mob] = 0;
            currentTaskCache[mob] = 0;
            Tree.Release(mob);
            return Result.Success;
        }
    }
}
