using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-alarm.json</c> may say. Everything optional; an empty file keeps the code's
/// numbers.
///
/// <para>
/// <b>PascalCase, and it is not a style question.</b> The deserialiser matches these names as written, so a
/// key in lower case is not an error and not a warning — it is a value silently left at its default. The
/// only proof of a live threshold is the line this module writes at boot, or asking the debugger's door.
/// </para>
/// </summary>
public sealed class BotAlarmSettings
{
    /// <summary>How often the rules are read, in milliseconds.</summary>
    public int? TickMs { get; set; }

    /// <summary>Errors in one look before the channel is told.</summary>
    public int? ErrorsAt { get; set; }

    /// <summary>The window the work rules are judged over.</summary>
    public int? WorkMs { get; set; }

    /// <summary>Fewest pieces of work taken on in that window before finishing them is judged.</summary>
    public int? WorkLeast { get; set; }

    /// <summary>The share of work taken on that must finish.</summary>
    public double? WorkFloor { get; set; }

    /// <summary>The share of the population standing still that is worth an alarm.</summary>
    public double? StandShare { get; set; }

    /// <summary>Fewest bots alive before that share means anything.</summary>
    public int? StandLeast { get; set; }

    /// <summary>How long a market with stalls and wants may move nothing.</summary>
    public int? MarketMs { get; set; }

    /// <summary>Deaths in one work window that count as a plague.</summary>
    public int? DeathsAt { get; set; }

    /// <summary>How often the channel says that nothing is wrong.</summary>
    public int? AliveMs { get; set; }

    /// <summary>How long a standing alarm waits before saying so again.</summary>
    public int? RestMs { get; set; }
}

/// <summary>Reads the alarm file and moves the numbers it names.</summary>
public static class BotAlarmConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotAlarmConfig));

    private const string ConfigPath = "Configuration/bot-alarm.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotAlarmSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotAlarmSettings());

            logger.Information("Wrote a starter alarm file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotSigns.TickMs = settings.TickMs ?? BotSigns.TickMs;
        BotSigns.ErrorsAt = settings.ErrorsAt ?? BotSigns.ErrorsAt;
        BotSigns.WorkMs = settings.WorkMs ?? BotSigns.WorkMs;
        BotSigns.WorkLeast = settings.WorkLeast ?? BotSigns.WorkLeast;
        BotSigns.WorkFloor = settings.WorkFloor ?? BotSigns.WorkFloor;
        BotSigns.StandShare = settings.StandShare ?? BotSigns.StandShare;
        BotSigns.StandLeast = settings.StandLeast ?? BotSigns.StandLeast;
        BotSigns.MarketMs = settings.MarketMs ?? BotSigns.MarketMs;
        BotSigns.DeathsAt = settings.DeathsAt ?? BotSigns.DeathsAt;
        BotSigns.AliveMs = settings.AliveMs ?? BotSigns.AliveMs;

        BotAlarm.RestMs = settings.RestMs ?? BotAlarm.RestMs;
    }
}

/// <summary>
/// The alarm channel as a module: it reads the shard once a minute and writes an event when something it
/// can name has gone wrong.
///
/// <para>
/// <see cref="BotPhase.World"/> and after <c>Population</c>, because every rule divides by something the
/// population keeps. It offers nothing into the auction and answers for no bot's body — like the debugger,
/// it only reads, which is what lets it be switched off with no effect on anything.
/// </para>
///
/// <para>
/// <b>Separate from the debugger on purpose.</b> The debugger is an observer with a model, a body and an
/// opinion; this is a smoke alarm. The one is worth switching off while working on the other, and a shard
/// running with no watcher at all should still be able to shout when its work stops finishing.
/// </para>
/// </summary>
public sealed class BotAlarmModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotAlarmModule));

    private static Timer _timer;

    public override string Name => "Alarm";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population"];

    private sealed class SignsTimer : Timer
    {
        public SignsTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => BotSigns.Look();
    }

    public override void Start()
    {
        BotAlarmConfig.Load();

        BotTail.Open();
        BotAlarm.Open();
        BotSigns.Start();

        // Did the world come back? Asked once, here, before any rule that needs a window. See BotBoot.
        BotBoot.Check();

        _timer?.Stop();
        _timer = new SignsTimer(TimeSpan.FromMilliseconds(BotSigns.TickMs));
        _timer.Start();

        // The thresholds it is actually running with, said once. A belief about when this will shout,
        // formed from the defaults in the source, can be wrong by a factor of two with nothing looking odd.
        logger.Information("The alarm: {Channel}. {Rules}", BotAlarm.Describe(), BotSigns.Describe());
    }

    /// <summary>What it has done, for whoever is reading the session log.</summary>
    public static string Summarise() => $"{BotAlarm.Describe()}; {BotTail.Describe()}; {BotBoot.Describe()}";

    public override void Reset()
    {
        logger.Information("The alarm, before the reload: {State}", Summarise());

        _timer?.Stop();
        _timer = null;

        BotAlarm.Forget();
        BotTail.Forget();
        BotSigns.Forget();
    }
}
