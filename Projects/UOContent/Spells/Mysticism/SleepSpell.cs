using System;
using System.Collections.Generic;
using ModernUO.CodeGeneratedEvents;
using Server.Collections;
using Server.Engines.BuffIcons;
using Server.Mobiles;
using Server.Network;

namespace Server.Spells.Mysticism;

public static class SleepSpell
{
    internal const int CastSpeedMalus = 4;
    internal const int CastRecoveryMalus = 6;
    internal const int SwingSpeedMalus = 60;

    private static readonly Dictionary<Mobile, SleepContext> _effects = [];
    private static readonly Dictionary<Mobile, TimerExecutionToken> _immunities = [];

    public static void Configure()
    {
        EventSink.Logout += Clear;
        EventSink.MapChanged += OnMapChanged;
    }

    public static bool Apply(Mobile caster, Mobile target, TimeSpan duration)
    {
        if (caster == null || target is { Deleted: true } || !target.Alive || duration <= TimeSpan.Zero ||
            _effects.ContainsKey(target) || IsImmune(target))
        {
            return false;
        }

        var context = new SleepContext();
        _effects[target] = context;
        Timer.StartTimer(duration, () => EndEffect(target, false), out context.TimerToken);

        target.FixedParticles(0x373A, 1, 15, 0x48F, 7, 0, EffectLayer.Head, 0);
        target.PlaySound(0x1FB);

        if (target is PlayerMobile player)
        {
            player.NetState?.SendSpeedControl(SpeedControlSetting.Walk);
            player.AddBuff(new BuffInfo(BuffIcon.MassSleep, 1075824, duration));
        }

        return true;
    }

    public static bool IsUnderEffect(Mobile target) => target != null && _effects.ContainsKey(target);

    public static bool IsImmune(Mobile target) => target != null && _immunities.ContainsKey(target);

    public static void OnMobileDamaged(Mobile target, int damage)
    {
        if (damage > 0)
        {
            EndEffect(target, true);
        }
    }

    internal static int GetCastSpeedMalus(Mobile target) => IsUnderEffect(target) ? CastSpeedMalus : 0;

    internal static int GetCastRecoveryMalus(Mobile target) => IsUnderEffect(target) ? CastRecoveryMalus : 0;

    internal static int GetSwingSpeedMalus(Mobile target) => IsUnderEffect(target) ? SwingSpeedMalus : 0;

    internal static TimeSpan GetDuration(Mobile caster, Mobile target)
    {
        var supportSkill = Math.Max(caster.Skills.Focus.Value, caster.Skills.Imbuing.Value);
        var seconds = (caster.Skills.Mysticism.Value + supportSkill) / 20.0 + 3.0 - target.Skills.MagicResist.Value / 10.0;
        return TimeSpan.FromSeconds(Math.Max(seconds, 0.0));
    }

    internal static TimeSpan GetImmunityDuration(Mobile target) =>
        TimeSpan.FromSeconds(Math.Clamp((int)(target.Skills.MagicResist.Value / 10.0), 3, 12));

    internal static void EndEffectForTests(Mobile target, bool grantImmunity) => EndEffect(target, grantImmunity);

    internal static void ClearAllForTests()
    {
        using var targets = PooledRefList<Mobile>.Create();

        foreach (var target in _effects.Keys)
        {
            targets.Add(target);
        }

        for (var i = 0; i < targets.Count; i++)
        {
            EndEffect(targets[i], false);
        }

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
    public static void Clear(Mobile target)
    {
        EndEffect(target, false);

        if (_immunities.Remove(target, out var token))
        {
            token.Cancel();
        }
    }

    private static void OnMapChanged(Mobile target, Map oldMap) => Clear(target);

    private static void EndEffect(Mobile target, bool grantImmunity)
    {
        if (target == null || !_effects.Remove(target, out var context))
        {
            return;
        }

        context.TimerToken.Cancel();

        if (target is PlayerMobile player)
        {
            player.NetState?.SendSpeedControl(SpeedControlSetting.Disable);
            player.RemoveBuff(BuffIcon.MassSleep);
        }

        if (grantImmunity && target.Player && !target.Deleted && target.Alive)
        {
            StartImmunity(target);
        }
    }

    private static void StartImmunity(Mobile target)
    {
        if (_immunities.ContainsKey(target))
        {
            return;
        }

        Timer.StartTimer(GetImmunityDuration(target), () => RemoveImmunity(target), out var token);
        _immunities[target] = token;
    }

    private static void RemoveImmunity(Mobile target)
    {
        if (_immunities.Remove(target, out var token))
        {
            token.Cancel();
        }
    }

    private sealed class SleepContext
    {
        internal TimerExecutionToken TimerToken;
    }
}
