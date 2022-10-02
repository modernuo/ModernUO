namespace Server.Mobiles;

public abstract class CounterAttack : MonsterAbility
{
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.TakeDamage;
    public override double ChanceToTrigger => 0.4;

    public override void Trigger(BaseCreature source, Mobile target)
    {
        if (CanEffectTarget(source, target))
        {
            source.DoHarmful(target);
            OnAttack(source, target);
        }

        base.Trigger(source, target);
    }

    protected abstract void OnAttack(BaseCreature source, Mobile defender);

    protected virtual bool CanEffectTarget(BaseCreature source, Mobile defender) =>
        defender.Alive && source.CanBeHarmful(defender);
}
