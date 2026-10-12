using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The relics as a module (07.10.2026, relics20): registers itself from its own <c>Configure</c> (no line in <c>BotCore</c>), reads
/// <c>Configuration/bot-relics.json</c>, offers the three pieces of work relics make (the call to clear a lair, the carrier's walk
/// home, the thief's try), settles what the store read back, and starts the relics' clock and the <c>Relics:</c> line.
/// </summary>
public sealed class BotRelicsModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRelicsModule));

    /// <summary>How often the relics' clock turns: the lair's field and the registry's look ride on it. Five seconds.</summary>
    public static int EveryMs { get; set; } = 5000;

    /// <summary>How often the <c>Relics:</c> line is written. The population's own five minutes.</summary>
    public static int SayEveryMs { get; set; } = 300000;

    private static Timer _clock;

    private static Timer _summary;

    public override string Name => "Relics";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Will", "Squads", "Population", "Estate", "Events"];

    public override void Start()
    {
        BotRelicsConfig.Load();

        BotWill.Offer(new BotRelicCall());
        BotWill.Offer(new BotRelicBearer());
        BotWill.Offer(new BotRelicSnatch());

        BotRelicStore.Apply();

        Stop();

        _clock = Timer.DelayCall(TimeSpan.FromMilliseconds(Math.Max(1000, EveryMs)), TimeSpan.FromMilliseconds(Math.Max(1000, EveryMs)), Tick);
        _summary = Timer.DelayCall(
            TimeSpan.FromMilliseconds(Math.Max(10000, SayEveryMs)),
            TimeSpan.FromMilliseconds(Math.Max(10000, SayEveryMs)),
            () => logger.Information("Relics: {What}", BotRelics.Describe())
        );

        logger.Information(
            "The relics are {State}: {Count} in the registry; a town relic's holder pays {Discount:P0} at its town's counters, gains {Appeal:F0} appeal a relic and calls a Great Fair once a day; "
            + "a lair every {Lair:F1} hours (the first after {First} minutes), its warden at most {Odds:P0} of the company; a banner bought back for {Ransom}gp after {After:F0} hours, sent home after {Return:F0}; "
            + "a stolen relic ransomed for {Fence}gp; thieves at {Bar:F0} Stealing or better, one try every {Every} minutes on the island",
            BotRelics.Running ? "on" : "off",
            BotRelics.All.Count,
            BotRelics.Discount,
            BotRelics.Appeal,
            BotRelicLair.EveryMs / 3600000.0,
            BotRelicLair.FirstMs / 60000,
            BotRelicLair.Odds,
            BotRelics.BannerRansom,
            BotRelics.RansomAfterMs / 3600000.0,
            BotRelics.ReturnAfterMs / 3600000.0,
            BotRelics.RelicRansom,
            BotRelicSnatch.Bar,
            BotRelicSnatch.EveryMs / 60000
        );
    }

    private static void Stop()
    {
        _clock?.Stop();
        _clock = null;
        _summary?.Stop();
        _summary = null;
    }

    private static int _faults;

    private static void Tick()
    {
        if (!BotTowns.Ready)
        {
            return;
        }

        var now = Core.TickCount;

        try
        {
            BotRelics.Beat(now);
            BotRelicLair.Beat(now);
        }
        catch (Exception e)
        {
            _faults++;

            if (_faults <= 3 || _faults % 100 == 0)
            {
                logger.Error(e, "The relics' beat failed ({Times} times so far)", _faults);
            }
        }
    }

    public override void Reset()
    {
        logger.Information("Relics, before the reload: {What}", BotRelics.Describe());

        Stop();

        // The lair's warden and chest go before the counters are forgotten; the relic in it goes back into hiding.
        BotRelicLair.Abandon("the world is being reloaded");

        BotRelicLair.Forget();
        BotRelicHomeward.Forget();
        BotRelicSnatch.Forget();
        BotRelics.Forget();
    }
}

/// <summary>The relics' way in: the module and the store registered from outside <c>BotCore</c>, after it has read its own switch.</summary>
public static class BotRelicsCore
{
    /// <summary>After <c>BotCore.Configure</c> (40) and the events (45); before the world is read, which the store needs.</summary>
    [CallPriority(46)]
    public static void Configure()
    {
        if (!BotCore.Enabled)
        {
            return;
        }

        BotRelicStore.Configure();
        BotModules.Register(new BotRelicsModule());
    }
}

/// <summary><c>Configuration/bot-relics.json</c>: every key optional, PascalCase, nothing written means the code's own number.</summary>
public sealed class BotRelicsSettings
{
    public bool? Running { get; set; }

    public double? Discount { get; set; }

    public double? Appeal { get; set; }

    public bool? Lairs { get; set; }

    public int? LairEveryMs { get; set; }

    public int? LairFirstMs { get; set; }

    public double? WardenOdds { get; set; }

    public int? BannerRansom { get; set; }

    public int? RansomAfterMs { get; set; }

    public int? ReturnAfterMs { get; set; }

    public int? RelicRansom { get; set; }

    public double? Grudge { get; set; }

    public bool? Revenge { get; set; }

    public bool? WarTakesAll { get; set; }

    public bool? Thieves { get; set; }

    public double? StealBar { get; set; }
}

public static class BotRelicsConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRelicsConfig));

    private const string ConfigPath = "Configuration/bot-relics.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var s = JsonConfig.Deserialize<BotRelicsSettings>(path);

        if (s == null)
        {
            JsonConfig.Serialize(path, new BotRelicsSettings());

            logger.Information("Wrote a starter relics file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotRelics.Running = s.Running ?? BotRelics.Running;
        BotRelics.Discount = s.Discount ?? BotRelics.Discount;
        BotRelics.Appeal = s.Appeal ?? BotRelics.Appeal;
        BotRelics.BannerRansom = s.BannerRansom ?? BotRelics.BannerRansom;
        BotRelics.RansomAfterMs = s.RansomAfterMs ?? BotRelics.RansomAfterMs;
        BotRelics.ReturnAfterMs = s.ReturnAfterMs ?? BotRelics.ReturnAfterMs;
        BotRelics.RelicRansom = s.RelicRansom ?? BotRelics.RelicRansom;
        BotRelics.Grudge = s.Grudge ?? BotRelics.Grudge;
        BotRelics.Revenge = s.Revenge ?? BotRelics.Revenge;
        BotRelics.WarTakesAll = s.WarTakesAll ?? BotRelics.WarTakesAll;

        BotRelicLair.Running = s.Lairs ?? BotRelicLair.Running;
        BotRelicLair.EveryMs = s.LairEveryMs ?? BotRelicLair.EveryMs;
        BotRelicLair.FirstMs = s.LairFirstMs ?? BotRelicLair.FirstMs;
        BotRelicLair.Odds = s.WardenOdds ?? BotRelicLair.Odds;

        BotRelicSnatch.Running = s.Thieves ?? BotRelicSnatch.Running;
        BotRelicSnatch.Bar = s.StealBar ?? BotRelicSnatch.Bar;
    }
}
