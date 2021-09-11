using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Spells.Second;

namespace Server.Mobiles.BT
{
    public class CureSelfNode : OwnerConditionNode
    {
        public CureSelfNode(BehaviorTree tree, TimeSpan duration) : base(tree, (mob, board) => mob.Poison != null)
        {
            AddChild(
                new CooldownNode(tree, duration)
                    .AddChild(
                        new SequenceNode(tree)
                            .AddChild(new CastSpellActionNode(tree, (mob) => new CureSpell(mob)))
                            .AddChild(new TargetActionNode(tree))
                    )
            );
        }
    }
}
