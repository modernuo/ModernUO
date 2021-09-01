using System;
using Server.Spells;
using Server.Spells.Second;
using System.Collections.Generic;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class CureSelfNode : SequenceNode
    {
        public CureSelfNode(BehaviorTree tree) : base(tree)
        {
            Children = new List<BehaviorTreeNode>(new BehaviorTreeNode[5]
            {
                new ConditionNode(
                    tree,
                    (mob) =>
                    {
                        if (mob.Poison == null)
                        {
                            mob.DebugSay("I am not poisoned");
                            return false;
                        }

                        object nextHealTime;
                        if (Tree.Blackboard.TryGetValue("nextHealTime", out nextHealTime))
                        {
                            if (DateTime.Now >= (DateTime)nextHealTime)
                            {
                                return true;
                            }
                            mob.DebugSay("My heal cooldown is active");
                        }

                        mob.DebugSay("My heal cooldown is not set");

                        return false;
                    }
                ),
                new ConditionNode(
                    tree,
                    (mob) =>
                    {
                        return mob.Spell == null || !mob.Spell.IsCasting;
                    }
                ),
                new CastSpellActionNode(
                    tree,
                    (mob) => new CureSpell(mob)
                ),
                new AlwaysSucceedNode(
                    tree,
                    new TapNode(
                        tree,
                        (mob) =>
                        {
                            Tree.Blackboard.Remove("nextHealTime");
                            Tree.Blackboard.Add("nextHealTime", DateTime.Now + TimeSpan.FromSeconds(10.0));
                        }
                    )
                ),
                new TargetActionNode(tree),
                
            });
        }
    }
    /*
    public class CureSelfActionNode : ActionNode
    {
        public TimeSpan Cooldown { get; private set; }
        public CureSelfActionNode(BehaviorTree tree, TimeSpan cooldown) : base(tree)
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
            if (Tree.Mobile.Poison != null && Tree.Blackboard.TryGetValue("nextHealTime", out nextHealTime))
            {
                if (DateTime.Now >= (DateTime)nextHealTime)
                {
                    new CureSpell(Tree.Mobile).Cast();
                    Tree.Mobile.DebugSay("Casting cure spell for myself");
                    Tree.Blackboard.Remove("nextHealTime");
                    Tree.Blackboard.Add("nextHealTime", DateTime.Now + Cooldown);
                    return Result.Success;
                }

                Tree.Mobile.DebugSay("I'm poisoned, but my healing is on cooldown");

                return Result.Failure;
            }

            Tree.Mobile.DebugSay("I'm not poisoned");

            return Result.Failure;
        }
    }
    */
}
