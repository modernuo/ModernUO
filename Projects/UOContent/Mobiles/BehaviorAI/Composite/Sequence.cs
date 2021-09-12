using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public class Sequence : Composite
    {
        public Sequence(BehaviorTree tree) : base(tree)
        {
        }
        public override void OnChildComplete(BehaviorTreeContext context, Result lastResult)
        {
            if (!currentChildCache.TryGetValue(context.Mobile, out int currentChild))
            {
                currentChild = 0;
                currentChildCache.Add(context.Mobile, currentChild);
            }

            Behavior child = Children[currentChild];

            if (lastResult == Result.Failure)
            {
                currentChildCache[context.Mobile] = 0;
                SetResult(context, Result.Failure);
                return;
            }

            currentChild++;
            if (currentChild >= Children.Count)
            {
                currentChildCache[context.Mobile] = 0;
                SetResult(context, Result.Success);
                return;
            }
            else
            {
                SetResult(context, Result.Running);
                currentChildCache[context.Mobile] = currentChild;
                if (Children[currentChild] == null)
                {
                    throw new NullReferenceException();
                }
                Tree.Enqueue(context, Children[currentChild], OnChildComplete);
            }
        }
    }
}
