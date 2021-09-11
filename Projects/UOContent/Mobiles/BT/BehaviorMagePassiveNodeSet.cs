using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Mobiles.BT
{
    public class BehaviorMagePassiveNodeSet : OwnerConditionNode
    {
        public BehaviorMagePassiveNodeSet(BehaviorTree tree)
            : base(tree, canActivate)
        {
            AddChild(
                new SelectorNode(tree)
                    .AddChild(new TapNode(tree, (mob, board) => { mob.CurrentSpeed = mob.PassiveSpeed; }))
                    .AddChild(new CooldownNode(tree, TimeSpan.FromSeconds(2.0), new WanderNode(tree, 3)))
            );
        }
        private static bool canActivate(BaseCreature mob, Blackboard board)
        {
            return mob.Combatant == null;
        }
    }
}
