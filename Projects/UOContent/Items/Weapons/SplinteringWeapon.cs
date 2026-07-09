using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Collections;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Network;

namespace Server.Items;

public static class SplinteringWeapon
{
    internal const int DurabilityLoss = 10;
    internal const int TickCount = 5;

    internal static readonly TimeSpan ForcedWalkDuration = TimeSpan.FromSeconds(4.0);
    internal static readonly TimeSpan BleedTickInterval = TimeSpan.FromSeconds(2.0);
    internal static readonly TimeSpan PlayerImmunityDuration = TimeSpan.FromSeconds(15.0);

    private static readonly Dictionary<(Mobile Attacker, Mobile Defender), SplinteringContext> _contexts = [];
    private static readonly Dictionary<Mobile, TimerExecutionToken> _playerImmunity = [];

    public static void Initialize()
    {
        EventSink.Logout += Clear;
        EventSink.Movement += OnMovement;
    }

    public static bool TryProcOnEligibleHit(
        Mobile attacker,
        Mobile defender,
        BaseWeapon weapon,
        WeaponAbility ability,
        int splinteringChance
    )
    {
        if (!Core.SA || splinteringChance <= 0 || weapon == null || attacker == null || defender == null)
        {
            return false;
        }

        if (!IsValidCombatant(attacker) || !IsValidCombatant(defender))
        {
            return false;
        }

        if (IsExcludedAbility(ability) || IsPlayerImmune(defender))
        {
            return false;
        }

        if (splinteringChance <= Utility.Random(100))
        {
            return false;
        }

        var key = (attacker, defender);
        if (_contexts.ContainsKey(key))
        {
            return false;
        }

        _contexts[key] = new SplinteringContext(attacker, defender);

        if (defender is PlayerMobile)
        {
            AddPlayerImmunity(defender);
        }

        return true;
    }

    internal static bool IsForceWalking(Mobile defender)
    {
        if (defender == null)
        {
            return false;
        }

        foreach (var context in _contexts.Values)
        {
            if (context.Defender == defender && context.ForceWalking)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsPlayerImmune(Mobile defender) =>
        defender is PlayerMobile && _playerImmunity.ContainsKey(defender);

    internal static int ActiveContextCount => _contexts.Count;

    internal static bool EndForceWalkForTests(Mobile attacker, Mobile defender)
    {
        if (attacker == null || defender == null || !_contexts.TryGetValue((attacker, defender), out var context))
        {
            return false;
        }

        context.EndForceWalk();
        return true;
    }

    internal static bool TickForTests(Mobile attacker, Mobile defender)
    {
        if (attacker == null || defender == null || !_contexts.TryGetValue((attacker, defender), out var context))
        {
            return false;
        }

        context.OnBleedTick();
        return true;
    }

    internal static void ExpirePlayerImmunityForTests(Mobile mobile) => RemovePlayerImmunity(mobile);

    internal static void ClearAll()
    {
        foreach (var context in _contexts.Values)
        {
            context.Stop();
        }

        _contexts.Clear();

        foreach (var token in _playerImmunity.Values)
        {
            token.Cancel();
        }

        _playerImmunity.Clear();
    }

    internal static void ClearEffects(Mobile mobile)
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
            StopContext(pairs[i]);
        }
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
    public static void Clear(Mobile mobile)
    {
        ClearEffects(mobile);
        RemovePlayerImmunity(mobile);
    }

    internal static void OnMovement(MovementEventArgs e)
    {
        if ((e.Direction & Direction.Running) != 0 && IsForceWalking(e.Mobile) && e.Mobile.AccessLevel < AccessLevel.GameMaster)
        {
            e.Blocked = true;
        }
    }

    private static bool IsExcludedAbility(WeaponAbility ability) => ability is Disarm or InfectiousStrike;

    private static bool IsValidCombatant(Mobile mobile) =>
        mobile is { Deleted: false, Alive: true } && mobile.Map != null && mobile.Map != Map.Internal;

    private static void AddPlayerImmunity(Mobile defender)
    {
        if (defender is not PlayerMobile || _playerImmunity.ContainsKey(defender))
        {
            return;
        }

        Timer.StartTimer(PlayerImmunityDuration, () => RemovePlayerImmunity(defender), out var token);
        _playerImmunity[defender] = token;
    }

    private static void RemovePlayerImmunity(Mobile mobile)
    {
        if (mobile != null && _playerImmunity.Remove(mobile, out var token))
        {
            token.Cancel();
        }
    }

    private static void StopContext((Mobile Attacker, Mobile Defender) key)
    {
        if (_contexts.Remove(key, out var context))
        {
            context.Stop();
        }
    }

    private sealed class SplinteringContext
    {
        private readonly Mobile _attacker;
        private readonly Mobile _defender;
        private TimerExecutionToken _forcedWalkTimerToken;
        private TimerExecutionToken _bleedTimerToken;
        private int _bleedLevel = TickCount;

        public SplinteringContext(Mobile attacker, Mobile defender)
        {
            _attacker = attacker;
            _defender = defender;
            ForceWalking = true;

            StartForceWalk();

            defender.SendLocalizedMessage(1112486); // A shard of the brittle weapon has become lodged in you!
            attacker.SendLocalizedMessage(1113077); // A shard of your blade breaks off and sticks in your opponent!
            Effects.PlaySound(defender.Location, defender.Map, 0x1DF);

            if (defender is PlayerMobile pm && BuffInfo.Enabled)
            {
                pm.AddBuff(new BuffInfo(BuffIcon.Unknown2, 1154670, 1152144, ForcedWalkDuration));
            }

            Timer.StartTimer(ForcedWalkDuration, EndForceWalk, out _forcedWalkTimerToken);
            Timer.StartTimer(BleedTickInterval, BleedTickInterval, OnBleedTick, out _bleedTimerToken);
        }

        public Mobile Defender => _defender;

        public bool ForceWalking { get; private set; }

        public void Stop()
        {
            _forcedWalkTimerToken.Cancel();
            _bleedTimerToken.Cancel();
            EndForceWalk();
        }

        private void StartForceWalk()
        {
            if (_defender.NetState != null && _defender.AccessLevel < AccessLevel.GameMaster)
            {
                _defender.NetState.SendSpeedControl(SpeedControlSetting.Walk);
            }
        }

        public void EndForceWalk()
        {
            if (!ForceWalking)
            {
                return;
            }

            ForceWalking = false;

            if (_defender.NetState != null)
            {
                _defender.NetState.SendSpeedControl(SpeedControlSetting.Disable);
            }

            if (_defender is PlayerMobile pm && BuffInfo.Enabled)
            {
                pm.RemoveBuff(BuffIcon.Unknown2);
            }

            _defender.SendLocalizedMessage(1112487); // The shard is successfully removed.
        }

        public void OnBleedTick()
        {
            if (!IsValidCombatant(_attacker) || !IsValidCombatant(_defender))
            {
                StopContext((_attacker, _defender));
                return;
            }

            BleedAttack.DoBleed(_defender, _attacker, _bleedLevel, bloodDrinker: false);
            _bleedLevel--;

            if (_bleedLevel <= 0)
            {
                StopContext((_attacker, _defender));
            }
        }
    }
}
