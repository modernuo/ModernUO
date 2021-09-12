using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public class Loop : Decorator
    {
        private Dictionary<BaseCreature, int> currentIterationCache;
        private int totalIterations;
        public Loop(BehaviorTree tree, int n) : base(tree)
        {
            currentIterationCache = new Dictionary<BaseCreature, int>();
            totalIterations = n;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if (!currentIterationCache.TryGetValue(context.Mobile, out int iteration))
            {
                iteration = 0;
                currentIterationCache.Add(context.Mobile, iteration);
            }

            base.Tick(context);
        }
        public override void OnChildComplete(BehaviorTreeContext context, Result result)
        {
            if (!currentIterationCache.TryGetValue(context.Mobile, out int iteration))
            {
                iteration = 0;
                currentIterationCache.Add(context.Mobile, iteration);
            }

            iteration++;

            if (iteration >= totalIterations)
            {
                currentIterationCache[context.Mobile] = 0;
                SetResult(context, Result.Success);
                return;
            }

            currentIterationCache[context.Mobile] = iteration;

            if (Child != null)
            {
                Tree.Enqueue(context, Child, OnChildComplete);
                return;
            }

            currentIterationCache[context.Mobile] = 0;
            SetResult(context, Result.Failure);
        }
    }
}
