using Server.Spells;
using Server.Spells.First;
using Server.Spells.Second;
using Server.Spells.Third;
using Server.Spells.Sixth;

namespace Server.Mobiles.BehaviorAI
{
    public class MageCombat : OwnerCondition
    {
        public MageCombat(BehaviorTree tree) : base(tree, canActivate)
        {
            AddChild(
                new Sequence(tree)
                    .AddChild(new Condition(tree, (context) => context.Mobile.Mana >= 65))
                    .AddChild(new CastSpell(tree, (context) => new ExplosionSpell(context.Mobile)))
                    .AddChild(new WaitForTarget(tree))
                    .AddChild(new AutoTarget(tree))
                    .AddChild(
                        new ForceSuccess(tree)
                            .AddChild(
                                new Selector(tree)
                                    .AddChild(new OwnerCondition(tree, (context) => Utility.RandomBool(), new CastSpell(tree, (context) => new PoisonSpell(context.Mobile))))
                                    .AddChild(new OwnerCondition(tree, (context) => Utility.RandomBool(), new CastSpell(tree, (context) => new FireballSpell(context.Mobile))))
                                    .AddChild(new CastSpell(tree, (context) => new EnergyBoltSpell(context.Mobile)))
                            )
                    )
                    .AddChild(new WaitForTarget(tree))
                    .AddChild(new AutoTarget(tree))
            );
        }
        public static bool canActivate(BehaviorTreeContext context)
        {
            return context.Mobile.Combatant != null;
        }
    }
}
