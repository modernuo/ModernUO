using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Spells.Third;

namespace Server.Mobiles.BT
{
    public class PoisonEnemyNode : OwnerConditionNode
    {
        public PoisonEnemyNode(BehaviorTree tree, TimeSpan duration) : base(tree, (mob, board) => mob.Combatant != null && !mob.Combatant.Deleted && mob.Combatant.Poison == null)
        {
            AddChild(
                new CooldownNode(tree, duration)
                    .AddChild(
                        new SequenceNode(tree)
                            .AddChild(new CastSpellActionNode(tree, (mob) => new PoisonSpell(mob)))
                            .AddChild(new TargetActionNode(tree))
                    )
            );
        }

        private bool canActivate(BaseCreature mob, Blackboard blackboard)
        {
            if (mob.Combatant == null || mob.Combatant.Deleted)
            {
                return false;
            }

            return mob.Combatant.Poison == null;
        }
    }
}
