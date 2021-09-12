using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
