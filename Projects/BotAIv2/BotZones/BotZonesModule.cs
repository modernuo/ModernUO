using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-zones.json</c> may say. Everything optional; an empty file keeps the code's numbers. PascalCase:
/// a key in lower case is silently a default, and the boot line is the proof of what took.
/// </summary>
public sealed class BotZonesSettings
{
    /// <summary>Whether the zones are kept at all.</summary>
    public bool? Enabled { get; set; }

    public int? TickMs { get; set; }

    public int? RefreshMs { get; set; }

    public int? JsonEveryMs { get; set; }

    /// <summary>The sweep: loop milliseconds a beat, sectors to a window's side, windows at most a beat.</summary>
    public double? ScanBudgetMs { get; set; }

    public int? ScanWindowSectors { get; set; }

    public int? ScanMostWindows { get; set; }

    /// <summary>The model. See the properties of the same names on <see cref="BotZones"/>.</summary>
    public int? DeclaredMost { get; set; }

    public int? LiveRadius { get; set; }

    public int? LooseRadius { get; set; }

    public int? PassiveRadius { get; set; }

    public int? WaterRadius { get; set; }

    public int? WaterGroup { get; set; }

    public int? ObservedMargin { get; set; }

    public double? ObservedLeast { get; set; }

    public double? ObservedWins { get; set; }

    public int? ConfidentSeconds { get; set; }

    public int? ConfidentSamples { get; set; }

    public int? HalfLifeMinutes { get; set; }

    public int? PerceptionMost { get; set; }

    public int? WaterMeleeReach { get; set; }

    public int? WaterCasterReach { get; set; }

    public int? RootedReach { get; set; }

    public int? ConvergeTiles { get; set; }

    /// <summary>How local a zone is: see <see cref="BotZones.JoinGap"/> and the three beside it.</summary>
    public int? JoinGap { get; set; }

    public int? ZoneMostTiles { get; set; }

    public int? StrayBeyond { get; set; }

    public int? ObservedSlack { get; set; }

    public bool? TownsClear { get; set; }

    /// <summary>A camp's patrol grown on what was seen, a caster's spell, wanderers' trails, events (08.10.2026). See <see cref="BotZones.ReachBeyond"/>.</summary>
    public int? ReachBeyond { get; set; }

    public double? ReachPercentile { get; set; }

    public int? ReachLeast { get; set; }

    public int? ReachHalfLifeMinutes { get; set; }

    public int? SpellReach { get; set; }

    public int? WanderMinutes { get; set; }

    public int? WanderMostCells { get; set; }

    public bool? Events { get; set; }

    public double? DeadlyVsBot { get; set; }

    public double? EmptyWeight { get; set; }

    public int? DeathsPromote { get; set; }

    public int? DeathsWindowHours { get; set; }

    public int? ClearLeastLive { get; set; }

    public int? MostPoints { get; set; }

    public int? ClearMostPoints { get; set; }

    public int? SeaMostPoints { get; set; }

    public int? MostRings { get; set; }

    public int? MostKinds { get; set; }

    public double? SimplifyTiles { get; set; }

    public int? SmoothPasses { get; set; }

    /// <summary>Whether the build may run off the loop, and the cores the machine needs for it to.</summary>
    public bool? OffLoop { get; set; }

    public int? OffLoopCores { get; set; }

    public int? InlineRefreshMs { get; set; }

    /// <summary>The tour. See <see cref="BotZoneTour"/>.</summary>
    public bool? Tour { get; set; }

    public int? TourHoldMs { get; set; }

    public int? TourGraceMs { get; set; }

    public int? TourReach { get; set; }

    public int? TourMostSectors { get; set; }

    public int? TourMostWalkers { get; set; }

    public int? TourFocusMs { get; set; }

    public bool? TourWater { get; set; }

    /// <summary>The zone watch (07.10.2026): its keeper and its counter. See <see cref="BotZoneWatch"/>.</summary>
    public string TourKeeper { get; set; }

    public int? AccuracyWindowHours { get; set; }

    public int? AccuracyMinDeaths { get; set; }

    public int? AccuracyMinZones { get; set; }

    public int? AlarmWindowHours { get; set; }

    public int? AlarmMinDeaths { get; set; }

    public double? AccuracyTarget { get; set; }

    public int? AccuracyHoldMinutes { get; set; }

