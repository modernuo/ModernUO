using System;
using Server.Collections;

namespace Server.Mobiles;

public abstract class AreaEffectMonsterAbility : MonsterAbility
{
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.OnCombatAction;
    public override MonsterAbilityType AbilityType => MonsterAbilityType.MassPoison;

    public override double ChanceToTrigger => 0.5;
    public override TimeSpan MinTriggerCooldown => TimeSpan.FromSeconds(5.0);
    public override TimeSpan MaxTriggerCooldown => TimeSpan.FromSeconds(30.0);

    public virtual int CombatantRange => 3;

    public virtual int AreaRange => 3;

    public override bool CanTrigger(BaseCreature source)
    {
        if (!base.CanTrigger(source))
        {
            return false;
        }

        var combatant = source.Combatant;

        // Only do the mass ability as a side-effect of combating a specific target
        return combatant?.Deleted == false && combatant.Map == source.Map && source.InRange(combatant.Location, CombatantRange) &&
               source.CanBeHarmful(combatant) && source.InLOS(combatant);
    }

    public override void Trigger(BaseCreature source, Mobile target)
    {
        // target is null for OnCombatAction triggers

        var eable = source.GetMobilesInRange(2);
        using var queue = PooledRefQueue<Mobile>.Create();
        foreach (var m in eable)
        {
            if (CanEffectTarget(source, m))
            {
                queue.Enqueue(m);
            }
        }
        eable.Free();

        while (queue.Count > 0)
        {
            DoEffectTarget(source, queue.Dequeue());
        }

        base.Trigger(source, target);
    }

    protected virtual bool CanEffectTarget(BaseCreature source, Mobile defender) =>
        source != defender && defender.Alive && source.IsEnemy(defender) && source.CanBeHarmful(defender);

    protected abstract void DoEffectTarget(BaseCreature source, Mobile defender);
}
