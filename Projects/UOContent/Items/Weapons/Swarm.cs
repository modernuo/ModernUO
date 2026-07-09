using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Collections;
using Server.Engines.BuffIcons;
using Server.Mobiles;

namespace Server.Items;

public static class Swarm
{
    internal const int TickCount = 3;
    internal const int RawDamage = 10;

    internal static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(5.0);
    internal static readonly TimeSpan EffectDuration = TimeSpan.FromSeconds(15.0);

    private static readonly Dictionary<Mobile, SwarmContext> _contexts = [];

    public static void TryProcOnNormalHit(Mobile attacker, Mobile defender, int hitSwarmChance)
    {
        if (!Core.TOL || hitSwarmChance <= 0 || attacker == null || defender == null)
        {
            return;
        }

        if (!IsValidCombatant(attacker) || !IsValidCombatant(defender))
        {
            return;
        }

        if (hitSwarmChance <= Utility.Random(100))
        {
            return;
        }

        if (_contexts.ContainsKey(defender))
        {
            return;
        }

        _contexts[defender] = new SwarmContext(attacker, defender);
    }

    internal static int ApplySwarmTick(Mobile attacker, Mobile defender, int rawDamage)
    {
        if (!Core.TOL || !IsValidCombatant(attacker) || !IsValidCombatant(defender))
        {
            return 0;
        }

        if (HasBurningTorchEquipped(defender))
        {
            ClearDefender(defender);
            return 0;
        }

        var defenderHits = defender.Hits;
        AOS.Damage(defender, attacker, rawDamage, false, 100, 0, 0, 0, 0);
        return Math.Max(0, defenderHits - defender.Hits);
    }

    internal static bool HasActiveContext(Mobile attacker, Mobile defender) =>
        attacker != null && defender != null && _contexts.TryGetValue(defender, out var context) && context.Attacker == attacker;

    internal static bool HasActiveContext(Mobile defender) => defender != null && _contexts.ContainsKey(defender);

    internal static int ActiveContextCount => _contexts.Count;

    internal static int GetTicksRemainingForTests(Mobile attacker, Mobile defender) =>
        HasActiveContext(attacker, defender) ? _contexts[defender].TicksRemaining : 0;

    internal static bool TickForTests(Mobile attacker, Mobile defender)
    {
        if (!HasActiveContext(attacker, defender))
        {
            return false;
        }

        _contexts[defender].Tick();
        return true;
    }

    internal static void ClearAll()
    {
        foreach (var context in _contexts.Values)
        {
            context.Stop();
        }

        _contexts.Clear();
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

        foreach (var (defender, context) in _contexts)
        {
            if (context.Attacker == mobile || defender == mobile)
            {
                defenders.Add(defender);
            }
        }

        for (var i = 0; i < defenders.Count; i++)
        {
            StopContext(defenders[i]);
        }
    }

    public static void ClearDefender(Mobile defender)
    {
        if (defender == null)
        {
            return;
        }

        StopContext(defender);
    }

    private static void StopContext(Mobile defender)
    {
        if (_contexts.Remove(defender, out var context))
        {
            context.Stop();
        }
    }

    private static bool HasBurningTorchEquipped(Mobile mobile) =>
        mobile.FindItemOnLayer<Torch>(Layer.TwoHanded) is { Burning: true };

    private static bool IsValidCombatant(Mobile mobile) =>
        mobile is { Deleted: false, Alive: true } && mobile.Map != null && mobile.Map != Map.Internal;

    private sealed class SwarmContext
    {
        private readonly Mobile _defender;
        private TimerExecutionToken _tickTimerToken;

        public SwarmContext(Mobile attacker, Mobile defender)
        {
            Attacker = attacker;
            _defender = defender;
            TicksRemaining = TickCount;

            if (_defender is PlayerMobile pm && BuffInfo.Enabled)
            {
                pm.AddBuff(new BuffInfo(BuffIcon.Swarm, 1157328, 1157362, EffectDuration));
            }

            Attacker.PlaySound(0x00E);
            Timer.StartTimer(TickInterval, TickInterval, Tick, out _tickTimerToken);
        }

        public Mobile Attacker { get; }

        public int TicksRemaining { get; private set; }

        public void Stop()
        {
            _tickTimerToken.Cancel();

            if (_defender is PlayerMobile pm && BuffInfo.Enabled)
            {
                pm.RemoveBuff(BuffIcon.Swarm);
            }
        }

        public void Tick()
        {
            if (!IsValidCombatant(Attacker) || !IsValidCombatant(_defender) || HasBurningTorchEquipped(_defender))
            {
                StopContext(_defender);
                return;
            }

            ApplySwarmTick(Attacker, _defender, RawDamage);
            TicksRemaining--;

            if (TicksRemaining <= 0)
            {
                StopContext(_defender);
            }
        }
    }
}
