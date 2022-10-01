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
    public virtual TimeSpan MinTriggerCooldown => TimeSpan.FromSeconds(30.0);
    public virtual TimeSpan MaxTriggerCooldown => TimeSpan.FromSeconds(60.0);

    /// <summary>
    /// Returns true if ability is not on cooldown, and the change to trigger succeeds.
    /// </summary>
    /// <param name="source">The mobile this ability is attached to</param>
    /// <returns>Boolean indicating the ability can trigger.</returns>
    public virtual bool CanTrigger(BaseCreature source)
    {
        var c = ChanceToTrigger;

        if (c >= 1)
        {
            return true;
        }

        if (c <= 0)
        {
            return false;
        }

        if (_nextTriggerTicks?.TryGetValue(source, out var nextTrigger) != true)
        {
            return true;
        }

        return Core.TickCount >= nextTrigger && c > Utility.RandomDouble();
    }

    /// <summary>
    /// Triggers the monster's ability. Override this and call `base.Trigger(source);` to make sure
    /// the cooldown is tracked.
    /// </summary>
    /// <param name="source"></param>
    public virtual void Trigger(BaseCreature source, Mobile target)
    {
        var nextTrigger = Core.TickCount
                          + Utility.RandomMinMax(MinTriggerCooldown.Ticks, MaxTriggerCooldown.Ticks)
                          / TimeSpan.TicksPerMillisecond;

        _nextTriggerTicks ??= new Dictionary<BaseCreature, long>();
        _nextTriggerTicks[source] = nextTrigger;
    }
}
