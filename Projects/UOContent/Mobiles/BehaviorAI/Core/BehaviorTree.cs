using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public class BehaviorTree
    {
        public Behavior Root { get; private set; }
        private Dictionary<BaseCreature, Queue<BehaviorQueueEntry>> behaviorQueueCache;
        private Dictionary<BaseCreature, bool> executingCache;
        public BehaviorTree()
        {
            behaviorQueueCache = new Dictionary<BaseCreature, Queue<BehaviorQueueEntry>>();
            executingCache = new Dictionary<BaseCreature, bool>();
        }
        public bool TryAddRoot(Composite behavior)
        {
            if (Root == null)
            {
                Root = behavior;
                return true;
            }
            return false;
        }
        public void Start(BehaviorTreeContext context)
        {
            if (Root != null)
            {
                Enqueue(context, Root, ExecutionFinished);
            }
        }
        public void Stop(BehaviorTreeContext context)
        {
            getQueue(context).Clear();
        }
        public virtual void Tick(BehaviorTreeContext context)
        {
            if (!executingCache.TryGetValue(context.Mobile, out bool executing))
            {
                executing = false;
                executingCache[context.Mobile] = executing;
            }

            if (!executing)
            {
                executingCache[context.Mobile] = true;
                Queue<BehaviorQueueEntry> queue = getQueue(context);
                queue.Enqueue(null);
                while (Step(context))
                {
                }
                executingCache[context.Mobile] = false;
            }
        }
        public virtual bool Step(BehaviorTreeContext context)
        {
            Queue<BehaviorQueueEntry> queue = getQueue(context);

            BehaviorQueueEntry current = queue.Dequeue();

            if (current == null || current.Behavior == null || current.Context == null)
            {
                return false;
            }

            current.Behavior.Tick(current.Context);

            if (!current.Behavior.IsRunning(current.Context))
            {
                if (current.Observer != null)
                {
                    current.Observer(current.Context, current.Behavior.GetResult(current.Context));
                }
                return true;
            }

            queue.Enqueue(current);

            return false;
        }
        public virtual void ExecutionFinished(BehaviorTreeContext context, Result result)
        {
        }
        public void Enqueue(BehaviorTreeContext context, Behavior behavior, BehaviorObserver observer)
        {
            if (!behaviorQueueCache.TryGetValue(context.Mobile, out Queue<BehaviorQueueEntry> queue))
            {
                queue = new Queue<BehaviorQueueEntry>();
                behaviorQueueCache.Add(context.Mobile, queue);
            }

            queue.Enqueue(new BehaviorQueueEntry(context, behavior, observer));
        }
        private Queue<BehaviorQueueEntry> getQueue(BehaviorTreeContext context)
        {
            if (!behaviorQueueCache.TryGetValue(context.Mobile, out Queue<BehaviorQueueEntry> queue))
            {
                queue = new Queue<BehaviorQueueEntry>();
                behaviorQueueCache.Add(context.Mobile, queue);
            }

            return queue;
        }
    }
}
