using System;

namespace Server.Mobiles.BT
{
    public sealed class MageBehaviorTree : BehaviorTree
    {
        private static MageBehaviorTree instance;

        private MageBehaviorTree()
        {
        }
        public static MageBehaviorTree Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MageBehaviorTree();
                    instance.InitTree();
                }
                return instance;
            }
        }
        private void InitTree()
        {
            TryAddRoot(
                // do the first thing that succeeds of the three children
                new SelectorNode(this)
                    .AddChild(new TapNode(this, (mob, board) => mob.DebugSay("Thinking...")))
                    .AddChild(new BehaviorMageCombatNodeSet(this))
                    .AddChild(new BehaviorMagePassiveNodeSet(this))
            );
        }
    }
}
