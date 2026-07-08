using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Collections;
using Server.Engines.BuffIcons;
using Server.Mobiles;

namespace Server.Items;

public static class Sparks
{
    internal const int TickCount = 5;
    internal const int RawDamageMin = 20;
    internal const int RawDamageMax = 40;

    internal static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1.0);
    internal static readonly TimeSpan EffectDuration = TimeSpan.FromSeconds(5.0);

    private static readonly Dictionary<(Mobile Attacker, Mobile Defender), SparksContext> _contexts = [];

    internal static int? RawDamageOverrideForTests { get; set; }

    public static void TryProcOnNormalHit(Mobile attacker, Mobile defender, int hitSparksChance)
    {
        if (!Core.TOL || hitSparksChance <= 0 || attacker == null || defender == null)
        {
            return;
        }

        if (!IsValidCombatant(attacker) || !IsValidCombatant(defender))
        {
            return;
        }

        if (hitSparksChance <= Utility.Random(100))
        {
            return;
        }

        if (_contexts.ContainsKey((attacker, defender)))
        {
            return;
        }

        _contexts[(attacker, defender)] = new SparksContext(attacker, defender);
    }

    internal static int ApplySparksTick(Mobile attacker, Mobile defender, int rawDamage)
    {
        if (!Core.TOL || !IsValidCombatant(attacker) || !IsValidCombatant(defender))
        {
            return 0;
        }

        var raw = rawDamage;

        if (!defender.Player)
        {
            raw *= 2;
        }

        var defenderHits = defender.Hits;
        AOS.Damage(defender, attacker, raw, false, 0, 0, 0, 0, 100);
        var damageApplied = Math.Max(0, defenderHits - defender.Hits);

        if (damageApplied > 0 && attacker is { Deleted: false, Alive: true })
        {
            attacker.Mana = Math.Min(attacker.ManaMax, attacker.Mana + damageApplied);
        }

        return damageApplied;
    }

    internal static bool HasActiveContext(Mobile attacker, Mobile defender) =>
        attacker != null && defender != null && _contexts.ContainsKey((attacker, defender));

    internal static int ActiveContextCount => _contexts.Count;

    internal static int GetTicksRemainingForTests(Mobile attacker, Mobile defender) =>
        _contexts.TryGetValue((attacker, defender), out var context) ? context.TicksRemaining : 0;

    internal static bool TickForTests(Mobile attacker, Mobile defender)
    {
        if (!_contexts.TryGetValue((attacker, defender), out var context))
        {
            return false;
        }

        context.Tick();
        return true;
    }

    internal static void ClearAll()
    {
        foreach (var context in _contexts.Values)
        {
            context.Stop();
        }

        _contexts.Clear();
        RawDamageOverrideForTests = null;
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

        using var pairs = PooledRefList<(Mobile Attacker, Mobile Defender)>.Create();

        foreach (var pair in _contexts.Keys)
        {
            if (pair.Attacker == mobile || pair.Defender == mobile)
            {
                pairs.Add(pair);
            }
        }

        for (var i = 0; i < pairs.Count; i++)
        {
            var pair = pairs[i];
            StopContext(pair.Attacker, pair.Defender);
        }
    }

    private static void StopContext(Mobile attacker, Mobile defender)
    {
        if (_contexts.Remove((attacker, defender), out var context))
        {
            context.Stop();
        }
    }

    private static bool IsValidCombatant(Mobile mobile) =>
        mobile is { Deleted: false, Alive: true } && mobile.Map != null && mobile.Map != Map.Internal;

    private static int RollRawDamage() => RawDamageOverrideForTests ?? Utility.RandomMinMax(RawDamageMin, RawDamageMax);

    private sealed class SparksContext
    {
        private readonly Mobile _attacker;
        private readonly Mobile _defender;
        private TimerExecutionToken _tickTimerToken;

        public SparksContext(Mobile attacker, Mobile defender)
        {
            _attacker = attacker;
            _defender = defender;
            TicksRemaining = TickCount;

            if (_defender is PlayerMobile pm && BuffInfo.Enabled)
            {
                pm.AddBuff(new BuffInfo(BuffIcon.Sparks, 1157330, 1157361, EffectDuration));
            }

            _attacker.PlaySound(0x20A);
            _defender.FixedParticles(0x3818, 1, 11, 0x13A8, 0, 0, EffectLayer.Waist);

            Timer.StartTimer(TickInterval, TickInterval, Tick, out _tickTimerToken);
        }

        public int TicksRemaining { get; private set; }

        public void Stop()
        {
            _tickTimerToken.Cancel();

            if (_defender is PlayerMobile pm && BuffInfo.Enabled)
            {
                pm.RemoveBuff(BuffIcon.Sparks);
            }
        }

        public void Tick()
        {
            if (!IsValidCombatant(_attacker) || !IsValidCombatant(_defender))
            {
                StopContext(_attacker, _defender);
                return;
            }

            ApplySparksTick(_attacker, _defender, RollRawDamage());
            TicksRemaining--;

            if (TicksRemaining <= 0)
            {
                StopContext(_attacker, _defender);
            }
        }
    }
}
