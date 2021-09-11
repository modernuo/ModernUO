using System;
using Server.Items;

namespace Server.Mobiles.BT
{
    public class BehaviorMageCombatNodeSet : OwnerConditionNode
    {
        public BehaviorMageCombatNodeSet(BehaviorTree tree)
            : base(tree, canActivate)
        {
            AddChild(
                new SelectorNode(tree)
                    .AddChild(new TapNode(tree, (mob, board) => { mob.CurrentSpeed = mob.ActiveSpeed; }))
                    .AddChild(new CooldownNode(tree, TimeSpan.FromSeconds(0.5), new InverterNode(tree, new KeepRangeNode(tree, 1, 2))))
                    .AddChild(new MageDebuffNode(tree))
                    .AddChild(new MageComboNode(tree))
                    .AddChild(new OwnerConditionNode(tree, (mob, board) => mob.Poison != null, new CureSelfNode(tree, TimeSpan.FromSeconds(6.0))))
                    .AddChild(new OwnerConditionNode(tree, (mob, board) => mob.HitsMax - mob.Hits >= 40, new HealSelfNode(tree, TimeSpan.FromSeconds(6.0))))
                    .AddChild(new OwnerConditionNode(tree, shouldMeditate, new MeditateNode(tree, 75)))
            );
        }

        private static bool canActivate(BaseCreature mob, Blackboard board)
        {
            return mob.Combatant != null && !mob.Combatant.Deleted;
        }
        private static bool shouldMeditate(BaseCreature mob, Blackboard board)
        {
            return mob.Mana <= 30 || (mob.Meditating && mob.Mana < 75);
        }
    }
}
