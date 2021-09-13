
namespace Server.Mobiles.BehaviorAI
{
    public class BehaviorQueueEntry
    {
        public BehaviorTreeContext Context { get; }
        public BehaviorObserver Observer { get; }
        public Behavior Behavior { get; }
        public BehaviorQueueEntry(BehaviorTreeContext context, Behavior behavior, BehaviorObserver observer)
        {
            Context = context;
            Behavior = behavior;
            Observer = observer;
        }
    }
}
