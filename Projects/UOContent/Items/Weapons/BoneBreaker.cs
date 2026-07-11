using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Collections;
using Server.Mobiles;

namespace Server.Items;

public static class BoneBreaker
{
    internal const int Damage = 50;
    internal const int DrainChance = 20;
    internal const int ManaThreshold = 30;
    internal const int DrainTickCount = 4;

    internal static readonly TimeSpan DrainTickInterval = TimeSpan.FromSeconds(1.0);
    internal static readonly TimeSpan ImmunityDuration = TimeSpan.FromSeconds(60.0);

    private static readonly Dictionary<Mobile, DrainContext> _drains = [];
    private static readonly Dictionary<Mobile, TimerExecutionToken> _immunities = [];

    public static void Configure()
    {
        EventSink.Logout += Clear;
        EventSink.MapChanged += OnMapChanged;
    }

    private static void OnMapChanged(Mobile mobile, Map oldMap) => Clear(mobile);

    public static void TryProcOnNormalHit(Mobile attacker, Mobile defender, int boneBreaker)
    {
        TryProcOnNormalHit(attacker, defender, boneBreaker, DrainChance);
    }

    internal static void TryProcOnNormalHit(Mobile attacker, Mobile defender, int boneBreaker, int drainChance)
    {
        if (!Core.TOL || boneBreaker <= 0 || !IsValidCombatant(attacker) || !IsValidCombatant(defender) ||
            IsImmune(defender))
        {
            return;
        }

        if (attacker.Mana >= ManaThreshold)
        {
            var manaCost = GetManaCost(attacker);
            attacker.Mana -= manaCost;
            AOS.Damage(defender, attacker, Damage, false, 100, 0, 0, 0, 0);
        }

        if (drainChance > Utility.Random(100) && IsValidCombatant(defender) && !_drains.ContainsKey(defender))
        {
            _drains[defender] = new DrainContext(attacker, defender);
        }
    }

    public static bool HasActiveDrain(Mobile mobile) => mobile != null && _drains.ContainsKey(mobile);

    internal static bool IsImmune(Mobile mobile) => mobile != null && _immunities.ContainsKey(mobile);

    internal static int ActiveDrainCount => _drains.Count;

    internal static int GetManaCost(Mobile attacker)
    {
        var lowerManaCost = Math.Min(AosAttributes.GetValue(attacker, AosAttribute.LowerManaCost), 40);
        return (int)(ManaThreshold * (1.0 - lowerManaCost / 100.0));
    }

    internal static bool TickForTests(Mobile defender)
    {
        if (!_drains.TryGetValue(defender, out var context))
        {
            return false;
        }

        context.Tick();
        return true;
    }

    internal static void ExpireImmunityForTests(Mobile mobile) => RemoveImmunity(mobile);

    internal static void ClearAll()
    {
        foreach (var context in _drains.Values)
        {
            context.Stop();
        }

        _drains.Clear();

        foreach (var token in _immunities.Values)
        {
            token.Cancel();
        }

        _immunities.Clear();
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

        using var defenders = PooledRefList<Mobile>.Create();

        foreach (var (defender, context) in _drains)
        {
            if (defender == mobile || context.Attacker == mobile)
            {
                defenders.Add(defender);
            }
        }

        for (var i = 0; i < defenders.Count; i++)
        {
            StopDrain(defenders[i], false);
        }

        RemoveImmunity(mobile);
    }

    private static bool IsValidCombatant(Mobile mobile) =>
        mobile is { Deleted: false, Alive: true } && mobile.Map != null && mobile.Map != Map.Internal;

    private static void StopDrain(Mobile defender, bool grantImmunity)
    {
        if (!_drains.Remove(defender, out var context))
        {
            return;
        }

        context.Stop();

        if (grantImmunity)
        {
            AddImmunity(defender);
        }
    }

    private static void AddImmunity(Mobile defender)
    {
        if (_immunities.ContainsKey(defender))
        {
            return;
        }

        Timer.StartTimer(ImmunityDuration, () => RemoveImmunity(defender), out var token);
        _immunities[defender] = token;
    }

    private static void RemoveImmunity(Mobile mobile)
    {
        if (mobile != null && _immunities.Remove(mobile, out var token))
        {
            token.Cancel();
        }
    }

    private sealed class DrainContext
    {
        private readonly Mobile _defender;
        private TimerExecutionToken _tickTimerToken;

        public DrainContext(Mobile attacker, Mobile defender)
        {
            Attacker = attacker;
            _defender = defender;
            TicksRemaining = DrainTickCount;
            Timer.StartTimer(DrainTickInterval, DrainTickInterval, Tick, out _tickTimerToken);
        }

        public Mobile Attacker { get; }

        public int TicksRemaining { get; private set; }

        public void Stop() => _tickTimerToken.Cancel();

        public void Tick()
        {
            if (!IsValidCombatant(Attacker) || !IsValidCombatant(_defender))
            {
                StopDrain(_defender, false);
                return;
            }

            var staminaDrain = Math.Max(1, _defender.StamMax / 10);
            _defender.Stam = Math.Max(1, _defender.Stam - staminaDrain);
            TicksRemaining--;

            if (TicksRemaining <= 0)
            {
                StopDrain(_defender, true);
            }
        }
    }
}
