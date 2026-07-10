using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Mobiles;

namespace Server.Items;

public static class DamageEater
{
    internal const int SpecificCap = 30;
    internal const int AllTypesCap = 18;
    internal const int MaxCharges = 20;
    internal const int EffectCliloc = 1113617;

    internal static readonly TimeSpan ConversionDelay = TimeSpan.FromSeconds(3.0);

    // UO.com describes conversion every three seconds from the last received damage.
    // Treat each queued type portion as one charge: after a quiet interval, one charge converts,
    // and remaining charges continue at the same cadence. Any new damage resets that interval.

    private static readonly Dictionary<Mobile, DamageEaterContext> _contexts = [];

    public static void OnDamageTaken(
        Mobile defender,
        int appliedDamage,
        int physicalDamage,
        int fireDamage,
        int coldDamage,
        int poisonDamage,
        int energyDamage,
        int directDamage
    )
    {
        if (!IsValidOwner(defender) || appliedDamage <= 0)
        {
            Clear(defender);
            return;
        }

        if (!HasValue(defender))
        {
            Clear(defender);
            return;
        }

        if (!_contexts.TryGetValue(defender, out var context))
        {
            context = new DamageEaterContext(defender);
            _contexts[defender] = context;
        }

        context.AddDamage(
            appliedDamage,
            physicalDamage,
            fireDamage,
            coldDamage,
            poisonDamage,
            energyDamage,
            directDamage
        );

        if (context.PendingCharges == 0)
        {
            Clear(defender);
        }
    }

    internal static int GetPendingChargesForTests(Mobile mobile) =>
        _contexts.TryGetValue(mobile, out var context) ? context.PendingCharges : 0;

    internal static int GetPendingHealingForTests(Mobile mobile) =>
        _contexts.TryGetValue(mobile, out var context) ? context.PendingHealing : 0;

    internal static bool TickForTests(Mobile mobile)
    {
        if (!_contexts.TryGetValue(mobile, out var context))
        {
            return false;
        }

        context.Convert();
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

    internal static void ClearIfInactive(Mobile mobile)
    {
        if (mobile != null && _contexts.ContainsKey(mobile) && !HasValue(mobile))
        {
            Clear(mobile);
        }
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

    private static bool HasValue(Mobile mobile) => AbsorptionAttributes.HasDamageEaterOn(mobile);

    private static bool IsValidOwner(Mobile mobile) =>
        Core.SA && mobile?.Deleted == false && mobile.Alive && mobile.Map != null && mobile.Map != Map.Internal;

    private static int ScaleDamagePortion(int appliedDamage, int portion, int totalPortion)
    {
        return (int)((long)appliedDamage * portion / totalPortion);
    }

    private sealed class DamageEaterContext
    {
        private readonly Mobile _owner;
        private readonly Queue<int> _pending = [];
        private TimerExecutionToken _conversionToken;
        private DateTime _nextConversion;
        private int _pendingHealing;

        public DamageEaterContext(Mobile owner)
        {
            _owner = owner;
        }

        public int PendingCharges => _pending.Count;
        public int PendingHealing => _pendingHealing;

        public void AddDamage(
            int appliedDamage,
            int physicalDamage,
            int fireDamage,
            int coldDamage,
            int poisonDamage,
            int energyDamage,
            int directDamage
        )
        {
            var totalPortion = physicalDamage + fireDamage + coldDamage + poisonDamage + energyDamage + directDamage;

            if (totalPortion <= 0)
            {
                return;
            }

            QueueHealing(
                ScaleDamagePortion(appliedDamage, physicalDamage, totalPortion),
                AbsorptionAttribute.KineticEater
            );
            QueueHealing(
                ScaleDamagePortion(appliedDamage, fireDamage, totalPortion),
                AbsorptionAttribute.FireEater
            );
            QueueHealing(
                ScaleDamagePortion(appliedDamage, coldDamage, totalPortion),
                AbsorptionAttribute.ColdEater
            );
            QueueHealing(
                ScaleDamagePortion(appliedDamage, poisonDamage, totalPortion),
                AbsorptionAttribute.PoisonEater
            );
            QueueHealing(
                ScaleDamagePortion(appliedDamage, energyDamage, totalPortion),
                AbsorptionAttribute.EnergyEater
            );
            QueueDirectHealing(ScaleDamagePortion(appliedDamage, directDamage, totalPortion));

            if (_pending.Count > 0)
            {
                _nextConversion = Core.Now + ConversionDelay;
                _conversionToken.Cancel();
                Timer.StartTimer(ConversionDelay, Convert, out _conversionToken);
            }
        }

        public void Convert()
        {
            if (Core.Now < _nextConversion)
            {
                _conversionToken.Cancel();
                Timer.StartTimer(_nextConversion - Core.Now, Convert, out _conversionToken);
                return;
            }

            if (!IsValidOwner(_owner) || !HasValue(_owner))
            {
                DamageEater.Clear(_owner);
                return;
            }

            if (_pending.Count == 0)
            {
                DamageEater.Clear(_owner);
                return;
            }

            var amount = _pending.Dequeue();
            _pendingHealing -= amount;

            var oldHits = _owner.Hits;
            _owner.Heal(amount, _owner, false);

            if (_owner.Hits > oldHits)
            {
                _owner.SendLocalizedMessage(EffectCliloc);
            }

            if (_pending.Count == 0)
            {
                DamageEater.Clear(_owner);
                return;
            }

            _nextConversion = Core.Now + ConversionDelay;
            Timer.StartTimer(ConversionDelay, Convert, out _conversionToken);
        }

        public void Stop()
        {
            _conversionToken.Cancel();
            _pending.Clear();
            _pendingHealing = 0;
        }

        private void QueueHealing(int portionDamage, AbsorptionAttribute specificAttribute)
        {
            if (portionDamage <= 0 || _pending.Count >= MaxCharges)
            {
                return;
            }

            var specific = AbsorptionAttributes.GetEaterValue(_owner, specificAttribute);
            var allTypes = AbsorptionAttributes.GetEaterValue(_owner, AbsorptionAttribute.DamageEater);
            var healing = GetHealing(portionDamage, specific, allTypes, specific >= allTypes);

            Enqueue(healing);
        }

        private void QueueDirectHealing(int portionDamage)
        {
            if (portionDamage <= 0 || _pending.Count >= MaxCharges)
            {
                return;
            }

            var allTypes = AbsorptionAttributes.GetEaterValue(_owner, AbsorptionAttribute.DamageEater);
            Enqueue(GetHealing(portionDamage, 0, allTypes, false));
        }

        private static int GetHealing(int portionDamage, int specific, int allTypes, bool useSpecific)
        {
            var rate = useSpecific ? specific : allTypes;
            var cap = useSpecific ? SpecificCap : AllTypesCap;

            if (rate <= 0)
            {
                return 0;
            }

            var healing = portionDamage * (double)rate / 100.0;
            var cappedHealing = portionDamage * (double)cap / 100.0;

            return (int)Math.Min(healing, cappedHealing);
        }

        private void Enqueue(int healing)
        {
            if (healing <= 0 || _pending.Count >= MaxCharges)
            {
                return;
            }

            _pending.Enqueue(healing);
            _pendingHealing += healing;
        }
    }
}
