using System;

namespace Server.Mobiles;

public class DeathExplosion : AreaEffectMonsterAbility
{
    private static int _chainDepth;

    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.Death;
    public override MonsterAbilityType AbilityType => MonsterAbilityType.DeathExplosion;

    public override double ChanceToTrigger => 1.0;
    public override TimeSpan MinTriggerCooldown => TimeSpan.Zero;
    public override TimeSpan MaxTriggerCooldown => TimeSpan.Zero;

    public override bool CanTrigger(BaseCreature source, MonsterAbilityTrigger trigger) => true;

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        if (trigger != MonsterAbilityTrigger.Death || _chainDepth > 0)
        {
            return;
        }

        source.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
        source.PlaySound(0x307);

        _chainDepth++;
        try
        {
            base.Trigger(trigger, source, target);
        }
        finally
        {
            _chainDepth--;
        }
    }

    protected override void DoEffectTarget(BaseCreature source, Mobile defender)
    {
        source.DoHarmful(defender);
        // TODO: Is the damage type correct?
        AOS.Damage(defender, source, defender.HitsMax / 5, true, 50, 50, 0, 0, 0);
    }
}
