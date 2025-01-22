using System;
using ModernUO.CodeGeneratedEvents;
using Server.Mobiles;

namespace Server.Engines.BuffIcons;

public class BuffInfo
{
    private TimerExecutionToken _timerToken;

    public BuffInfo(
        BuffIcon iconID, int titleCliloc, TimeSpan duration = default, TextDefinition args = null,
        bool retainThroughDeath = false
    ) : this(iconID, titleCliloc, titleCliloc + 1, duration, args, retainThroughDeath)
    {
    }

    public BuffInfo(
        BuffIcon iconID, int titleCliloc, int secondaryCliloc, TimeSpan duration = default, TextDefinition args = null,
        bool retainThroughDeath = false
    )
    {
        ID = iconID;
        TitleCliloc = titleCliloc;
        SecondaryCliloc = secondaryCliloc;
        Duration = duration;
        Args = args;
        RetainThroughDeath = retainThroughDeath;
    }

    public static bool Enabled { get; private set; }

    public BuffIcon ID { get; }

    public int TitleCliloc { get; }

    public int SecondaryCliloc { get; }

    public DateTime StartTime { get; private set; }

    public TimeSpan Duration { get; }

    public bool RetainThroughDeath { get; }

    public TextDefinition Args { get; }

    public static void Configure()
    {
        Enabled = ServerConfiguration.GetOrUpdateSetting("buffIcons.enable", Core.ML);
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm)
    {
        if (!Enabled)
        {
            return;
        }

        pm.ResendBuffs();
    }

    public void StartTimer(Mobile m)
    {
        if (Duration != TimeSpan.Zero)
        {
            StartTime = Core.Now;
            Timer.StartTimer(Duration, () => RemoveBuff(m, this), out _timerToken);
        }
    }

    public static void AddBuff(Mobile m, BuffInfo b)
    {
        (m as PlayerMobile)?.AddBuff(b);
    }

    public static void RemoveBuff(Mobile m, BuffInfo b)
    {
        if (b == null)
        {
            return;
        }

        b._timerToken.Cancel();
        (m as PlayerMobile)?.RemoveBuff(b.ID);
    }

    public static void RemoveBuff(Mobile m, BuffIcon b)
    {
        (m as PlayerMobile)?.RemoveBuff(b);
    }
}
