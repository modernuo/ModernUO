using System;

namespace Server.Mobiles;

public class BloodBathAttack : MonsterAbilitySingleTargetDoT
{
    public override MonsterAbilityType AbilityType => MonsterAbilityType.BloodBath;
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.GiveMeleeDamage;

    public override TimeSpan MinDelay => TimeSpan.FromSeconds(1.0);
    public override TimeSpan MaxDelay => TimeSpan.FromSeconds(1.0);

    protected override int GetCount(BaseCreature source, Mobile defender) => 5;

    protected override void EffectTick(BaseCreature source, Mobile defender, ref TimeSpan nextDelay)
    {
        if (defender.Alive)
        {
            defender.Damage(2, source);
        }
        else
        {
            RemoveEffect(source, defender);
        }
    }

    public override double ChanceToTrigger => 0.1;

    protected override void OnBeforeTarget(MonsterAbilityTrigger trigger, BaseCreature source, Mobile defender)
    {
        if (RemoveEffect(source, defender))
        {
            defender.SendLocalizedMessage(1070825); // The creature continues to rage!
        }
        else
        {
            defender.SendLocalizedMessage(1070826); // The creature goes into a rage, inflicting heavy damage!
        }
    }

    protected override void EndEffect(BaseCreature source, Mobile defender)
    {
    }

    protected override void OnEffectExpired(BaseCreature source, Mobile defender)
    {
        defender.SendLocalizedMessage(1070824); // The creature's rage subsides.
    }
}
