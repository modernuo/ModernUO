using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Mobiles;

public abstract class StunAttack : MonsterAbility
{
    // Prevents infinite loop
    private HashSet<BaseCreature> _stunning;

    public override MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.GiveMeleeDamage;
    public override MonsterAbilityType AbilityType => MonsterAbilityType.Stun;
    public override double ChanceToTrigger => 0.3;

    public virtual TimeSpan StunDuration => TimeSpan.FromSeconds(5.0);

    public override bool CanTrigger(BaseCreature source, MonsterAbilityTrigger trigger) => !_stunning.Contains(source) && base.CanTrigger(source, trigger);

    public override void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        if (source.Weapon is BaseWeapon weapon)
        {
            _stunning ??= new HashSet<BaseCreature>();
            _stunning.Add(source);
            weapon.OnHit(source, target);
            _stunning.Remove(source);
        }

        if (target.Alive)
        {
            target.Frozen = true;
            Timer.DelayCall(StunDuration, Recover, target);
        }

        base.Trigger(trigger, source, target);
    }

    protected virtual void Recover(Mobile defender)
    {
        defender.Frozen = false;
        defender.Combatant = null;
    }
}
