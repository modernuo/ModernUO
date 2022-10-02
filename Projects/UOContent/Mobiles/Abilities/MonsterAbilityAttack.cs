namespace Server.Mobiles;

public abstract class MonsterAbilityAttack : MonsterAbility
{
    public override void Trigger(BaseCreature source, Mobile target)
    {
        if (CanEffectTarget(source, target))
        {
            OnAttack(source, target);
        }

        base.Trigger(source, target);
    }

    protected virtual void OnAttack(BaseCreature source, Mobile defender)
    {
        source.DoHarmful(defender);
    }

    protected virtual bool CanEffectTarget(BaseCreature source, Mobile defender) =>
        defender.Alive && source.CanBeHarmful(defender);
}
