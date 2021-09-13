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

            currentChild++;
            if (lastResult == Result.Failure)
            {
                currentChildCache[context.Mobile] = 0;
                SetResult(context, Result.Failure);
                return;
            }

            if (currentChild >= Children.Count)
            {
                currentChildCache[context.Mobile] = 0;
                SetResult(context, Result.Success);
                return;
            }

            currentChildCache[context.Mobile] = currentChild;
            SetResult(context, Result.Running);
            Tree.Enqueue(context, Children[currentChild], OnChildComplete);
        }
    }
}
