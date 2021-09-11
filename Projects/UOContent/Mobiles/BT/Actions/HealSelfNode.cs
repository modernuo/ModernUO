using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Spells.Fourth;
using Server.Spells.First;
using Server.Spells;

namespace Server.Mobiles.BT
{
    public class HealSelfNode : OwnerConditionNode
    {
        public HealSelfNode(BehaviorTree tree, TimeSpan duration) : base(tree, (mob, board) => mob.HitsMax - mob.Hits > 0)
        {
            AddChild(
                new CooldownNode(tree, duration)
                    .AddChild(
                    new SequenceNode(tree)
                            .AddChild(new CastSpellActionNode(tree, GetHealSpell))
                            .AddChild(new TargetActionNode(tree))
                    )
            );
        }

        private Spell GetHealSpell(BaseCreature mob)
        {
            if (mob.Mana < 10 || mob.HitsMax - mob.Hits < 15 || Utility.RandomDouble() < 0.3)
            {
                return new HealSpell(mob);
            }

            return new GreaterHealSpell(mob);
        }
    }
}
