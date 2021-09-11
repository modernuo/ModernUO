using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Spells.First;

namespace Server.Mobiles.BT
{
    public class MageDebuffNode : SelectorNode
    {
        public MageDebuffNode(BehaviorTree tree) : base(tree)
        {
            AddChild(
                new OwnerConditionNode(tree, (mob, board) => mob.Combatant != null && mob.Combatant.Str == mob.Combatant.RawStr)
                    .AddChild(
                        new SequenceNode(tree)
                            .AddChild(new CastSpellActionNode(tree, (mob) => new WeakenSpell(mob)))
                            .AddChild(new TargetActionNode(tree))
                    )
            );
            AddChild(
                new OwnerConditionNode(tree, (mob, board) => mob.Combatant != null && mob.Combatant.Dex == mob.Combatant.RawDex)
                    .AddChild(
                        new SequenceNode(tree)
                            .AddChild(new CastSpellActionNode(tree, (mob) => new ClumsySpell(mob)))
                            .AddChild(new TargetActionNode(tree))
                    )
            );
            AddChild(
                new OwnerConditionNode(tree, (mob, board) => mob.Combatant != null && mob.Combatant.Int == mob.Combatant.RawInt)
                    .AddChild(
                        new SequenceNode(tree)
                            .AddChild(new CastSpellActionNode(tree, (mob) => new FeeblemindSpell(mob)))
                            .AddChild(new TargetActionNode(tree))
                    )
            );
        }
    }
}
