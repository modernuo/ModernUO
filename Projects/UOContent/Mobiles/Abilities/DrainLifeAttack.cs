using System;

namespace Server.Mobiles;

public class DrainLifeAttack : MonsterAbilitySingleTargetDoT
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.DrainLife;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.GiveMeleeDamage;

    public override TimeSpan MinDelay => TimeSpan.FromSeconds(1.0);
    public override TimeSpan MaxDelay => TimeSpan.FromSeconds(1.0);

    protected override int GetCount(BaseCreature source, Mobile defender) => 5;

    public override double ChanceToTrigger => 0.5;

    private void DrainLife(BaseCreature source, Mobile defender)
    {
        source.DoHarmful(defender);
        defender.Mana -= 15;

        if (defender.Alive)
        {
            var damageGiven = AOS.Damage(defender, source, 5, 0, 0, 0, 0, 100);
            source.Hits += damageGiven;
        }
        else
        {
            RemoveEffect(source, defender);
            defender.SendLocalizedMessage(1070849); // The drain on your life force is gone.
        }
    }

    protected override bool CanEffectTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender) =>
        base.CanEffectTarget(trigger, source, defender) && defender.Mana > 14;

    protected override void OnBeforeTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        if (RemoveEffect(source, defender))
        {
            defender.SendLocalizedMessage(1070847); // The creature continues to steal your life force!
        }
        else
        {
            defender.SendLocalizedMessage(1070848); // You feel your life force being stolen away.
        }
    }

    protected override void OnTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        base.OnTarget(trigger, source, defender);
        DrainLife(source, defender);
    }

    protected override void EffectTick(BaseCreature source, Mobile defender, ref TimeSpan nextDelay)
    {
        DrainLife(source, defender);
    }

    protected override void EndEffect(BaseCreature source, Mobile defender)
    {
    }

    protected override void OnEffectExpired(BaseCreature source, Mobile defender)
    {
        defender.SendLocalizedMessage(1070849); // The drain on your life force is gone.
    }
}
