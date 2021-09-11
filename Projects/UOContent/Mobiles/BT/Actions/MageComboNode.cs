using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Spells.Sixth;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Fourth;
using Server.Spells.Fifth;
using Server.Spells;
using Server.Spells.Third;

namespace Server.Mobiles.BT
{
    public class MageComboNode : OwnerConditionNode
    {
        public MageComboNode(BehaviorTree tree)
            : base(tree, canActivate)
        {
            AddChild(
                new SequenceNode(tree)
                    .AddChild(new ForceSuccessNode(tree, new RandomExecutionNode(tree, 0.5, new PoisonEnemyNode(tree, TimeSpan.FromSeconds(6.0)))))
                    .AddChild(new ConditionNode(tree, shouldContinueCombo))
                    .AddChild(
                        new ForceSuccessNode(tree)
                            .AddChild(
                                new RandomExecutionNode(tree, 0.5)
                                    .AddChild(new SequenceNode(tree)
                                        .AddChild(new CastSpellActionNode(tree, getInterruptSpell))
                                        .AddChild(new TargetActionNode(tree))
                                    )
                            )
                    )
                    .AddChild(new ForceSuccessNode(tree, new CureSelfNode(tree, TimeSpan.FromSeconds(5.0))))
                    .AddChild(new ConditionNode(tree, shouldContinueCombo))
                    .AddChild(
                        // new ForceSuccessNode(tree)
                            // .AddChild(
                                new SequenceNode(tree)
                                    .AddChild(new CastSpellActionNode(tree, getExplosionSpell))
                                    .AddChild(new TargetActionNode(tree))
                                    .AddChild(new ConditionNode(tree, shouldContinueCombo))
                                    .AddChild(new CastSpellActionNode(tree, getEnergyBoltSpell))
                                    .AddChild(new TargetActionNode(tree))
                            // )
                    )
            );
        }
        private static bool canActivate(BaseCreature mob, Blackboard board)
        {
            return mob.Combatant != null && !mob.Combatant.Deleted && mob.Mana > 70;
        }
        private static bool shouldAttemptInterrupt(BaseCreature mob, Blackboard board)
        {
            if (mob.Combatant == null || mob.Combatant.Deleted)
            {
                return false;
            }

            if (mob.Combatant.Spell != null && mob.Combatant.Spell.IsCasting)
            {
                switch(mob.Combatant.Spell)
                {
                    case GreaterHealSpell:
                    case ExplosionSpell:
                    case EnergyBoltSpell:
                    case CureSpell:
                        return true;
                    default:
                        return false;
                }
            }

            if (mob.Combatant.Meditating)
                return true;

            return false;
        }
        private static bool shouldContinueCombo(BaseCreature mob, Blackboard board)
        {
            if (mob.Combatant == null || mob.Combatant.Deleted)
                return false;

            if (mob.Combatant.HitsMax - mob.Combatant.Hits >= 70)
            {
                return true;
            }

            if (mob.HitsMax - mob.Hits <= 50)
            {
                return true;
            }

            if (mob.Mana > 10)
            {
                return true;
            }

            if (mob.Combatant.Spell != null && mob.Combatant.Poison != null)
            {
                return true;
            }

            return false;
        }
        private static Spell getLightningSpell(BaseCreature mob)
        {
            return new LightningSpell(mob);
        }

        private static Spell getExplosionSpell(BaseCreature mob)
        {
            return new ExplosionSpell(mob);
        }

        private static Spell getEnergyBoltSpell(BaseCreature mob)
        {
            return new EnergyBoltSpell(mob);
        }

        private static Spell getInterruptSpell(BaseCreature mob)
        {
            var chance = Utility.RandomDouble();
            if (chance < 0.1)
            {
                new FireballSpell(mob);
            }
            else if (chance < 0.3)
            {
                return new LightningSpell(mob);
            }
            if (chance < 0.5)
            {
                return new MagicArrowSpell(mob);
            }
            return new HarmSpell(mob);
        }
    }
}
