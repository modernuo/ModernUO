using System;
using System.Collections.Generic;

namespace Server.Mobiles;

public abstract partial class MonsterAbility
{
    private Dictionary<Mobile, long> _nextTriggerTicks = new();

    public virtual MonsterAbilityType AbilityType => MonsterAbilityType.Generic;

    public virtual MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.None;
    public virtual double ChanceToTrigger => 1.0;
    public virtual TimeSpan MinTriggerCooldown => TimeSpan.FromSeconds(30);
    public virtual TimeSpan MaxTriggerCooldown => TimeSpan.FromSeconds(60);

    /// <summary>
    /// Returns true if ability is not on cooldown, and the change to trigger succeeds.
    /// </summary>
    /// <param name="source">The mobile this ability is attached to</param>
    /// <returns>Boolean indicating the ability can trigger.</returns>
    public virtual bool CanTrigger(Mobile source)
    {
        var c = ChanceToTrigger;
        return c >= 1 ||
               c > 0 && (!_nextTriggerTicks.TryGetValue(source, out var nextTicks) || Core.TickCount >= nextTicks) &&
               c > Utility.RandomDouble();
    }

    /// <summary>
    /// Triggers the monster's ability. Override this and call `base.Trigger(source);` to make sure
    /// the cooldown is tracked.
    /// </summary>
    /// <param name="source"></param>
    public virtual void Trigger(Mobile source, Mobile target)
    {
        var nextTicks = Core.TickCount
                        + Utility.RandomMinMax(MinTriggerCooldown.Ticks, MaxTriggerCooldown.Ticks)
                        / TimeSpan.TicksPerMillisecond;

        _nextTriggerTicks[source] = nextTicks;
    }
}
