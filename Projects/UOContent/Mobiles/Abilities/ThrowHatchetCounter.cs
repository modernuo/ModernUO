using Server.Items;

namespace Server.Mobiles;

public class ThrowHatchetCounter : MonsterAbilityAttack
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.ThrowWeapon;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.TakeDamage;
    public override double ChanceToTrigger => 0.4;

    protected override void OnAttack(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        source.MovingEffect(defender, 0xF43, 10, 0, false, false);
        AOS.Damage(defender, source, 50, 100, 0, 0, 0, 0, 0);
    }

    protected override bool CanEffectTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender) =>
        base.CanEffectTarget(trigger, source, defender) && defender.Weapon is BaseRanged;
}
