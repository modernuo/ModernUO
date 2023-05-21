namespace Server.Mobiles;

public class PoisonGasCounter : MonsterAbilitySingleTarget
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.Poison;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.TakeDamage;

    public override double ChanceToTrigger => 0.05;
    public virtual int AttackRange => 1;

    protected override bool CanEffectTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender) =>
        source.InRange(defender, AttackRange) && defender is not BaseCreature { BardProvoked: true };

    protected override void OnTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        source.Animate(10, 4, 1, true, false, 0);
        source.DoHarmful(defender);
        AOS.Damage(defender, source, 50, 100, 0, 0, 0, 0, 0);

        defender.FixedParticles(0x36BD, 1, 10, 0x1F78, 0xA6, 0, (EffectLayer)255);
        defender.ApplyPoison(source, Poison.Deadly);
    }
}
