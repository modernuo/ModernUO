using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Spells.First;
using Server.Spells.Fourth;

namespace Server.Mobiles.BehaviorTreeAI
{

    public class HealSelfActionNode : BehaviorTreeNode
    {
        public TimeSpan Cooldown { get; private set; }
        public HealSelfActionNode(BehaviorTree tree, TimeSpan cooldown) : base(tree)
        {
            object nextHealTime;
            if (!Tree.Blackboard.TryGetValue("nextHealTime", out nextHealTime))
            {
                tree.Blackboard.Add("nextHealTime", DateTime.Now);
            }
            Cooldown = cooldown;
        }
        public override Result Execute()
        {
            object nextHealTime;
            if (Tree.Mobile.Hits < Tree.Mobile.HitsMax && Tree.Blackboard.TryGetValue("nextHealTime", out nextHealTime))
            {
                if (DateTime.Now >= (DateTime)nextHealTime)
                {
                    if (Tree.Mobile.Hits < Tree.Mobile.HitsMax - 50)
                    {
                        new GreaterHealSpell(Tree.Mobile).Cast();
                        Tree.Mobile.DebugSay("Casting greater heal for myself");
                    }
                    else
                    {
                        new HealSpell(Tree.Mobile).Cast();
                        Tree.Mobile.DebugSay("Casting heal spell for myself");
                    }

                    Tree.Blackboard.Remove("nextHealTime");
                    Tree.Blackboard.Add("nextHealTime", DateTime.Now + Cooldown);

                    return Result.Success;
                }

                Tree.Mobile.DebugSay("I'm hurt, but my healing is on cooldown {0}s", ((DateTime)nextHealTime - DateTime.Now).TotalSeconds);

                return Result.Failure;
            }

            Tree.Mobile.DebugSay("I'm at full health");

            return Result.Failure;
        }
    }
}
