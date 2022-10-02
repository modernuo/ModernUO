using Server.Items;

namespace Server.Mobiles;

public class ThrowHatchetAttack : ThrowWeaponMonsterAbility
{
    protected override void ThrowEffect(BaseCreature source, Mobile defender)
    {
        source.MovingEffect(defender, 0xF43, 10, 0, false, false);
    }

    protected override bool CanEffectTarget(BaseCreature source, Mobile defender) =>
        base.CanEffectTarget(source, defender) && defender.Weapon is BaseRanged;
}
