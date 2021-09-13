namespace Server.Mobiles.BehaviorAI
{
    public class Inverter : Decorator
    {
        public Inverter(BehaviorTree tree) : base(tree)
        {
        }
        public Inverter(BehaviorTree tree, Behavior child) : base(tree, child)
        {
        }
        public override void OnChildComplete(BehaviorTreeContext context, Result result)
        {
            if (result == Result.Success)
            {
                SetResult(context, Result.Failure);
            }
            else
            {
                SetResult(context, Result.Success);
            }
        }
    }
}
