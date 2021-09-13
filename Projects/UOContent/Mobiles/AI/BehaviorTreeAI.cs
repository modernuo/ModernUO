using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// using Server.Mobiles.BT;
using Server.Targeting;
using Server.Mobiles.BehaviorAI;
using Server.Spells.Third;

namespace Server.Mobiles.AI
{
    public class BehaviorTreeAI : BaseAI
    {
        private Blackboard blackboard;
        private BehaviorTree behaviorTree;
        private BehaviorTreeContext behaviorTreeContext;
        public BehaviorTreeAI(BaseCreature m) : base(m)
        {
            // blackboard = new Blackboard();
            blackboard = new Blackboard();
            behaviorTreeContext = new BehaviorTreeContext(m, blackboard);
            behaviorTree = new BehaviorTree();
            behaviorTree.TryAddRoot(
                new Selector(behaviorTree)
                    .AddChild(new MageCombat(behaviorTree))
                    .AddChild(new MagePassive(behaviorTree))
            /*
            new Sequence(behaviorTree)
                .AddChild(new Condition(behaviorTree, (context) => context.Mobile.RawStr - context.Mobile.Str == 0))
                .AddChild(new CastSpell(behaviorTree, (context) => new BlessSpell(context.Mobile)))
                .AddChild(new WaitForTarget(behaviorTree))
                .AddChild(new DynamicTarget(behaviorTree, (context) => context.Mobile))
            */
            );

            behaviorTree.Start(behaviorTreeContext);
        }

        public override bool Think()
        {
            if (m_Mobile.Deleted)
            {
                return false;
            }

            // MageBehaviorTree.Instance.RunBehavior(m_Mobile, blackboard);

            behaviorTree.Tick(behaviorTreeContext);

            return true;
        }

        public override bool DoActionWander()
        {
            return true;
        }
    }
}
