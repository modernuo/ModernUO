using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-reach.json</c> may say. Everything optional; an empty file keeps the code's numbers. PascalCase:
/// a key in lower case is silently a default, and the boot line is the proof of what took.
/// </summary>
public sealed class BotReachSettings
{
    /// <summary>Whether the reach map is drawn and asked at all.</summary>
    public bool? Enabled { get; set; }

    /// <summary>The loop's milliseconds a tick of drawing may take, and how often a tick runs.</summary>
    public double? SliceMs { get; set; }

    public int? EveryMs { get; set; }

    /// <summary>Whether the clusters are kept on disk between boots.</summary>
    public bool? Persist { get; set; }

    /// <summary>Cores the machine needs for the solve to run off the loop.</summary>
    public int? OffLoopCores { get; set; }

    /// <summary>How near a sampled place open ground must be.</summary>
    public int? SampleReach { get; set; }

    /// <summary>Steps onto ground the map calls unreachable, in one cluster, before it is drawn again; and how many are said.</summary>
    public int? RedrawAfter { get; set; }

    public int? SayContradictions { get; set; }

    /// <summary>The check: whether it runs at the first solve, how many tiles, its budget, clock, flood and whether a debugger stands on each.</summary>
    public bool? CheckOnBoot { get; set; }

    public int? CheckSamples { get; set; }

    public double? CheckBudgetMs { get; set; }

    public double? CheckCeilingMs { get; set; }

    public int? CheckFloodCells { get; set; }

    public bool? CheckCarry { get; set; }
}

/// <summary>
/// The reach map as a module: the numbers read, the map started on the population's home map, and one <c>Reach:</c> line every
/// five minutes with every counter it keeps.
/// </summary>
public sealed class BotReachModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotReachModule));

    private const string ConfigPath = "Configuration/bot-reach.json";

    public override string Name => "Reach";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Movement"];

    /// <summary>How often the map is summed up in the log.</summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static Timer _say;

    public override void Start()
    {
        Load();

        if (!BotReachMap.Running)
        {
            logger.Information("Reach: switched off in {Path}; every question answers that the map cannot say", ConfigPath);

            return;
        }

        BotReachMap.Start(BotPopulation.Home ?? Map.Felucca);

        logger.Information(
            "Reach: samplers need open ground within {Sample} tiles; a cluster is drawn again after {Redraw} steps onto ground it calls unreachable; the check {Check}",
            BotReachMap.SampleReach,
            BotReachMap.RedrawAfter,
            BotReachCheck.OnBoot
                ? $"examines {BotReachCheck.Samples} border tiles once the first map is solved, {BotReachCheck.BudgetMs:F0}ms a beat, a debugger standing on each: {BotReachCheck.Carry}"
                : "waits for the door"
        );

        _say?.Stop();
        _say = new SayTimer(TimeSpan.FromMilliseconds(SayEveryMs));
        _say.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotReachSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotReachSettings());

            logger.Information("Wrote a starter reach file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotReachMap.Running = settings.Enabled ?? BotReachMap.Running;
        BotReachMap.SliceMs = settings.SliceMs ?? BotReachMap.SliceMs;
        BotReachMap.EveryMs = settings.EveryMs ?? BotReachMap.EveryMs;
        BotReachMap.Persist = settings.Persist ?? BotReachMap.Persist;
        BotReachMap.OffLoopCores = settings.OffLoopCores ?? BotReachMap.OffLoopCores;
        BotReachMap.SampleReach = settings.SampleReach ?? BotReachMap.SampleReach;
        BotReachMap.RedrawAfter = settings.RedrawAfter ?? BotReachMap.RedrawAfter;
        BotReachMap.SayContradictions = settings.SayContradictions ?? BotReachMap.SayContradictions;
        BotReachCheck.OnBoot = settings.CheckOnBoot ?? BotReachCheck.OnBoot;
        BotReachCheck.Samples = settings.CheckSamples ?? BotReachCheck.Samples;
        BotReachCheck.BudgetMs = settings.CheckBudgetMs ?? BotReachCheck.BudgetMs;
        BotReachCheck.CeilingMs = settings.CheckCeilingMs ?? BotReachCheck.CeilingMs;
        BotReachCheck.FloodCells = settings.CheckFloodCells ?? BotReachCheck.FloodCells;
        BotReachCheck.Carry = settings.CheckCarry ?? BotReachCheck.Carry;
    }

    private sealed class SayTimer : Timer
    {
        public SayTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => logger.Information("Reach: {What}; {Check}", BotReachMap.Describe(), BotReachCheck.Describe());
    }

    public override void Reset()
    {
        logger.Information("Reach, before the reload: {What}", BotReachMap.Describe());

        _say?.Stop();
        _say = null;

        BotReachMap.Forget();
    }
}
