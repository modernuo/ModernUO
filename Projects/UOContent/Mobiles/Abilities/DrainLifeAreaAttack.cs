namespace Server.Mobiles;

public class DrainLifeAreaAttack : AreaEffectMonsterAbility
{
    public override MonsterAbilityTrigger AbilityTrigger =>
        MonsterAbilityTrigger.GiveMeleeDamage | MonsterAbilityTrigger.TakeMeleeDamage;

    public override MonsterAbilityType AbilityType => MonsterAbilityType.MassDrainLife;

    public override double ChanceToTrigger => 0.1;
    public override int AreaRange => 2;

    public virtual int MinDamage => 10;
    public virtual int MaxDamage => 40;

    protected virtual void DrainLife(BaseCreature source, Mobile defender)
    {
        var toDrain = Utility.RandomMinMax(MinDamage, MaxDamage);

        source.Hits += toDrain;
        source.DoHarmful(defender);
        defender.Damage(toDrain, source);
    }

    protected override void DoEffectTarget(BaseCreature source, Mobile defender)
    {
        defender.FixedParticles(0x374A, 10, 15, 5013, 0x496, 0, EffectLayer.Waist);
        defender.PlaySound(0x231);

        defender.SendMessage("You feel the life drain out of you!");
        DrainLife(source, defender);
    }
}
