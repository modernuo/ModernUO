using Server.Items;

namespace Server.Mobiles;

public class EnergyBoltCounter : MonsterAbilitySingleTarget
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.EnergyBoltCounter;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.TakeDamage; // Ranged attackers
    public override double ChanceToTrigger => 0.4;

    protected override void OnTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        source.MovingParticles(defender, 0x379F, 7, 0, false, true, 0xBE3, 0xFCB, 0x211);
        defender.PlaySound(0x229);

        source.DoHarmful(defender);
        AOS.Damage(defender, source, 50, 0, 0, 0, 0, 100, 0);
    }

    protected override bool CanEffectTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender) =>
        base.CanEffectTarget(trigger, source, defender)
        && (trigger == MonsterAbilityTrigger.TakeSpellDamage || defender.Weapon is BaseRanged);
}
