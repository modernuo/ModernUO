using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

public class StaminaSystem : GenericPersistence
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(StaminaSystem));

    private static TimeSpan ResetDuration = TimeSpan.FromHours(24);

    private static readonly Dictionary<IHasSteps, StepsTaken> _stepsTaken = new();
    private static readonly OrderedHashSet<IHasSteps> _resetHash = new();

    // TODO: This exploits single thread processing and is not thread safe!
    public static DFAlgorithm DFA { get; set; }

    public static int StonesOverweightAllowance { get; set; }
    public static bool CannotMoveWhenFatigued { get; set; }
    public static int StonesPerOverweightLoss { get; set; }
    public static int BaseOverweightLoss { get; set; }
    public static double AdditionalLossWhenBelow { get; set; }
    public static bool EnableMountStamina { get; set; }
    public static bool UseMountStaminaOnlyWhenOverloaded { get; set; }

    public static void Configure()
    {
        CannotMoveWhenFatigued = ServerConfiguration.GetOrUpdateSetting("stamina.cannotMoveWhenFatigued", !Core.AOS);
        StonesPerOverweightLoss = ServerConfiguration.GetOrUpdateSetting("stamina.stonesPerOverweightLoss", 25);
        StonesOverweightAllowance = ServerConfiguration.GetOrUpdateSetting("stamina.stonesOverweightAllowance", 4);
        BaseOverweightLoss = ServerConfiguration.GetOrUpdateSetting("stamina.baseOverweightLoss", 5);
        AdditionalLossWhenBelow = ServerConfiguration.GetOrUpdateSetting("stamina.additionalLossWhenBelow", 0.10);
        EnableMountStamina = ServerConfiguration.GetOrUpdateSetting("stamina.enableMountStamina", true);
        UseMountStaminaOnlyWhenOverloaded = ServerConfiguration.GetSetting("stamina.useMountStaminaOnlyWhenOverloaded", Core.SA);
    }

    public StaminaSystem() : base("StaminaSystem", 10)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEncodedInt(_stepsTaken.Count);
        foreach (var (m, stepsTaken) in _stepsTaken)
        {
            writer.Write(m as ISerializable); // To serialize all IHasSteps must be an ISerializable
            stepsTaken.Serialize(writer);
        }
    }

    public override void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        var count = reader.ReadEncodedInt();
        _stepsTaken.EnsureCapacity(count);

        var now = Core.Now;

        for (var i = 0; i < count; i++)
        {
            var m = reader.ReadEntity<ISerializable>() as IHasSteps;
            var stepsTaken = new StepsTaken();
            stepsTaken.Deserialize(reader);

            if (m == null)
            {
                continue;
            }

            RegenSteps(m, ref stepsTaken, false);

            if (stepsTaken.Steps > 0)
            {
                _stepsTaken.Add(m, stepsTaken);

                if (m is IMount && now < stepsTaken.IdleStartTime + ResetDuration)
                {
                    _resetHash.Add(m);
                }
            }
        }
    }

    public static void Initialize()
    {
        EventSink.Movement += EventSink_Movement;
        EventSink.Login += Login;
        EventSink.Logout += Logout;
        EventSink.PlayerDeleted += OnPlayerDeleted;

        // Credit idle time
        using var queue = PooledRefQueue<IHasSteps>.Create();
        foreach (var m in _stepsTaken.Keys)
        {
            ref var stepsTaken = ref CollectionsMarshal.GetValueRefOrNullRef(_stepsTaken, m);
            if (!Unsafe.IsNullRef(ref stepsTaken))
            {
                RegenSteps(m, ref stepsTaken, false);
                if (stepsTaken.Steps <= 0)
                {
                    queue.Enqueue(m);
                }
            }
        }

        while (queue.Count > 0)
        {
            _stepsTaken.Remove(queue.Dequeue());
        }
    }

    private static void OnPlayerDeleted(Mobile m)
    {
        RemoveEntry(m as IHasSteps);
    }

    private static void Login(Mobile m)
    {
        if (EnableMountStamina)
        {
            // Start idle for mount
            ref var stepsTaken = ref GetMountStepsTaken(m.Mount, out var exists);
            if (exists)
            {
                if (stepsTaken.Steps <= 0 || Core.Now >= stepsTaken.IdleStartTime + ResetDuration)
                {
                    _stepsTaken.Remove(m.Mount);
                }
                else
                {
                    stepsTaken.IdleStartTime = Core.Now;
                }
            }

            _resetHash.Remove(m.Mount);
        }

        if (m is PlayerMobile pm)
        {
            ref var stepsTaken = ref CollectionsMarshal.GetValueRefOrNullRef(_stepsTaken, pm);
            if (!Unsafe.IsNullRef(ref stepsTaken) && RegenSteps(pm, ref stepsTaken))
            {
                stepsTaken.IdleStartTime = Core.Now;
            }
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
            ref var stepsTaken = ref GetMountStepsTaken(m.Mount, out var exists);
            if (exists)
            {
                if (RegenSteps(m.Mount, ref stepsTaken))
                {
                    stepsTaken.IdleStartTime = Core.Now;
                    _resetHash.Add(m.Mount);
                }
            }
        }

        if (m is PlayerMobile pm)
        {
            ref var stepsTaken = ref CollectionsMarshal.GetValueRefOrNullRef(_stepsTaken, pm);

            if (!Unsafe.IsNullRef(ref stepsTaken) && RegenSteps(pm, ref stepsTaken))
            {
                stepsTaken.IdleStartTime = Core.Now;
            }
        }
    }

    public static void RemoveEntry(IHasSteps m)
    {
        if (m != null)
        {
            _stepsTaken.Remove(m);
        }
    }

    public static void OnDismount(IHasSteps mount)
    {
        if (!EnableMountStamina)
        {
            return;
        }

        ref var stepsTaken = ref GetMountStepsTaken(mount, out var exists);
        if (exists && RegenSteps(mount, ref stepsTaken))
        {
            _resetHash.Add(mount);
            return;
        }

        _resetHash.Remove(mount);
    }

    private static ref StepsTaken GetMountStepsTaken(IHasSteps m, out bool exists)
    {
        if (m == null)
        {
            exists = false;
            return ref Unsafe.NullRef<StepsTaken>();
        }

        ref var stepsTaken = ref CollectionsMarshal.GetValueRefOrNullRef(_stepsTaken, m);
        exists = !Unsafe.IsNullRef(ref stepsTaken);
        return ref stepsTaken;
    }

    private static ref StepsTaken GetOrCreateStepsTaken(IHasSteps m, out bool created)
    {
        ref var stepsTaken = ref CollectionsMarshal.GetValueRefOrAddDefault(_stepsTaken, m, out var exists);
        created = !exists;

        return ref stepsTaken;
    }

    public static void RegenSteps(IHasSteps m, int amount, bool removeOnInvalidation = true)
    {
        ref var stepsTaken = ref GetMountStepsTaken(m, out var exists);
        if (exists)
        {
            RegenSteps(m, ref stepsTaken, removeOnInvalidation);
        }
    }

    // Triggered on logout, dismount, and world load
    private static bool RegenSteps(IHasSteps m, ref StepsTaken stepsTaken, bool removeOnInvalidation = true)
    {
        var stepsGained = (int)((Core.Now - stepsTaken.IdleStartTime) / m.IdleTimePerStepsGain * m.StepsGainedPerIdleTime);
        return RegenSteps(m, stepsGained, ref stepsTaken, removeOnInvalidation);
    }

    private static bool RegenSteps(IHasSteps m, int amount, ref StepsTaken stepsTaken, bool removeOnInvalidation = true)
    {
        if (m == null || Unsafe.IsNullRef(ref stepsTaken))
        {
            return false;
        }

        stepsTaken.Steps -= amount;

        if (stepsTaken.Steps <= 0)
        {
            if (removeOnInvalidation)
            {
                _stepsTaken.Remove(m);
                stepsTaken = ref Unsafe.NullRef<StepsTaken>();
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

            if (from.Stam == 0)
            {
                // You are too fatigued to move, because you are carrying too much weight!
                from.SendLocalizedMessage(500109);
                e.Blocked = true;
                return;
            }
        }

        if (AdditionalLossWhenBelow > 0 && from.Stam / Math.Max(from.StamMax, 1.0) < AdditionalLossWhenBelow)
        {
            --from.Stam;
        }

        if (CannotMoveWhenFatigued && from.Stam == 0)
        {
            from.SendLocalizedMessage(500110); // You are too fatigued to move.
            e.Blocked = true;
            return;
        }

        if (running && from is PlayerMobile pm)
        {
            ref StepsTaken stepsTaken = ref GetOrCreateStepsTaken(pm, out var created);
            if (!created)
            {
                RegenSteps(pm, ref stepsTaken, false);
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

        var running = (e.Direction & Direction.Running) != 0;
        var stamLoss = overweight > 0 ? GetStamLoss(from, overweight, running) : 0;

        ref var stepsTaken = ref GetOrCreateStepsTaken(mount, out var created);

        // Gain any idle steps
        if (!created)
        {
            RegenSteps(mount, ref stepsTaken, false); // Don't delete the entry if it's reset
        }

        if (mount is Mobile m && AdditionalLossWhenBelow > 0 && m.Stam / Math.Max(m.StamMax, 1.0) < AdditionalLossWhenBelow)
        {
            stamLoss++;
        }

        var maxSteps = mount.StepsMax;

        if (stepsTaken.Steps <= maxSteps)
        {
            // Pre-SA mounts would lose stamina while running even when they were not overweight
            if (running && !UseMountStaminaOnlyWhenOverloaded)
            {
                stamLoss++;
            }

            if (stamLoss > 0)
            {
                stepsTaken.Steps += stamLoss;
                stepsTaken.IdleStartTime = Core.Now;

                // This only executes when mounted, so we have the player say it since the actual mount is internalized
                if ((mount as BaseCreature)?.Debug == true && stepsTaken.Steps % 20 == 0)
                {
                    from.PublicOverheadMessage(MessageType.Regular, 41, false, $"Steps {stepsTaken.Steps}/{mount.StepsMax}");
                }
            }
        }

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
        public int Steps;
        public DateTime IdleStartTime;

        public void Serialize(IGenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(Steps);
            writer.WriteDeltaTime(IdleStartTime);
        }

        public void Deserialize(IGenericReader reader)
        {
            reader.ReadEncodedInt(); // version

            Steps = reader.ReadEncodedInt();
            IdleStartTime = reader.ReadDeltaTime();
        }
    }

    private class ResetTimer : Timer
    {
        private static TimeSpan CheckDuration = TimeSpan.FromHours(1);

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

            using var queue = PooledRefQueue<IHasSteps>.Create();

            ref StepsTaken stepsTaken = ref Unsafe.NullRef<StepsTaken>();
            foreach (var m in _resetHash)
            {
                stepsTaken = ref GetMountStepsTaken(m, out var exists);
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

            _nextCheck = Core.Now + CheckDuration;
        }
    }
}
