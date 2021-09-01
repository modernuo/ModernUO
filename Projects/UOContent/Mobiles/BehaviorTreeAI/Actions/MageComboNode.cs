using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Server.Spells.Sixth;
using Server.Spells.Fifth;
using Server.Spells.First;

namespace Server.Mobiles.BehaviorTreeAI
{
    public class MageComboNode : SequenceNode
    {
        public MageComboNode(BehaviorTree tree) : base(tree)
        {
            /*
            Children = new List<BehaviorTreeNode>(new BehaviorTreeNode[1]
            {
                // if we have a combatant
                new ConditionNode(
                    tree,
                    mob => mob.Combatant != null,
                )
                new ConditionNode(
                    tree,
                    mob => mob.Combatant != null,
                    // do the following in sequence
                    new SelectorNode(
                        tree,
                        new BehaviorTreeNode[2]
                        {
                            // do the first appropriate debuff until all are done
                            new SelectorNode(
                                tree,
                                new BehaviorTreeNode[3]
                                {
                                    // weaken if necessary
                                    new ConditionNode(
                                        tree,
                                        mob => mob.Combatant.Str == mob.Combatant.RawStr,
                                        new SequenceNode(
                                            tree,
                                            new BehaviorTreeNode[2]
                                            {
                                                new CastSpellActionNode(
                                                    tree,
                                                    mob => new WeakenSpell(mob)
                                                ),
                                                new TargetActionNode(tree)
                                            }
                                        )
                                    ),
                                    // clumbsy if necessary
                                    new ConditionNode(
                                        tree,
                                        mob => mob.Combatant.Dex == mob.Combatant.RawDex,
                                        new SequenceNode(
                                            tree,
                                            new BehaviorTreeNode[2]
                                            {
                                                new CastSpellActionNode(
                                                    tree,
                                                    mob => new ClumsySpell(mob)
                                                ),
                                                new TargetActionNode(tree)
                                            }
                                        )
                                    ),
                                    // feeblemind if necessary
                                    new ConditionNode(
                                        tree,
                                        mob => mob.Combatant.Int == mob.Combatant.RawInt,
                                        new SequenceNode(
                                            tree,
                                            new BehaviorTreeNode[2]
                                            {
                                                new CastSpellActionNode(
                                                    tree,
                                                    mob => new FeeblemindSpell(mob)
                                                ),
                                                new TargetActionNode(tree)
                                            }
                                        )
                                    ),
                                }
                            ),
                            // do explosion ebolt combo
                            new SequenceNode(
                                tree,
                                new BehaviorTreeNode[2]
                                {
                                    // explosion
                                    new SequenceNode(
                                        tree,
                                        new BehaviorTreeNode[2]
                                        {
                                            new CastSpellActionNode(
                                                tree,
                                                mob => new ExplosionSpell(mob)
                                            ),
                                            new TargetActionNode(tree)
                                        }
                                    ),
                                    // ebolt
                                    new SequenceNode(
                                        tree,
                                        new BehaviorTreeNode[2]
                                        {
                                            new CastSpellActionNode(
                                                tree,
                                                mob => new EnergyBoltSpell(mob)
                                            ),
                                            new TargetActionNode(tree)
                                        }
                                    )
                                }
                            ),
                        }
                    )
                )
            });
            */

            /*
            Children = new List<BehaviorTreeNode>(new BehaviorTreeNode[1] {
                new ConditionNode(
                    tree,
                    mob => mob.Combatant != null,
                    new SequenceNode(
                        tree,
                        new BehaviorTreeNode[3] {
                            new AlwaysSucceedNode(
                                tree,
                                new ConditionNode(
                                    tree,
                                    mob => mob.Combatant != null && mob.Combatant.Str == mob.Combatant.RawStr,
                                    new SequenceNode(
                                        tree,
                                        new BehaviorTreeNode[2]
                                        {
                                            new CastSpellActionNode(
                                                tree,
                                                mob => new WeakenSpell(mob)
                                            ),
                                            new TargetActionNode(tree)
                                        }
                                    )
                                )
                            ),
                            new SequenceNode(tree, new BehaviorTreeNode[2]{
                                new CastSpellActionNode(
                                    tree,
                                    mob => new ExplosionSpell(mob)
                                ),
                                new TargetActionNode(tree)
                            }),
                            new SequenceNode(tree, new BehaviorTreeNode[2]{
                                new SelectorNode(
                                    tree,
                                    new BehaviorTreeNode[2]
                                    {
                                        new ConditionNode(
                                            tree,
                                            mob => Utility.RandomDouble() < 0.3,
                                            new CastSpellActionNode(
                                                tree,
                                                mob => new MindBlastSpell(mob)
                                            )
                                        ),
                                        new CastSpellActionNode(
                                            tree,
                                            mob => new EnergyBoltSpell(mob)
                                        )
                                    }
                                ),
                                new TargetActionNode(tree)
                            }),
                        }
                    )
                )
            });
            */
        }
    }
}
