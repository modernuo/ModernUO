namespace Server.Mobiles;

public abstract class MonsterAbilityAttack : MonsterAbility
{
    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        if (CanEffectTarget(trigger, source, target))
        {
            OnAttack(trigger, source, target);
        }

        base.Trigger(trigger, source, target);
    }

    protected virtual void OnAttack(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        source.DoHarmful(defender);
    }

    protected virtual bool CanEffectTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender) =>
        defender.Alive && source.CanBeHarmful(defender);
}
