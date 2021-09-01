using System;
using System.Collections.Generic;

namespace Server.Mobiles.BehaviorTreeAI
{
    class SelfHealerNode : SelectorNode
    {
        public SelfHealerNode(BehaviorTree tree) : base(tree)
        {
            Children = new List<BehaviorTreeNode>(new BehaviorTreeNode[1]
            {
                new SequenceNode(
                    tree,
                    new BehaviorTreeNode[2]
                    {
                        new ConditionNode(
                            tree,
                            mob => true // Utility.RandomDouble() < 0.5
                        ),
                        new CureSelfNode(tree),
                    }
                )
                /*
                new SequenceNode(
                    tree,
                    new BehaviorTreeNode[2]
                    {
                        new ConditionNode(
                            tree,
                            mob => mob.Hits < mob.HitsMax - 20
                        ),
                        new SequenceNode(
                            tree,
                            new BehaviorTreeNode[3]
                            {
                                new ConditionNode(
                                    tree,
                                    mob => true // Utility.RandomDouble() < 0.2
                                ),
                                new HealSelfActionNode(tree, TimeSpan.FromSeconds(10.0)),
                                new TargetActionNode(tree)
                            }
                        )
                    }
                )
                */
            });
        }
    }
}
