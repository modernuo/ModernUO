using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using ModernUO.Serialization;
using Server.Collections;
using Server.Items;
using Server.Mobiles;
using Server.Misc;

namespace Server.Spells.Spellweaving;

public class WildfireSpell : ArcanistSpell, ITargetingSpell<IPoint3D>
{
    private const int BaseRadius = 5;
    private const int MaxBaseDamage = 15;
    private const int TargetCooldownMilliseconds = 1000;

    private static readonly SpellInfo _info = new("Wildfire", "Haelyn", -1);
    private static readonly Dictionary<Mobile, long> _targetCooldowns = new();
    private WildfireTimer _timer;

    public WildfireSpell(Mobile caster, Item scroll = null) : base(caster, scroll, _info)
    {
    }

    public override TimeSpan CastDelayBase => TimeSpan.FromSeconds(2.5);

    public int TargetRange => Core.T2A ? 10 : 12;

    public override double RequiredSkill => 66.0;

    public override int RequiredMana => 50;

    public override void OnCast()
    {
        Caster.Target = new SpellTarget<IPoint3D>(this, allowGround: true);
    }

    public void Target(IPoint3D p)
    {
        if (!TryGetTargetLocation(p, out var location, out var map))
        {
            return;
        }

        if (!CheckSequence())
        {
            return;
        }

        SpellHelper.Turn(Caster, location);

        var focusLevel = FocusLevel;
        var radius = GetRadius(focusLevel);
        var duration = GetDuration(Caster.Skills.Spellweaving.Value, focusLevel);
        var damage = GetBaseDamage(Caster.Skills.Spellweaving.Value, focusLevel);

        PlaceFieldVisuals(location, map, radius, duration);
        Effects.PlaySound(location, map, 0x5CF);
        _timer = new WildfireTimer(this, location, map, radius, damage, duration);
        _timer.Start();
    }

    internal static int GetBaseDamage(double spellweaving, int focusLevel) =>
        Math.Min(MaxBaseDamage, 10 + (int)(spellweaving / 24) + focusLevel);

    internal static TimeSpan GetDuration(double spellweaving, int focusLevel) =>
        TimeSpan.FromSeconds(Math.Min(5, Math.Max(1, (int)(spellweaving / 24))) + focusLevel);

    internal static int GetRadius(int focusLevel) => BaseRadius + focusLevel;

    internal static int GetDamageForTargetCount(int baseDamage, int targetCount) =>
        targetCount switch
        {
            <= 1 => baseDamage,
            2 => Math.Min(10, baseDamage / 2),
            _ => Math.Max(5, baseDamage / 3)
        };

    internal static int GetDamageAfterSdi(int damage, int spellDamageIncrease, bool playerVsPlayer)
    {
        if (playerVsPlayer)
        {
            spellDamageIncrease = Math.Min(spellDamageIncrease, 15);
        }

        return damage * (100 + spellDamageIncrease) / 100;
    }

    internal static bool IsValidTarget(Mobile caster, Mobile target) =>
        target is { Deleted: false, Alive: true, Hidden: false } &&
        caster != target &&
        caster.Map == target.Map &&
        caster.CanSee(target) &&
        caster.InLOS(target) &&
        SpellHelper.ValidIndirectTarget(caster, target) &&
        caster.CanBeHarmful(target, false);

    internal static void ClearTargetCooldown(Mobile target)
    {
        if (target != null)
        {
            _targetCooldowns.Remove(target);
        }
    }

    internal static void ClearAllForTests()
    {
        _targetCooldowns.Clear();
    }

    internal void StartTimerForTests(Point3D location, Map map, int radius, int damage, TimeSpan duration)
    {
        _timer = new WildfireTimer(this, location, map, radius, damage, duration);
    }

    internal void TickForTests() => _timer?.TickForTests();

