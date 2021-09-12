using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public delegate void BehaviorObserver(BehaviorTreeContext context, Result lastResult);
    public enum Result
    {
        Success,
        Failure,
        Running
    }
    public abstract class Behavior
    {
        public BehaviorTree Tree { get; protected set; }
        public BehaviorObserver Observer { get; protected set; }
        private Dictionary<BaseCreature, Result> lastResultCache;
        public virtual bool IsRunning(BehaviorTreeContext context) => GetResult(context) == Result.Running;
        public Behavior(BehaviorTree tree)
        {
            Tree = tree;
            lastResultCache = new Dictionary<BaseCreature, Result>();
        }
        public virtual void Tick(BehaviorTreeContext context)
        {
        }
        public virtual void Execute(BehaviorTreeContext context)
        {
        }
        public Result GetResult(BehaviorTreeContext context)
        {
            if (!lastResultCache.TryGetValue(context.Mobile, out Result result))
            {
                result = Result.Failure;
                lastResultCache.Add(context.Mobile, result);
            }
            return result;
        }
        public void SetResult(BehaviorTreeContext context, Result result)
        {
            if (!lastResultCache.TryGetValue(context.Mobile, out Result old))
            {
                lastResultCache.Add(context.Mobile, Result.Failure);
            }

            lastResultCache[context.Mobile] = result;
        }
    }
}
