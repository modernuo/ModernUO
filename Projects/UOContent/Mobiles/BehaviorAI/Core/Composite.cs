using System.Collections.Generic;

namespace Server.Mobiles.BehaviorAI
{
    public class Composite : Behavior
    {
        public List<Behavior> Children { get; private set; }
        protected Dictionary<BaseCreature, int> currentChildCache;
        public Composite(BehaviorTree tree) : base(tree)
        {
            Children = new List<Behavior>();
            currentChildCache = new Dictionary<BaseCreature, int>();
        }
        public Composite AddChild(Behavior child)
        {
            if (Children != null && child != null)
            {
                Children.Add(child);
            }
            return this;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if (!currentChildCache.TryGetValue(context.Mobile, out int currentChild))
            {
                currentChild = 0;
                currentChildCache.Add(context.Mobile, currentChild);
            }

            if (GetResult(context) != Result.Terminated)
            {
                currentChild = 0;
                currentChildCache[context.Mobile] = currentChild;
                SetResult(context, Result.Running);
                Tree.Enqueue(context, Children[currentChild], OnChildComplete);
            }
        }
        public virtual void OnChildComplete(BehaviorTreeContext context, Result result)
        {
            SetResult(context, Result.Failure);
        }
    }
}
