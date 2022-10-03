using System;

namespace Server.Mobiles;

public class DeathExplosion : AreaEffectMonsterAbility
{
    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.Death;
    public override MonsterAbilityType AbilityType => MonsterAbilityType.DeathExplosion;

    public override double ChanceToTrigger => 1.0;
    public override TimeSpan MinTriggerCooldown => TimeSpan.Zero;
    public override TimeSpan MaxTriggerCooldown => TimeSpan.Zero;

    public override bool CanTrigger(BaseCreature source, MonsterAbilityTrigger trigger) => true;

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        source.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
        source. PlaySound(0x307);

        base.Trigger(trigger, source, target);
    }

    protected override void DoEffectTarget(BaseCreature source, Mobile defender)
    {
        source.DoHarmful(defender);
        // TODO: Is the damage type correct?
        AOS.Damage(defender, source, defender.HitsMax / 5, true, 100, 0, 0, 0, 0);
    }
}
