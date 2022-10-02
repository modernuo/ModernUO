using Server.Items;

namespace Server.Mobiles;

public class ThrowHatchetCounter : CounterAttack
{
    protected override void OnAttack(BaseCreature source, Mobile defender)
    {
        source.MovingEffect(defender, 0xF43, 10, 0, false, false);
        AOS.Damage(defender, source, 50, 100, 0, 0, 0, 0, 0);
    }

    protected override bool CanEffectTarget(BaseCreature source, Mobile defender) =>
        base.CanEffectTarget(source, defender) && defender.Weapon is BaseRanged;
}