    private bool TryGetTargetLocation(IPoint3D target, out Point3D location, out Map map)
    {
        location = default;
        map = Caster.Map;

        if (target == null || map == null || map == Map.Internal)
        {
            return false;
        }

        var targetPoint = target;
        SpellHelper.GetSurfaceTop(ref targetPoint);
        location = new Point3D(targetPoint);

        if (!Caster.InRange(location, TargetRange) || !Caster.CanSee(location) || !Caster.InLOS(location))
        {
            Caster.SendLocalizedMessage(500237); // Target can not be seen.
            return false;
        }

        if (!SpellHelper.CheckTown(location, Caster) || !SpellHelper.AdjustField(ref location, map, 12, false))
        {
            return false;
        }

        return true;
    }

    private static void PlaceFieldVisuals(Point3D location, Map map, int radius, TimeSpan duration)
    {
        for (var x = -1; x <= 1; x++)
        {
            for (var y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                var visualLocation = new Point3D(location.X + x * radius, location.Y + y * radius, location.Z);

                if (SpellHelper.AdjustField(ref visualLocation, map, 12, false))
                {
                    new WildfireFireItem(visualLocation, map, duration);
                }
            }
        }
    }

    private static void DefragTargetCooldowns()
    {
        using var expired = PooledRefList<Mobile>.Create();

        foreach (var (target, expiresAt) in _targetCooldowns)
        {
            if (target.Deleted || Core.TickCount >= expiresAt)
            {
                expired.Add(target);
            }
        }

        for (var i = 0; i < expired.Count; i++)
        {
            _targetCooldowns.Remove(expired[i]);
        }
    }

    [OnEvent(nameof(PlayerMobile.PlayerDeathEvent))]
    [OnEvent(nameof(PlayerMobile.PlayerDeletedEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeathEvent))]
    [OnEvent(nameof(BaseCreature.CreatureDeletedEvent))]
    public static void OnTargetRemoved(Mobile target) => ClearTargetCooldown(target);

    private sealed class WildfireTimer : Timer
    {
        private readonly WildfireSpell _spell;
        private readonly Mobile _caster;
        private readonly Point3D _location;
        private readonly Map _map;
        private readonly int _radius;
        private readonly int _baseDamage;

        public WildfireTimer(
            WildfireSpell spell,
            Point3D location,
            Map map,
            int radius,
            int baseDamage,
            TimeSpan duration
        ) : base(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), (int)duration.TotalSeconds)
        {
            _spell = spell;
            _caster = spell.Caster;
            _location = location;
            _map = map;
            _radius = radius;
            _baseDamage = baseDamage;
        }

        protected override void OnTick()
        {
            if (_caster.Deleted || _map == null || _map == Map.Internal)
            {
                Stop();
                return;
            }

            DefragTargetCooldowns();

            using var targets = PooledRefQueue<Mobile>.Create();

            foreach (var target in _map.GetMobilesInRange(_location, _radius))
            {
                if (!_targetCooldowns.ContainsKey(target) && IsValidTarget(_caster, target))
                {
                    targets.Enqueue(target);
                }
            }

            var targetCount = targets.Count;

            while (targets.Count > 0)
            {
                var target = targets.Dequeue();
                _caster.DoHarmful(target);

                var damage = GetDamageForTargetCount(_baseDamage, targetCount);
                var playerVsPlayer = _caster.Player && target.Player;
                damage = GetDamageAfterSdi(
                    damage,
                    AosAttributes.GetValue(_caster, AosAttribute.SpellDamage),
                    playerVsPlayer
                );

                SpellHelper.Damage(_spell, target, damage, 0, 100, 0, 0, 0);
                new WildfireFireItem(target.Location, _map, TimeSpan.FromSeconds(1));
                _targetCooldowns[target] = Core.TickCount + TargetCooldownMilliseconds;
            }
        }

        internal void TickForTests() => OnTick();
    }
}

[DispellableField]
[SerializationGenerator(0, false)]
public partial class WildfireFireItem : Item
{
    private Timer _timer;

    public WildfireFireItem(Point3D location, Map map, TimeSpan duration)
        : base(Utility.RandomBool() ? 0x398C : 0x3996)
    {
        Movable = false;
        MoveToWorld(location, map);
        _timer = Timer.DelayCall(duration, Delete);
    }

    public override void OnAfterDelete()
    {
        _timer?.Stop();
        _timer = null;
        base.OnAfterDelete();
    }

    [AfterDeserialization]
    private void AfterDeserialization()
    {
        Delete();
    }
}
