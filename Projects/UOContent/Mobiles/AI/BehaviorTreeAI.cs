using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Mobiles.BehaviorTreeAI;
using Server.Targeting;

namespace Server.Mobiles.AI
{
    public class BehaviorTreeAI : BaseAI
    {
        private BehaviorTree behaviorTree;
        public BehaviorTreeAI(BaseCreature m) : base(m)
        {
            behaviorTree = new BehaviorTree(m);

            behaviorTree.AddRoot(
                    new SelectorNode(behaviorTree, new BehaviorTreeNode[2]
                    {
                        new SelfHealerNode(behaviorTree),
                        // new MageComboNode(behaviorTree),
                        new TapNode(
                            behaviorTree,
                            (mob) => mob.DebugSay("I am taking no action")
                        )
                    })
            );

            behaviorTree.Start();
        }

        public override bool Think()
        {
            if (m_Mobile.Deleted)
            {
                return false;
            }

            // behaviorTree.RunBehavior();

            return base.Think();
        }
    }
}
