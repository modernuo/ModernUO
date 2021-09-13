namespace Server.Mobiles.BehaviorAI
{
    public class BehaviorTreeContext
    {
        public BaseCreature Mobile { get; }
        public Blackboard Blackboard { get; }
        public BehaviorTreeContext(BaseCreature mob, Blackboard blackboard)
        {
            Mobile = mob;
            Blackboard = blackboard;
        }
    }
}
