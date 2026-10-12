using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-proving.json</c> may say. Everything optional; empty keeps the code's numbers. PascalCase: a
/// key in lower case is silently a default (see BotDelveSettings).
/// </summary>
public sealed class BotProvingSettings
{
    /// <summary>Whether fights are held.</summary>
    public bool? Enabled { get; set; }

    /// <summary>Whether the delve reads proven strength.</summary>
    public bool? Judges { get; set; }

    public int? Rings { get; set; }

    public int? CapMs { get; set; }

    public int? StillMs { get; set; }

    public int? PauseMs { get; set; }

    public int? RetestMs { get; set; }

    public double? Pass { get; set; }

    public double? Reach { get; set; }

    public int? Keep { get; set; }

    /// <summary>Whether bots wake the ground round them as a client would (<see cref="BotWake"/>). Off unless said.</summary>
    public bool? Wake { get; set; }

    /// <summary>Only inside dungeons.</summary>
    public bool? WakeDeepsOnly { get; set; }
}

/// <summary>
/// The proving ground as a module: the numbers read, the field swept of whatever a stop left on it, the referee's beat
/// started, and one line every five minutes saying what the ground has measured.
///
/// <para>
/// <see cref="BotPhase.World"/>, because the rings are places on the map and the ladder is read off the dungeons' spawners;
/// it requires <c>Population</c>, whose bots are the ones copied, and <c>Delve</c>, whose dungeons are the ladder and whose
/// choice is the thing the readings change.
/// </para>
/// </summary>
public sealed class BotProvingModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotProvingModule));

    private const string ConfigPath = "Configuration/bot-proving.json";

    public override string Name => "Proving";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Delve"];

    /// <summary>How often the ground is summed up in the log.</summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static Timer _beat;

    private static Timer _say;

    public override void Start()
    {
        Load();

        BotProving.SweepAll();

        logger.Information(
            "The proving ground is {State} in Green Acres: {Rings} rings, a bot's double against the worst creature of each dungeon, weakest first, one rung up for every R of {Pass:F1} or better; a fight is called at {Cap}s or after {Still}s with nobody hurt, and a reading is tried again after {Retest} minutes or when the build moves. The delve {Judges}",
            BotProving.Running ? "open" : "closed",
            BotProving.Rings,
            BotProving.Pass,
            BotProving.CapMs / 1000,
            BotProving.StillMs / 1000,
            BotProving.RetestMs / 60000,
            BotProving.Judges ? "judges bands by proven strength" : "still judges bands by the old formula"
        );

        _beat?.Stop();
        _beat = new BeatTimer(TimeSpan.FromMilliseconds(Math.Max(50, BotProving.TickMs)));
        _beat.Start();

        _say?.Stop();
        _say = new SayTimer(TimeSpan.FromMilliseconds(SayEveryMs));
        _say.Start();

        // The rings are woken before every fight (BotProving.Wake); whether the bots' own ground is woken is the wake's.
        BotWake.Start();

        logger.Information("The wake {State}", BotWake.Describe());
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotProvingSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotProvingSettings());

            logger.Information("Wrote a starter proving file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotProving.Running = settings.Enabled ?? BotProving.Running;
        BotProving.Judges = settings.Judges ?? BotProving.Judges;
        BotProving.Rings = settings.Rings ?? BotProving.Rings;
        BotProving.CapMs = settings.CapMs ?? BotProving.CapMs;
        BotProving.StillMs = settings.StillMs ?? BotProving.StillMs;
        BotProving.PauseMs = settings.PauseMs ?? BotProving.PauseMs;
        BotProving.RetestMs = settings.RetestMs ?? BotProving.RetestMs;
        BotProving.Pass = settings.Pass ?? BotProving.Pass;
        BotProving.Reach = settings.Reach ?? BotProving.Reach;
        BotProving.Keep = settings.Keep ?? BotProving.Keep;
        BotWake.Running = settings.Wake ?? BotWake.Running;
        BotWake.DeepsOnly = settings.WakeDeepsOnly ?? BotWake.DeepsOnly;
    }

    private sealed class BeatTimer : Timer
    {
        public BeatTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => BotProving.Tick();
    }

    private sealed class SayTimer : Timer
    {
        public SayTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick()
        {
            logger.Information("Proving: {What}", BotProving.Describe());

            if (BotWake.Running || BotWake.PutBack > 0)
            {
                logger.Information("Wake: {What}", BotWake.Describe());
            }
        }
    }

    public override void Reset()
    {
        logger.Information("Proving, before the reload: {State}", BotProving.Describe());

        _beat?.Stop();
        _beat = null;

        _say?.Stop();
        _say = null;

        BotWake.Stop();
        BotProving.Rewind();
    }
}
