using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Mobiles;

namespace Server.Items;

public static class BattleLust
{
    internal const int DamageThreshold = 30;
    internal const int MaxPoints = 15;
    internal const int PvpDamageCap = 45;
    internal const int PvmDamageCap = 90;

    private static readonly TimeSpan GainDelay = TimeSpan.FromSeconds(2.0);
    private static readonly TimeSpan DecayDelay = TimeSpan.FromSeconds(6.0);

    private static readonly Dictionary<Mobile, BattleLustContext> _contexts = [];

    public static void OnDamageTaken(Mobile damaged, Mobile source, int damage)
    {
        if (damaged == null)
        {
            return;
        }

        if (!CanGain(damaged, source, damage))
        {
            if (_contexts.TryGetValue(damaged, out var existing) && !IsValidOwner(damaged))
            {
                Remove(damaged, existing);
            }

            return;
        }

        if (!_contexts.TryGetValue(damaged, out var context))
        {
            context = new BattleLustContext(damaged);
            _contexts[damaged] = context;
        }

        context.TryGain();
    }

    internal static int GetDamageBonus(Mobile attacker, Mobile defender)
    {
        if (attacker == null || defender?.Deleted != false)
        {
            return 0;
        }

        if (!_contexts.TryGetValue(attacker, out var context))
        {
            return 0;
        }

        if (!IsValidOwner(attacker))
        {
            Remove(attacker, context);
            return 0;
        }

        var opponentCount = GetActiveAggressedCount(attacker);

        if (opponentCount <= 0)
        {
            return 0;
        }

        var cap = defender.Player ? PvpDamageCap : PvmDamageCap;
        return Math.Min(context.Points * opponentCount, cap);
    }

    internal static int GetPoints(Mobile mobile)
    {
        if (mobile == null || !_contexts.TryGetValue(mobile, out var context))
        {
            return 0;
        }

        if (!IsValidOwner(mobile))
        {
            Remove(mobile, context);
            return 0;
        }

        return context.Points;
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
    public static void Clear(Mobile mobile)
    {
        if (mobile != null && _contexts.Remove(mobile, out var context))
        {
            context.Stop();
        }
    }

    private static bool CanGain(Mobile damaged, Mobile source, int damage) =>
        Core.SA &&
        damage >= DamageThreshold &&
        source?.Deleted == false &&
        source.Alive &&
        source != damaged &&
        IsValidOwner(damaged);

    private static bool IsValidOwner(Mobile mobile) =>
        Core.SA &&
        mobile?.Deleted == false &&
        mobile.Alive &&
        mobile.Map != null &&
        mobile.Map != Map.Internal &&
        mobile.Weapon is BaseWeapon weapon &&
        weapon.WeaponAttributes.BattleLust != 0;

    private static int GetActiveAggressedCount(Mobile mobile)
    {
        var count = 0;
        var list = mobile.Aggressed;

        for (var i = 0; i < list.Count; i++)
        {
            var info = list[i];
            var defender = info.Defender;

            if (!info.Expired && defender?.Deleted == false && defender.Alive)
            {
                count++;
            }
        }

        return count;
    }

    private static void Remove(Mobile mobile, BattleLustContext context)
    {
        _contexts.Remove(mobile);
        context.Stop();
    }

    private sealed class BattleLustContext
    {
        private readonly Mobile _owner;
        private TimerExecutionToken _decayTimerToken;
        private DateTime _nextGain;

        public BattleLustContext(Mobile owner)
        {
            _owner = owner;
            Timer.StartTimer(DecayDelay, DecayDelay, Decay, out _decayTimerToken);
        }

        public int Points { get; private set; }

        public void TryGain()
        {
            if (Core.Now < _nextGain)
            {
                return;
            }

            if (Points < MaxPoints)
            {
                Points++;
            }

            _nextGain = Core.Now + GainDelay;
        }

        public void Stop()
        {
            _decayTimerToken.Cancel();
        }

        private void Decay()
        {
            if (!IsValidOwner(_owner))
            {
                BattleLust.Clear(_owner);
                return;
            }

            Points--;

            if (Points <= 0)
            {
                BattleLust.Clear(_owner);
            }
        }
    }
}