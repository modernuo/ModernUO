using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BehaviorAI
{
    public class Cooldown : Decorator
    {
        public TimeSpan Duration { get; }
        private Dictionary<BaseCreature, long> nextCooldownCache;
        public Cooldown(BehaviorTree tree, TimeSpan duration) : base(tree)
        {
            nextCooldownCache = new Dictionary<BaseCreature, long>();
            Duration = duration;
        }
        public Cooldown(BehaviorTree tree, TimeSpan duration, Behavior child) : base(tree, child)
        {
            nextCooldownCache = new Dictionary<BaseCreature, long>();
            Duration = duration;
        }
        public override void Tick(BehaviorTreeContext context)
        {
            if(!nextCooldownCache.TryGetValue(context.Mobile, out long nextCooldown))
            {
                nextCooldown = Core.TickCount;
                nextCooldownCache.Add(context.Mobile, nextCooldown);
            }

            if (Core.TickCount > nextCooldown)
            {
                base.Tick(context);
            }
        }
        public override void OnChildComplete(BehaviorTreeContext context, Result result)
        {
            if (!nextCooldownCache.TryGetValue(context.Mobile, out long nextCooldown))
            {
                nextCooldown = Core.TickCount;
                nextCooldownCache.Add(context.Mobile, nextCooldown);
            }

            if (Child != null)
            {
                if (Child.GetResult(context) == Result.Success)
                {
                    nextCooldownCache[context.Mobile] = Core.TickCount + Duration.Ticks;
                }
                SetResult(context, Child.GetResult(context));
                return;
            }

            SetResult(context, Result.Failure);
        }
    }
}
