using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Mobiles;

namespace Server.Items;

public static class Searing
{
    // 10-15 fire damage is the issue's explicit ServUO-compatibility policy, not an EA-verified value.
    internal const int MinimumFireDamage = 10;
    internal const int MaximumFireDamage = 15;
    internal const int SelfDamage = 4;
    internal const int MeleeProcChance = 20;
    internal const int RangedProcChance = 10;
    internal const int PlayerRegenPenalty = 20;
    internal const int NpcRegenPenalty = 60;
    internal static readonly TimeSpan RegenPenaltyDuration = TimeSpan.FromSeconds(4);

    private static readonly Dictionary<Mobile, Context> _contexts = [];

    public static void Configure()
    {
        EventSink.Logout += ClearTarget;
    }

    internal static int ActiveContextCount => _contexts.Count;

    internal static bool BeginAttack(BaseWeapon weapon, Mobile attacker)
    {
        if (!CanUse(weapon, attacker))
        {
            return false;
        }

        if (attacker.Mana <= 0)
        {
            return false;
        }

        attacker.Mana--;
        return true;
    }

    internal static void TryProcOnNormalHit(BaseWeapon weapon, Mobile attacker, Mobile defender, bool procEligible)
    {
        if (!procEligible || !CanUse(weapon, attacker) || !IsValidTarget(defender))
        {
            return;
        }

        var chance = weapon is BaseRanged ? RangedProcChance : MeleeProcChance;

        if (chance <= Utility.Random(100))
        {
            return;
        }

        attacker.Damage(SelfDamage, attacker);
        AOS.Damage(defender, attacker, Utility.RandomMinMax(MinimumFireDamage, MaximumFireDamage), false, 0, 100, 0, 0, 0);
        ApplyRegenPenalty(defender);
    }

    internal static int GetHitRegenPenalty(Mobile mobile) =>
        mobile != null && _contexts.ContainsKey(mobile) ? mobile.Player ? -PlayerRegenPenalty : -NpcRegenPenalty : 0;

    internal static bool HasContext(Mobile mobile) => mobile != null && _contexts.ContainsKey(mobile);

    internal static void ExpireForTests(Mobile mobile) => Clear(mobile);

    internal static void ClearAll()
    {
        foreach (var context in _contexts.Values)
        {
            context.TimerToken.Cancel();
        }

        _contexts.Clear();
    }

    private static void ClearTarget(Mobile mobile)
    {
        if (mobile != null && _contexts.Remove(mobile, out var context))
        {
            context.TimerToken.Cancel();
        }
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

        if (_contexts.Remove(mobile, out var context))
        {
            context.TimerToken.Cancel();
        }

        if (mobile.Weapon is BaseWeapon weapon)
        {
            weapon.SearingIgnited = false;
        }
    }

    private static void ApplyRegenPenalty(Mobile defender)
    {
        Clear(defender);
        Timer.StartTimer(RegenPenaltyDuration, () => Clear(defender), out var token);
        _contexts[defender] = new Context(token);
        defender.CheckStatTimers();
    }

    private static bool CanUse(BaseWeapon weapon, Mobile attacker) =>
        Core.HS &&
        weapon is { Deleted: false, SearingIgnited: true } &&
        weapon.ExtendedWeaponAttributes.Searing != 0 &&
        attacker?.Deleted == false &&
        attacker.Alive &&
        attacker.Map != null &&
        attacker.Map != Map.Internal &&
        attacker.Weapon == weapon &&
        weapon.Parent == attacker;

    private static bool IsValidTarget(Mobile mobile) =>
        mobile is { Deleted: false, Alive: true } && mobile.Map != null && mobile.Map != Map.Internal;

    private readonly record struct Context(TimerExecutionToken TimerToken);
}
