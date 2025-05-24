using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;

namespace Server.Mobiles;

/// <summary>
/// Abstract class used to build singletons for managing a specific monster ability.
/// </summary>
public abstract class MonsterAbility
{
    private Dictionary<BaseCreature, long> _nextTriggerTicks;

    public virtual MonsterAbilityType AbilityType => MonsterAbilityType.Generic;

    public virtual MonsterAbilityTrigger AbilityTrigger => MonsterAbilityTrigger.None;

    public virtual double ChanceToTrigger => 1.0;
    public virtual TimeSpan MinTriggerCooldown => TimeSpan.Zero;
    public virtual TimeSpan MaxTriggerCooldown => TimeSpan.Zero;

    /// <summary>
    /// Returns true if ability is not on cooldown, and the chance to trigger succeeds.
    /// </summary>
    /// <returns>Boolean indicating the ability can trigger.</returns>
    public virtual bool CanTrigger(BaseCreature source, MonsterAbilityTrigger trigger)
    {
        if ((AbilityTrigger & trigger) == 0 || source is not { Alive: true, Deleted: false })
        {
            return false;
        }

        if (_nextTriggerTicks?.TryGetValue(source, out var nextTrigger) == true)
        {
            if (nextTrigger - Core.TickCount > 0)
            {
                return false;
            }

            _nextTriggerTicks.Remove(source);
            if (_nextTriggerTicks.Count == 0)
            {
                _nextTriggerTicks = null;
            }
        }

        var c = ChanceToTrigger;

        return c >= 1 || c > 0 && c > Utility.RandomDouble();
    }

    /// <summary>
    /// Triggers the monster's ability. Override this and call `base.Trigger(source);` to make sure
    /// the cooldown is tracked.
    /// Note: This can fire after a monster is killed/deleted (map is null)
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

    public virtual void Move(BaseCreature source, Direction d)
    {
    }

    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
    public static void InvalidateNextAbilityTriggers(BaseCreature source)
    {
        var abilities = source.GetMonsterAbilities();
        if (abilities == null || abilities.Length == 0)
        {
            return;
        }

        for (var i = 0; i < abilities.Length; i++)
        {
            abilities[i]._nextTriggerTicks?.Remove(source);
        }
    }
}
