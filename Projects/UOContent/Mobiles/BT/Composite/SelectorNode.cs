using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class SelectorNode : CompositeNode
    {
        public SelectorNode(BehaviorTree tree): base(tree)
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
                currentTask = 0;
                currentTaskCache.Add(mob, currentTask);
            }

            if (currentTask < Children.Count)
            {
                var result = Children[currentTask].Execute(mob, blackboard);

                if (result == Result.Success)
                {
                    currentTaskCache[mob] = 0;
                    Tree.Release(mob);
                    return Result.Success;
                }
                else if (result == Result.Running)
                {
                    return Result.Running;
                }

                currentTask++;
                if (currentTask == Children.Count)
                {
                    currentTask = 0;
                    currentTaskCache[mob] = currentTask;
                    Tree.Release(mob);
                    return Result.Failure;
                }

                currentTaskCache[mob] = currentTask;
                return Result.Running;
            }

            Tree.Release(mob);
            return Result.Failure;
        }
    }
}
