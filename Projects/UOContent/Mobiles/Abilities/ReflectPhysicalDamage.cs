namespace Server.Mobiles;

public class ReflectPhysicalDamage : MonsterAbility
{
    // This is handled manually in AOS.Damage()
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.CombatAction;

    public override MonsterAbilityType AbilityType => MonsterAbilityType.ReflectPhysicalDamage;

    public virtual int PercentReflected => 10;
}
