using System;
using System.Collections.Generic;

namespace Server.Mobiles;

/// <summary>
/// Abstract class used to build singletons for managing a specific monster ability.
/// </summary>
public abstract partial class MonsterAbility
{
    private Dictionary<BaseCreature, long> _nextTriggerTicks;

    public virtual MonsterAbilityType AbilityType => MonsterAbilityType.Generic;

    public virtual MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.None;

    public virtual double ChanceToTrigger => 1.0;
    public virtual TimeSpan MinTriggerCooldown => TimeSpan.Zero;
    public virtual TimeSpan MaxTriggerCooldown => TimeSpan.Zero;

    public bool WillTrigger(MonsterAbilityTrigger trigger) => (AbilityTrigger & trigger) != 0;

    /// <summary>
    /// Returns true if ability is not on cooldown, and the change to trigger succeeds.
    /// </summary>
    /// <returns>Boolean indicating the ability can trigger.</returns>
    public virtual bool CanTrigger(BaseCreature source, MonsterAbilityTrigger trigger)
    {
        if (_nextTriggerTicks?.TryGetValue(source, out var nextTrigger) == true && Core.TickCount < nextTrigger)
        {
            return false;
        }

        var c = ChanceToTrigger;

        if (c >= 1)
        {
            return true;
        }

        if (c <= 0)
        {
            return false;
        }

        var rnd = Utility.RandomDouble();

        return c > rnd;
    }

    /// <summary>
    /// Triggers the monster's ability. Override this and call `base.Trigger(source);` to make sure
    /// the cooldown is tracked.
    /// </summary>
    public virtual void Trigger(MonsterAbilityTrigger trigger, BaseCreature source, Mobile target)
    {
        if (MinTriggerCooldown <= TimeSpan.Zero && MaxTriggerCooldown <= TimeSpan.Zero)
        {
            return;
        }

        var randomAmount = MinTriggerCooldown == MaxTriggerCooldown
            ? MinTriggerCooldown
            : Utility.RandomMinMax(MinTriggerCooldown, MaxTriggerCooldown);

        var nextTrigger = Core.TickCount + (long)randomAmount.TotalMilliseconds;

        _nextTriggerTicks ??= new Dictionary<BaseCreature, long>();
        _nextTriggerTicks[source] = nextTrigger;
    }

    public virtual void AlterMeleeDamageFrom(BaseCreature source, Mobile target, ref int damage)
    {
    }

    public virtual void AlterMeleeDamageTo(BaseCreature source, Mobile target, ref int damage)
    {
    }

    public virtual void AlterSpellDamageScalarFrom(BaseCreature source, Mobile target, ref double scalar)
    {
    }

    public virtual void AlterSpellDamageScalarTo(BaseCreature source, Mobile target, ref double scalar)
    {
    }

    public virtual void AlterSpellDamageFrom(BaseCreature source, Mobile target, ref int damage)
    {
    }

    public virtual void AlterSpellDamageTo(BaseCreature source, Mobile target, ref int damage)
    {
    }

    public virtual void Move(BaseCreature creature, Direction d)
    {
    }
}
