using System.Collections.Generic;

namespace Server.Mobiles.BT
{
    public class LoopNode : DecoratorNode
    {
        private int count;
        private Dictionary<BaseCreature, int> currentCountCache;
        public LoopNode(BehaviorTree tree, int n) : base(tree)
        {
            count = n;
            currentCountCache = new Dictionary<BaseCreature, int>();
        }
        public LoopNode(BehaviorTree tree, int n, BehaviorTreeNode child) : base(tree, child)
        {
            count = n;
            currentCountCache = new Dictionary<BaseCreature, int>();
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (!currentCountCache.TryGetValue(mob, out int currentCount))
            {
                currentCount = 0;
                currentCountCache.Add(mob, currentCount);
            }

            if (Child != null && currentCount < count)
            {
                Child.Execute(mob, blackboard);
                currentCountCache[mob]++;
                return Result.Running;
            }
            else if (currentCount >= count)
            {
                currentCountCache[mob] = 0;
                return Result.Success;
            }

            return Result.Failure;
        }
    }
}
