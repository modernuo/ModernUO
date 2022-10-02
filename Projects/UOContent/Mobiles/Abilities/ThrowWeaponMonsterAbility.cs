namespace Server.Mobiles;

public abstract class ThrowWeaponMonsterAbility : MonsterAbility
{
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.TakeDamage;
    public override double ChanceToTrigger => 0.4;

    public virtual int MinDamage => 50;
    public virtual int MaxDamage => 50;

    // Damage Types
    public virtual int ChaosDamage => 0;
    public virtual int PhysicalDamage => 100;
    public virtual int FireDamage => 0;
    public virtual int ColdDamage => 0;
    public virtual int PoisonDamage => 0;
    public virtual int EnergyDamage => 0;

    public override void Trigger(BaseCreature source, Mobile target)
    {
        if (CanEffectTarget(source, target))
        {
            ThrowEffect(source, target);
            source.DoHarmful(target);
            AOS.Damage(
                target,
                source,
                Utility.RandomMinMax(MinDamage, MaxDamage),
                PhysicalDamage,
                FireDamage,
                ColdDamage,
                PoisonDamage,
                EnergyDamage,
                ChaosDamage
            );
        }

        base.Trigger(source, target);
    }

    protected abstract void ThrowEffect(BaseCreature source, Mobile defender);

    protected virtual bool CanEffectTarget(BaseCreature source, Mobile defender) =>
        defender.Alive && source.CanBeHarmful(defender);
}
