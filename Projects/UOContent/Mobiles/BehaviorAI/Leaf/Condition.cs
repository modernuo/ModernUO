namespace Server.Mobiles.BehaviorAI
{
    public delegate bool ConditionPredicate(BehaviorTreeContext context);
    public class Condition : Behavior
    {
        public ConditionPredicate Predicate { get; }
        public Condition(BehaviorTree tree, ConditionPredicate predicate) : base(tree)
        {
            Predicate = predicate;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if(Predicate(context))
            {
                SetResult(context, Result.Success);
            }
            else
            {
                SetResult(context, Result.Failure);
            }
        }
    }
}
