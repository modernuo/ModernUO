
namespace Server.Mobiles.BT
{
    public class WanderNode : ActionNode
    {
        private int stepsPerWander;
        public WanderNode(BehaviorTree tree, int steps) : base(tree)
        {
            stepsPerWander = steps;
        }
        public override Result Execute(BaseCreature mob, Blackboard blackboard)
        {
            if (!mob.CheckIdle())
            {
                BehaviorTree.WalkRandomInHome(mob, stepsPerWander);
                return Result.Success;
            }
            return Result.Failure;
        }
    }
}
