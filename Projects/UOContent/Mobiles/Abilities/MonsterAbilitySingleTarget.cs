namespace Server.Mobiles;

public abstract class MonsterAbilitySingleTarget : MonsterAbility
{
    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        if (CanEffectTarget(trigger, source, target))
        {
            OnBeforeTarget(trigger, source, target);
            OnTarget(trigger, source, target);
        }

        base.Trigger(trigger, source, target);
    }

    protected virtual void OnBeforeTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
    }

    protected abstract void OnTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender);

    protected virtual bool CanEffectTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender) =>
        defender.Alive && source.CanBeHarmful(defender);
}