    public double? ResumeBelow { get; set; }

    public int? ResumeAfterMinutes { get; set; }

    /// <summary>Whether what was learned is kept across a restart, and how often it is written.</summary>
    public bool? Memory { get; set; }

    public int? MemorySaveMs { get; set; }
}

/// <summary>
/// The danger and safety zones as a module: the numbers read, the beat started, and one <c>Zones:</c> line every five minutes
/// with every counter the system keeps.
///
/// <para>
/// <see cref="BotPhase.World"/>, after <c>Population</c>: the zones are drawn on the population's home map, and the yardstick
/// of a zone's strength is the population's median bot.
/// </para>
/// </summary>
public sealed class BotZonesModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotZonesModule));

    private const string ConfigPath = "Configuration/bot-zones.json";

    public override string Name => "Zones";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population"];

    /// <summary>How often the zones are summed up in the log.</summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static Timer _say;

    public override void Start()
    {
        Load();

        if (!BotZones.Running)
        {
            logger.Information("Zones: switched off in {Path}; every query answers clear", ConfigPath);

            return;
        }

        BotZones.Start(BotPopulation.Home ?? Map.Felucca);

        logger.Information(
            "Zones: kept on {Map} in cells of {Cell} tiles. Declared discs up to {Declared} tiles until a haunt's confidence reaches {Wins:F2} ({Seconds}s awake, {Samples} sightings); a zone is a camp of spawners whose discs touch within {Gap} tiles, at most {Most} tiles across, its wanderers beyond range+{Stray} drawn where they stand and along their last {Wander} minutes (at most {WanderCells} cells), a patrol grown to where {Percentile:P0} of its sightings fall (at most {Beyond} tiles past the declaration, from {Least} sightings, halved every {ReachHalf} minutes), casters reaching {Spell} tiles past their sight, events {Events}, guarded ground {Towns}; deadly at ×{Deadly:F1} the median bot; {Promote} deaths in {Hours}h make a hostile zone deadly. The sweep spends {Budget:F1}ms a beat every {Tick}ms; a build every {Refresh}s, {Where}. The tour is {Tour}: {Walkers} walkers, {Hold}s a zone; zone keeper {Keeper}, accuracy over {Window}h judged from {MinDeaths} deaths and {MinZones} zones apart, the watch ends when both are at {Target:P0} for {HoldMin} min, an alarm when the deaths over {AlarmHours}h (at least {AlarmMin}) stay under {Resume:P0} for {ResumeMin} min",
            BotZones.Map,
            BotZoneState.Cell,
            BotZones.DeclaredMost,
            BotZones.ObservedWins,
            BotZones.ConfidentSeconds,
            BotZones.ConfidentSamples,
            BotZones.JoinGap,
            BotZones.ZoneMostTiles,
            BotZones.StrayBeyond,
            BotZones.WanderMinutes,
            BotZones.WanderMostCells,
            BotZones.ReachPercentile,
            BotZones.ReachBeyond,
            BotZones.ReachLeast,
            BotZones.ReachHalfLifeMinutes,
            BotZones.SpellReach,
            BotZones.Events ? "painted while they live" : "off",
            BotZones.TownsClear ? "kept clear" : "painted like any",
            BotZones.DeadlyVsBot,
            BotZones.DeathsPromote,
            BotZones.DeathsWindowHours,
            BotZoneScan.BudgetMs,
            BotZones.TickMs,
            BotZones.RefreshMs / 1000,
            BotZones.OffLoopNow
                ? $"off the loop ({Environment.ProcessorCount} cores)"
                : $"on the loop every {BotZones.InlineRefreshMs / 1000}s at most ({Environment.ProcessorCount} cores, {BotZones.OffLoopCores} wanted to go off it)",
            BotZoneTour.Running ? "on" : "off",
            BotZoneTour.MostWalkers,
            BotZoneTour.HoldMs / 1000,
            BotZoneTour.Keeper,
            BotZoneWatch.AccuracyWindowHours,
            BotZoneWatch.AccuracyMinDeaths,
            BotZoneWatch.AccuracyMinZones,
            BotZoneWatch.AccuracyTarget,
            BotZoneWatch.AccuracyHoldMinutes,
            BotZoneWatch.AlarmWindowHours,
            BotZoneWatch.AlarmMinDeaths,
            BotZoneWatch.ResumeBelow,
            BotZoneWatch.ResumeAfterMinutes
        );

        _say?.Stop();
        _say = new SayTimer(TimeSpan.FromMilliseconds(SayEveryMs));
        _say.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotZonesSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotZonesSettings());

            logger.Information("Wrote a starter zones file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotZones.Running = settings.Enabled ?? BotZones.Running;
        BotZones.TickMs = settings.TickMs ?? BotZones.TickMs;
        BotZones.RefreshMs = settings.RefreshMs ?? BotZones.RefreshMs;
        BotZones.JsonEveryMs = settings.JsonEveryMs ?? BotZones.JsonEveryMs;
        BotZoneScan.BudgetMs = settings.ScanBudgetMs ?? BotZoneScan.BudgetMs;
        BotZoneScan.WindowSectors = settings.ScanWindowSectors ?? BotZoneScan.WindowSectors;
        BotZoneScan.MostWindows = settings.ScanMostWindows ?? BotZoneScan.MostWindows;
        BotZones.DeclaredMost = settings.DeclaredMost ?? BotZones.DeclaredMost;
        BotZones.SeaMostPoints = settings.SeaMostPoints ?? BotZones.SeaMostPoints;
        BotZones.LiveRadius = settings.LiveRadius ?? BotZones.LiveRadius;
        BotZones.LooseRadius = settings.LooseRadius ?? BotZones.LooseRadius;
        BotZones.PassiveRadius = settings.PassiveRadius ?? BotZones.PassiveRadius;
        BotZones.WaterRadius = settings.WaterRadius ?? BotZones.WaterRadius;
        BotZones.WaterGroup = settings.WaterGroup ?? BotZones.WaterGroup;
        BotZones.ObservedMargin = settings.ObservedMargin ?? BotZones.ObservedMargin;
        BotZones.ObservedLeast = settings.ObservedLeast ?? BotZones.ObservedLeast;
        BotZones.ObservedWins = settings.ObservedWins ?? BotZones.ObservedWins;
        BotZones.ConfidentSeconds = settings.ConfidentSeconds ?? BotZones.ConfidentSeconds;
        BotZones.ConfidentSamples = settings.ConfidentSamples ?? BotZones.ConfidentSamples;
        BotZoneRaster.HalfLifeMinutes = settings.HalfLifeMinutes ?? BotZoneRaster.HalfLifeMinutes;
        BotZones.PerceptionMost = settings.PerceptionMost ?? BotZones.PerceptionMost;
        BotZones.WaterMeleeReach = settings.WaterMeleeReach ?? BotZones.WaterMeleeReach;
        BotZones.WaterCasterReach = settings.WaterCasterReach ?? BotZones.WaterCasterReach;
        BotZones.RootedReach = settings.RootedReach ?? BotZones.RootedReach;
        BotZones.ConvergeTiles = settings.ConvergeTiles ?? BotZones.ConvergeTiles;
        BotZones.JoinGap = settings.JoinGap ?? BotZones.JoinGap;
        BotZones.ZoneMostTiles = settings.ZoneMostTiles ?? BotZones.ZoneMostTiles;
        BotZones.StrayBeyond = settings.StrayBeyond ?? BotZones.StrayBeyond;
        BotZones.ObservedSlack = settings.ObservedSlack ?? BotZones.ObservedSlack;
        BotZones.TownsClear = settings.TownsClear ?? BotZones.TownsClear;
        BotZones.ReachBeyond = settings.ReachBeyond ?? BotZones.ReachBeyond;
        BotZones.ReachPercentile = settings.ReachPercentile ?? BotZones.ReachPercentile;
        BotZones.ReachLeast = settings.ReachLeast ?? BotZones.ReachLeast;
        BotZones.ReachHalfLifeMinutes = settings.ReachHalfLifeMinutes ?? BotZones.ReachHalfLifeMinutes;

        if (settings.SpellReach is { } spellReach)
        {
            BotZones.SpellReach = spellReach;
        }

        BotZones.WanderMinutes = settings.WanderMinutes ?? BotZones.WanderMinutes;
        BotZones.WanderMostCells = settings.WanderMostCells ?? BotZones.WanderMostCells;
        BotZones.Events = settings.Events ?? BotZones.Events;
        BotZones.DeadlyVsBot = settings.DeadlyVsBot ?? BotZones.DeadlyVsBot;
        BotZones.EmptyWeight = settings.EmptyWeight ?? BotZones.EmptyWeight;
        BotZones.DeathsPromote = settings.DeathsPromote ?? BotZones.DeathsPromote;
        BotZones.DeathsWindowHours = settings.DeathsWindowHours ?? BotZones.DeathsWindowHours;
        BotZones.ClearLeastLive = settings.ClearLeastLive ?? BotZones.ClearLeastLive;
        BotZones.MostPoints = settings.MostPoints ?? BotZones.MostPoints;
        BotZones.ClearMostPoints = settings.ClearMostPoints ?? BotZones.ClearMostPoints;
        BotZones.MostRings = settings.MostRings ?? BotZones.MostRings;
        BotZones.MostKinds = settings.MostKinds ?? BotZones.MostKinds;
        BotZones.SimplifyTiles = settings.SimplifyTiles ?? BotZones.SimplifyTiles;
        BotZones.SmoothPasses = settings.SmoothPasses ?? BotZones.SmoothPasses;
        BotZones.OffLoop = settings.OffLoop ?? BotZones.OffLoop;
        BotZones.OffLoopCores = settings.OffLoopCores ?? BotZones.OffLoopCores;
        BotZones.InlineRefreshMs = settings.InlineRefreshMs ?? BotZones.InlineRefreshMs;
        BotZoneTour.Running = settings.Tour ?? BotZoneTour.Running;
        BotZoneTour.HoldMs = settings.TourHoldMs ?? BotZoneTour.HoldMs;
        BotZoneTour.GraceMs = settings.TourGraceMs ?? BotZoneTour.GraceMs;
        BotZoneTour.Reach = settings.TourReach ?? BotZoneTour.Reach;
        BotZoneTour.MostSectors = settings.TourMostSectors ?? BotZoneTour.MostSectors;
        BotZoneTour.MostWalkers = settings.TourMostWalkers ?? BotZoneTour.MostWalkers;
        BotZoneTour.FocusMs = settings.TourFocusMs ?? BotZoneTour.FocusMs;
        BotZoneTour.Water = settings.TourWater ?? BotZoneTour.Water;
        BotZoneTour.Keeper = settings.TourKeeper ?? BotZoneTour.Keeper;
        BotZoneWatch.AccuracyWindowHours = settings.AccuracyWindowHours ?? BotZoneWatch.AccuracyWindowHours;
        BotZoneWatch.AccuracyMinDeaths = settings.AccuracyMinDeaths ?? BotZoneWatch.AccuracyMinDeaths;
        BotZoneWatch.AccuracyMinZones = settings.AccuracyMinZones ?? BotZoneWatch.AccuracyMinZones;
        BotZoneWatch.AlarmWindowHours = settings.AlarmWindowHours ?? BotZoneWatch.AlarmWindowHours;
        BotZoneWatch.AlarmMinDeaths = settings.AlarmMinDeaths ?? BotZoneWatch.AlarmMinDeaths;
        BotZoneWatch.AccuracyTarget = settings.AccuracyTarget ?? BotZoneWatch.AccuracyTarget;
        BotZoneWatch.AccuracyHoldMinutes = settings.AccuracyHoldMinutes ?? BotZoneWatch.AccuracyHoldMinutes;
        BotZoneWatch.ResumeBelow = settings.ResumeBelow ?? BotZoneWatch.ResumeBelow;
        BotZoneWatch.ResumeAfterMinutes = settings.ResumeAfterMinutes ?? BotZoneWatch.ResumeAfterMinutes;
        BotZoneMemory.Running = settings.Memory ?? BotZoneMemory.Running;
        BotZoneMemory.SaveEveryMs = settings.MemorySaveMs ?? BotZoneMemory.SaveEveryMs;

        // A hold under a minute sees too little of a patrol to be worth the walk: Patrick's order says a minute at least.
        BotZoneTour.HoldMs = Math.Max(60000, BotZoneTour.HoldMs);
    }

    private sealed class SayTimer : Timer
    {
        public SayTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => logger.Information("Zones: {What}", BotZones.Describe());
    }

    public override void Reset()
    {
        logger.Information("Zones, before the reload: {What}", BotZones.Describe());

        _say?.Stop();
        _say = null;

        BotZones.Forget();
    }
}
