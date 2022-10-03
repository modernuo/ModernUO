using Server.Collections;

namespace Server.Mobiles;

public abstract class AreaEffectMonsterAbility : MonsterAbility
{
    public virtual int AreaRange => 3;

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        var eable = source.GetMobilesInRange(AreaRange);
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

        base.Trigger(trigger, source, target);
    }

    protected virtual bool CanEffectTarget(BaseCreature source, Mobile defender) =>
        source != defender && defender.Alive && source.CanBeHarmful(defender)
            && (defender.Player || defender is BaseCreature bc && (bc.Team == source.Team || bc.Controlled || bc.Summoned))
            && (!Core.AOS || source.InLOS(defender));

    protected abstract void DoEffectTarget(BaseCreature source, Mobile defender);
}
