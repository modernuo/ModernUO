using System;

namespace Server.Mobiles;

public class PoisonGasAreaAttack : AreaEffectMonsterAbility
{
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.CombatAction;
    public override MonsterAbilityType AbilityType => MonsterAbilityType.MassPoison;

    public override double ChanceToTrigger => 0.5;
    public override TimeSpan MinTriggerCooldown => TimeSpan.FromSeconds(5.0);
    public override TimeSpan MaxTriggerCooldown => TimeSpan.FromSeconds(30.0);

    public virtual int CombatantRange => 3;

    public override bool CanTrigger(BaseCreature source, MonsterAbilityTrigger trigger)
    {
        if (!base.CanTrigger(source, trigger))
        {
            return false;
        }

        var combatant = source.Combatant;

        // Only do the mass ability as a side-effect of combating a specific target
        return combatant?.Deleted == false && combatant.Map == source.Map && source.InRange(combatant.Location, CombatantRange) &&
               source.CanBeHarmful(combatant) && source.InLOS(combatant);
    }

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        source.FixedParticles(0x376A, 9, 32, 0x2539, EffectLayer.LeftHand);
        source.PlaySound(0x1DE);

        base.Trigger(trigger, source, target);
    }

    protected override void DoEffectTarget(BaseCreature source, Mobile defender)
    {
        defender.ApplyPoison(source, Poison.Deadly);
    }
}
