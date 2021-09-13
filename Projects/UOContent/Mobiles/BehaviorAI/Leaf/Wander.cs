namespace Server.Mobiles.BehaviorAI
{
    public class Wander : Behavior
    {
        public int Range { get; }
        public double Chance { get; }
        public Wander(BehaviorTree tree) : this(tree, 2)
        {
        }
        public Wander(BehaviorTree tree, int range) : this(tree, range, 1.0)
        {
        }
        public Wander(BehaviorTree tree, int range, double chance) : base(tree)
        {
            Range = range;
            Chance = chance;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if (!context.Mobile.CheckIdle() && Utility.RandomDouble() < Chance)
            {
                BehaviorTree.WalkRandomInHome(context, Range);
                SetResult(context, Result.Success);
                return;
            }
            SetResult(context, Result.Failure);
        }
    }
}
