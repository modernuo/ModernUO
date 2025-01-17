using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ModernUO.CodeGeneratedEvents;
using Server.Collections;
using Server.Logging;
using Server.Mobiles;
using Server.Spells.Ninjitsu;

namespace Server.Misc;

public enum DFAlgorithm
{
    Standard,
    PainSpike
}

public static class StaminaSystem
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(StaminaSystem));

    private static readonly TimeSpan ResetDuration = TimeSpan.FromHours(4);

    private static readonly Dictionary<PlayerMobile, IHasSteps> _etherealMountStepCounters = [];
    private static readonly Dictionary<IHasSteps, StepsTaken> _stepsTaken = [];
    private static readonly HashSet<IHasSteps> _resetHash = [];

    // TODO: This exploits single thread processing and is not thread safe!
    public static DFAlgorithm DFA { get; set; }

    public static int StonesOverweightAllowance { get; set; }
    public static bool CannotMoveWhenFatigued { get; set; }
    public static int StonesPerOverweightLoss { get; set; }
    public static int BaseOverweightLoss { get; set; }
    public static double AdditionalLossWhenBelow { get; set; }
    public static bool EnableMountStamina { get; set; }
    public static bool UseMountStaminaOnlyWhenOverloaded { get; set; }

    // Pub 46 (UOML+)
    public static bool GlobalEtherealMountStamina { get; set; }

    public static void Configure()
    {
        CannotMoveWhenFatigued = ServerConfiguration.GetOrUpdateSetting("stamina.cannotMoveWhenFatigued", !Core.AOS);
        StonesPerOverweightLoss = ServerConfiguration.GetOrUpdateSetting("stamina.stonesPerOverweightLoss", 25);
        StonesOverweightAllowance = ServerConfiguration.GetOrUpdateSetting("stamina.stonesOverweightAllowance", 4);
        BaseOverweightLoss = ServerConfiguration.GetOrUpdateSetting("stamina.baseOverweightLoss", 5);
        AdditionalLossWhenBelow = ServerConfiguration.GetOrUpdateSetting("stamina.additionalLossWhenBelow", 0.10);
        EnableMountStamina = ServerConfiguration.GetOrUpdateSetting("stamina.enableMountStamina", true);
        UseMountStaminaOnlyWhenOverloaded = ServerConfiguration.GetSetting("stamina.useMountStaminaOnlyWhenOverloaded", Core.SA);
        GlobalEtherealMountStamina = ServerConfiguration.GetSetting("stamina.globalEtherealMountStamina", Core.ML);
    }

    public static void Initialize()
    {
        EventSink.Movement += EventSink_Movement;
        EventSink.Logout += Logout;

        // Credit idle time
        using var queue = PooledRefQueue<IHasSteps>.Create();
        foreach (var m in _stepsTaken.Keys)
        {
            // We cannot remove since we are iterating.
            ref var stepsTaken = ref RegenSteps(m, out var exists, removeOnInvalidation: false);

            if (exists && stepsTaken.Steps <= 0)
            {
                queue.Enqueue(m);
            }
        }

        while (queue.Count > 0)
        {
            _stepsTaken.Remove(queue.Dequeue());
        }
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    public static void OnPlayerDeleted(Mobile m)
    {
        RemoveEntry(m as IHasSteps);
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm)
    {
        bool exists;
        if (EnableMountStamina)
        {
            // Start idle for mount
            ref var stepsTaken = ref GetStepsTaken(pm.Mount, out exists);
            if (exists)
            {
                if (stepsTaken.Steps <= 0 || Core.Now >= stepsTaken.IdleStartTime + ResetDuration)
                {
                    _stepsTaken.Remove(pm.Mount);
                }
                else
                {
                    stepsTaken.IdleStartTime = Core.Now;
                }
            }

            _resetHash.Remove(pm.Mount);
        }

        ref var regenStepsTaken = ref RegenSteps(pm, out exists);
        if (exists)
        {
            regenStepsTaken.IdleStartTime = Core.Now;
        }
    }

    private static void Logout(Mobile m)
    {
        if (_stepsTaken == null || _stepsTaken.Count == 0)
        {
            return;
        }

        if (EnableMountStamina)
        {
            // Regain mount idle time
            ref var stepsTaken = ref RegenSteps(m.Mount, out var exists);
            if (exists)
            {
                stepsTaken.IdleStartTime = Core.Now;
                _resetHash.Add(m.Mount);
            }
        }

        if (m is PlayerMobile pm)
        {
            ref var stepsTaken = ref RegenSteps(pm, out var exists);
            if (exists)
            {
                stepsTaken.IdleStartTime = Core.Now;
            }
        }
    }

    private static bool IsEthereal(IHasSteps m, out PlayerMobile pm)
    {
        if (m is not EtherealMount and not VirtualMountItem)
        {
            pm = null;
            return false;
        }

        pm = ((IMount)m).Rider as PlayerMobile;
        return pm != null;
    }

    public static void RemoveEntry(IHasSteps m)
    {
        if (m == null)
        {
            return;
        }

        if (GlobalEtherealMountStamina && IsEthereal(m, out var pm))
        {
            _etherealMountStepCounters.Remove(pm);
        }

        _stepsTaken.Remove(m);
    }

    public static void OnDismount(IMount mount)
    {
        if (!EnableMountStamina)
        {
            return;
        }

        if (RegenSteps(mount))
        {
            _resetHash.Add(mount);
            return;
        }

        _resetHash.Remove(mount);
    }

    private static ref StepsTaken GetStepsTaken(IHasSteps m, out bool exists)
    {
        if (m == null)
        {
            exists = false;
            return ref Unsafe.NullRef<StepsTaken>();
        }

        if (GlobalEtherealMountStamina && IsEthereal(m, out var pm))
        {
            ref var stepsCounter = ref CollectionsMarshal.GetValueRefOrNullRef(_etherealMountStepCounters, pm);
            if (Unsafe.IsNullRef(ref stepsCounter))
            {
                exists = false;
                return ref Unsafe.NullRef<StepsTaken>();
            }

            m = stepsCounter;
        }

        ref var stepsTaken = ref CollectionsMarshal.GetValueRefOrNullRef(_stepsTaken, m);
        exists = !Unsafe.IsNullRef(ref stepsTaken);
        return ref stepsTaken;
    }

    private static ref StepsTaken GetOrCreateStepsTaken(IHasSteps m, out bool created)
    {
        if (GlobalEtherealMountStamina && IsEthereal(m, out var pm))
        {
            ref var stepsCounter = ref CollectionsMarshal.GetValueRefOrAddDefault(_etherealMountStepCounters, pm, out var stepsCounterExists);
            if (!stepsCounterExists)
            {
                stepsCounter = new EtherealMountStepCounter();
            }

            m = stepsCounter;
        }

        ref var stepsTaken = ref CollectionsMarshal.GetValueRefOrAddDefault(_stepsTaken, m, out var exists);
        created = !exists;
        if (created)
        {
            stepsTaken.Entity = m;
        }

        return ref stepsTaken;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RegenSteps(IHasSteps m, int amount = 0, bool removeOnInvalidation = true)
    {
        RegenSteps(m, out var exists, amount, removeOnInvalidation);
        return exists;
    }

    // Triggered on logout, dismount, and world load
    private static ref StepsTaken RegenSteps(IHasSteps m, out bool exists, int amount = 0, bool removeOnInvalidation = true)
    {
        if (m == null)
        {
            exists = false;
            return ref Unsafe.NullRef<StepsTaken>();
        }

        ref var stepsTaken = ref GetStepsTaken(m, out exists);
        if (!exists)
        {
            return ref Unsafe.NullRef<StepsTaken>();
        }

        exists = RegenSteps(ref stepsTaken, amount, removeOnInvalidation);
        return ref stepsTaken;
    }

    private static bool RegenSteps(ref StepsTaken stepsTaken, int amount = 0, bool removeOnInvalidation = true)
    {
        var entity = stepsTaken.Entity;
        var stepsGained = (int)((Core.Now - stepsTaken.IdleStartTime) / entity.IdleTimePerStepsGain * entity.StepsGainedPerIdleTime);

        stepsTaken.Steps -= stepsGained + amount;

        if (stepsTaken.Steps <= 0)
        {
            if (removeOnInvalidation)
            {
                RemoveEntry(entity);
                return false;
            }

            stepsTaken.Steps = 0;
        }

        return true;
    }

    public static void FatigueOnDamage(Mobile m, int damage)
    {
        var fatigue = DFA switch
        {
            DFAlgorithm.Standard  => damage * (100.0 / m.Hits) * ((double)m.Stam / 100) - 5.0,
            DFAlgorithm.PainSpike => damage * (100.0 / m.Hits + (50.0 + m.Stam) / 100 - 1.0) - 5.0,
            _                     => 0.0
        };

        if (fatigue > 0)
        {
            m.Stam -= (int)fatigue;
        }
    }

    public static int GetMaxWeight(Mobile m) => m.MaxWeight;

    public static void EventSink_Movement(MovementEventArgs e)
    {
        var from = e.Mobile;

        if (!from.Player || !from.Alive || from.AccessLevel > AccessLevel.Player)
        {
            return;
        }

        var maxWeight = GetMaxWeight(from) + StonesOverweightAllowance;
        var overweight = Mobile.BodyWeight + from.TotalWeight - maxWeight;

        if (EnableMountStamina && from.Mount != null)
        {
            ProcessMountMovement(from.Mount, overweight, e);
        }
        else
        {
            ProcessPlayerMovement(overweight, e);
        }

        DeathStrike.AddStep(from);
    }

    private static void ProcessPlayerMovement(int overweight, MovementEventArgs e)
    {
        var from = e.Mobile;
        var running = (e.Direction & Direction.Running) != 0;

        if (overweight > 0)
        {
            var stamLoss = GetStamLoss(from, overweight, running);

            from.Stam -= stamLoss;

            if (from.Stam <= 0)
            {
                // You are too fatigued to move, because you are carrying too much weight!
                from.SendLocalizedMessage(500109);
                e.Blocked = true;
                return;
            }
        }

        if (!running)
        {
            return;
        }

        if (AdditionalLossWhenBelow > 0 && from.Stam / Math.Max(from.StamMax, 1.0) < AdditionalLossWhenBelow)
        {
            --from.Stam;
        }

        if (CannotMoveWhenFatigued && from.Stam <= 0)
        {
            from.SendLocalizedMessage(500110); // You are too fatigued to move.
            e.Blocked = true;
            return;
        }

        if (from is PlayerMobile pm)
        {
            ref StepsTaken stepsTaken = ref GetOrCreateStepsTaken(pm, out var created);
            if (!created)
            {
                RegenSteps(ref stepsTaken, removeOnInvalidation: false);
            }

            var steps = ++stepsTaken.Steps;

            // 3x steps if mounted is used when EnableMountStamina is false
            var maxSteps = pm.StepsMax * (from.Mount != null ? 3 : 1);

            if (steps > maxSteps)
            {
                --pm.Stam;
                stepsTaken.Steps = 0;
            }

            stepsTaken.IdleStartTime = Core.Now;
        }
    }

    private static void ProcessMountMovement(IMount mount, int overweight, MovementEventArgs e)
    {
        var from = e.Mobile;

        var maxSteps = mount.StepsMax;
        var running = (e.Direction & Direction.Running) != 0;

        var stamLoss = overweight > 0 ? GetStamLoss(from, overweight, running) : 0;

        if (stamLoss <= 0 && (!running || UseMountStaminaOnlyWhenOverloaded))
        {
            return;
        }

        ref var stepsTaken = ref GetOrCreateStepsTaken(mount, out var created);

        if (!created)
        {
            RegenSteps(ref stepsTaken, removeOnInvalidation: false);
        }

        stepsTaken.Steps += stamLoss + 1;
        stepsTaken.IdleStartTime = Core.Now;

        if (stepsTaken.Steps > maxSteps)
        {
            stepsTaken.Steps = maxSteps;
            from.SendLocalizedMessage(500108); // Your mount is too fatigued to move.
            e.Blocked = true;
        }
    }

    public static int GetStamLoss(Mobile from, int overWeight, bool running)
    {
        var loss = BaseOverweightLoss + overWeight / StonesPerOverweightLoss;

        if (from.Mounted)
        {
            loss /= 3;
        }

        if (running)
        {
            loss *= 2;
        }

        return loss;
    }

    public static bool IsOverloaded(Mobile m)
    {
        if (!m.Player || !m.Alive || m.AccessLevel > AccessLevel.Player)
        {
            return false;
        }

        return Mobile.BodyWeight + m.TotalWeight > GetMaxWeight(m) + StonesOverweightAllowance;
    }

    private struct StepsTaken
    {
        public IHasSteps Entity;
        public int Steps;
        public DateTime IdleStartTime;
    }

    private class ResetTimer : Timer
    {
        private static readonly TimeSpan _checkDuration = TimeSpan.FromHours(1);

        private DateTime _nextCheck;

        public ResetTimer() : base(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5)) => _nextCheck = Core.Now;

        public static void Initialize()
        {
            new ResetTimer().Start();
        }

        ~ResetTimer()
        {
            StaminaSystem.logger.Error($"{nameof(ResetTimer)} is no longer running!");
        }

        protected override void OnTick()
        {
            if (Core.Now < _nextCheck)
            {
                return;
            }

            if (_resetHash.Count > 0)
            {
                using var queue = PooledRefQueue<IHasSteps>.Create();

                ref StepsTaken stepsTaken = ref Unsafe.NullRef<StepsTaken>();
                foreach (var m in _resetHash)
                {
                    stepsTaken = ref GetStepsTaken(m, out var exists);
                    if (!exists || Core.Now >= stepsTaken.IdleStartTime + ResetDuration)
                    {
                        queue.Enqueue(m);
                    }
                }

                if (_resetHash.Count == queue.Count)
                {
                    _resetHash.Clear();
                }
                else
                {
                    while (queue.Count > 0)
                    {
                        _resetHash.Remove(queue.Dequeue());
                    }
                }
            }

            _nextCheck = Core.Now + _checkDuration;
        }
    }

    // Placeholder for a special case where Ethereal mount steps are global to the player
    private class EtherealMountStepCounter : IHasSteps
    {
        // The properties are not actually used
        public int StepsMax => 3840;
        public int StepsGainedPerIdleTime => 1;
        public TimeSpan IdleTimePerStepsGain => TimeSpan.FromSeconds(1);
    }
}
