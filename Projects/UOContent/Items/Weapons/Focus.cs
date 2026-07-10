using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Collections;
using Server.Mobiles;

namespace Server.Items;

public static class FocusContext
{
    // UO.com defines the -50% starting point and +20% cap but not the intermediate table.
    // A deterministic 10-point step preserves those endpoints without copying ServUO's older sequence.
    internal const int InitialDamageModifier = -50;
    internal const int MaximumDamageModifier = 20;
    internal const int DamageModifierStep = 10;

    private static readonly Dictionary<BaseWeapon, Context> _contexts = [];
    private static readonly Dictionary<Mobile, HashSet<BaseWeapon>> _attackerIndex = [];
    private static readonly Dictionary<Mobile, HashSet<BaseWeapon>> _targetIndex = [];
    private static bool _configured;

    static FocusContext() => Configure();

    public static void Configure()
    {
        if (_configured)
        {
            return;
        }

        _configured = true;
        EventSink.Logout += Clear;
    }

    internal static int ActiveContextCount => _contexts.Count;

    internal static int GetCurrentModifierForTests(BaseWeapon weapon) =>
        weapon != null && _contexts.TryGetValue(weapon, out var context) ? context.DamageModifier : 0;

    internal static int GetDamageBonus(BaseWeapon weapon, Mobile attacker, Mobile defender)
    {
        if (!CanUse(weapon, attacker, defender))
        {
            Clear(weapon);
            return 0;
        }

        if (!_contexts.TryGetValue(weapon, out var context) || context.Attacker != attacker)
        {
            Clear(weapon);
            context = new Context(attacker, defender);
            _contexts[weapon] = context;
            AddToIndex(_attackerIndex, attacker, weapon);
            AddToIndex(_targetIndex, defender, weapon);
        }
        else if (context.Target != defender)
        {
            SetTarget(weapon, context, defender);
            context.DamageModifier = InitialDamageModifier;
        }

        return context.DamageModifier;
    }

    internal static void OnSuccessfulHit(BaseWeapon weapon, Mobile attacker, Mobile defender)
    {
        if (!CanUse(weapon, attacker, defender))
        {
            Clear(weapon);
            return;
        }

        if (!_contexts.TryGetValue(weapon, out var context) || context.Attacker != attacker ||
            context.Target != defender)
        {
            return;
        }

        context.DamageModifier = Math.Min(
            MaximumDamageModifier,
            context.DamageModifier + DamageModifierStep
        );
    }

    internal static void OnMiss(BaseWeapon weapon, Mobile attacker, Mobile defender)
    {
        if (!CanUse(weapon, attacker, defender))
        {
            Clear(weapon);
            return;
        }

        if (_contexts.TryGetValue(weapon, out var context) && context.Attacker == attacker &&
            context.Target != defender)
        {
            SetTarget(weapon, context, defender);
            context.DamageModifier = InitialDamageModifier;
        }
    }

    internal static void Clear(BaseWeapon weapon)
    {
        if (weapon == null || !_contexts.Remove(weapon, out var context))
        {
            return;
        }

        RemoveFromIndex(_attackerIndex, context.Attacker, weapon);
        RemoveFromIndex(_targetIndex, context.Target, weapon);
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
    public static void Clear(Mobile mobile)
    {
        if (mobile == null)
        {
            return;
        }

        using var weapons = PooledRefList<BaseWeapon>.Create();

        if (_attackerIndex.TryGetValue(mobile, out var attackers))
        {
            foreach (var weapon in attackers)
            {
                weapons.Add(weapon);
            }
        }

        if (_targetIndex.TryGetValue(mobile, out var targets))
        {
            foreach (var weapon in targets)
            {
                weapons.Add(weapon);
            }
        }

        for (var i = 0; i < weapons.Count; i++)
        {
            Clear(weapons[i]);
        }
    }

    internal static void ClearAll()
    {
        using var weapons = PooledRefList<BaseWeapon>.Create();

        foreach (var weapon in _contexts.Keys)
        {
            weapons.Add(weapon);
        }

        for (var i = 0; i < weapons.Count; i++)
        {
            Clear(weapons[i]);
        }
    }

    private static bool CanUse(BaseWeapon weapon, Mobile attacker, Mobile defender) =>
        Core.HS &&
        weapon?.Deleted == false &&
        weapon.ExtendedWeaponAttributes.Focus != 0 &&
        attacker?.Deleted == false &&
        attacker.Alive &&
        attacker.Map != null &&
        attacker.Map != Map.Internal &&
        attacker.Weapon == weapon &&
        weapon.Parent == attacker &&
        defender?.Deleted == false &&
        defender.Alive &&
        defender.Map != null &&
        defender.Map != Map.Internal;

    private static void SetTarget(BaseWeapon weapon, Context context, Mobile target)
    {
        RemoveFromIndex(_targetIndex, context.Target, weapon);
        context.Target = target;
        AddToIndex(_targetIndex, target, weapon);
    }

    private static void AddToIndex(
        Dictionary<Mobile, HashSet<BaseWeapon>> index,
        Mobile mobile,
        BaseWeapon weapon
    )
    {
        if (!index.TryGetValue(mobile, out var weapons))
        {
            weapons = [];
            index[mobile] = weapons;
        }

        weapons.Add(weapon);
    }

    private static void RemoveFromIndex(
        Dictionary<Mobile, HashSet<BaseWeapon>> index,
        Mobile mobile,
        BaseWeapon weapon
    )
    {
        if (mobile == null || !index.TryGetValue(mobile, out var weapons) || !weapons.Remove(weapon))
        {
            return;
        }

        if (weapons.Count == 0)
        {
            index.Remove(mobile);
        }
    }

    private sealed class Context
    {
        public Context(Mobile attacker, Mobile target)
        {
            Attacker = attacker;
            Target = target;
            DamageModifier = InitialDamageModifier;
        }

        public Mobile Attacker { get; }
        public Mobile Target { get; set; }
        public int DamageModifier { get; set; }
    }
}
