using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>The numbers of <c>Configuration/bot-residence.json</c>. A missing key keeps the code's value.</summary>
public sealed class BotResidenceSettings
{
    /// <summary>Whether bots have towns of their own. See BotResidence.Running.</summary>
    public bool? Running { get; set; }

    /// <summary>The home town's (Britain's) share of the population. See BotSettle.HomeShare.</summary>
    public double? HomeShare { get; set; }

    /// <summary>How hard a town already full of its share is passed over. See BotSettle.SpreadWeight.</summary>
    public double? SpreadWeight { get; set; }

    /// <summary>How much a town where the bot's guildmates live weighs more. See BotSettle.GuildPull.</summary>
    public double? GuildPull { get; set; }

    /// <summary>The least shopkeepers a town must have to be lived in. See BotSettle.LeastShops.</summary>
    public int? LeastShops { get; set; }

    /// <summary>Whether a town without a bank is passed over. See BotSettle.NeedsBank.</summary>
    public bool? NeedsBank { get; set; }

    /// <summary>Whether a town reached only through a gate may be lived in. See BotSettle.GateTowns.</summary>
    public bool? GateTowns { get; set; }

    /// <summary>How far round a town's bank a bot is born or rises. See BotResidence.TownScatter.</summary>
    public int? TownScatter { get; set; }

    /// <summary>Hours a bot's living is judged over. See BotRelocate.StretchHours.</summary>
    public double? StretchHours { get; set; }

    /// <summary>Gold under which a stretch was a poor living. See BotRelocate.PoorGold.</summary>
    public int? PoorGold { get; set; }

    /// <summary>Share of the trade under which a stretch was a poor living. See BotRelocate.PoorProgress.</summary>
    public double? PoorProgress { get; set; }

    /// <summary>Deaths where it lives that make a bot think of moving. See BotRelocate.DeathsToMove.</summary>
    public int? DeathsToMove { get; set; }

    /// <summary>Hours those deaths are counted over. See BotRelocate.DeathHours.</summary>
    public double? DeathHours { get; set; }

    /// <summary>The least hours between settling and moving on. See BotRelocate.MoveEveryHours.</summary>
    public double? MoveEveryHours { get; set; }

    /// <summary>The chance a bot with a reason to move is marked to. See BotRelocate.MoveOdds.</summary>
    public double? MoveOdds { get; set; }

    /// <summary>Minutes a marked move stays on offer. See BotRelocate.PendingMinutes.</summary>
    public double? PendingMinutes { get; set; }

    /// <summary>What moving house is worth a minute. See BotResettle.Prior.</summary>
    public double? Prior { get; set; }

    /// <summary>The classes that live in the home town whatever the choice. See BotRelocate.StayHome.</summary>
    public string[] StayHome { get; set; }
}

/// <summary>
/// Towns of residence, as a module: reads its file, hands the bots already in the world the towns their names carry, offers
/// the move, and looks the population over on its own clock. Patrick's order of 29.09.2026, point 5. See
/// <see cref="BotResidence"/>.
///
/// <para>
/// <b>After the population and the travel</b>, because it files bots that exist, judges towns by roads the travel module
/// draws, and walks a move by the traveller's own deed. Switched off (<c>bots.residence.enabled</c>, or <c>Running</c> in
/// the file), every bot's home is its guild's seat or the population's, exactly as before.
/// </para>
/// </summary>
public sealed class BotResidenceModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotResidenceModule));

    private const string ConfigPath = "Configuration/bot-residence.json";

    /// <summary>How often the module's clock looks: the towns until they are judged, then the population once a <c>BotRelocate.LookEveryMs</c>.</summary>
    public static int SliceMs { get; set; } = 5000;

    private static Timer _timer;

    public override string Name => "Residence";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Travel"];

    public override void Start()
    {
        Load();

        // The towns read off the regions now, so a bot that came back from the save in its town has that town from the first
        // beat; which of them can be walked to waits for the gates. See BotTowns.Ensure.
        BotTowns.Ensure(BotPopulation.Home);
        BotResidence.Start();

        if (BotResidence.Active)
        {
            BotWill.Offer(new BotMover());
        }

        logger.Information(
            "Residences are {State}: each bot lives in a town of its own — home keeps {Share:P0}, the spread weighs ×{Spread:F1}, a guild pulls ×{Pull:F1}; a town needs {Shops} shops{Bank} and {Way}; a living is judged over {Stretch:0.#}h (under {Gold}gp and {Progress:P0} of the trade is poor) and {Deaths} deaths in {DeathHours:0.#}h where it lives is too many; a move at most once in {Every:0.#}h, taken at {Odds:P0}, worth {Prior}/min; {Unhomed} newborns of the boot to move in once the towns are read",
            BotResidence.Active ? "on" : "off",
            BotSettle.HomeShare,
            BotSettle.SpreadWeight,
            BotSettle.GuildPull,
            BotSettle.LeastShops,
            BotSettle.NeedsBank ? " and a bank" : "",
            BotSettle.GateTowns ? "a road in the book or a gate" : "a road in the book",
            BotRelocate.StretchHours,
            BotRelocate.PoorGold,
            BotRelocate.PoorProgress,
            BotRelocate.DeathsToMove,
            BotRelocate.DeathHours,
            BotRelocate.MoveEveryHours,
            BotRelocate.MoveOdds,
            BotResettle.Prior,
            BotResidence.Unhomed.Count
        );

        _timer?.Stop();
        _timer = null;

        if (!BotResidence.Active)
        {
            return;
        }

        _timer = new ResidenceTimer(TimeSpan.FromMilliseconds(Math.Max(250, SliceMs)));
        _timer.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotResidenceSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotResidenceSettings());
            logger.Information("Wrote a starter residence file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotResidence.Running = settings.Running ?? BotResidence.Running;
        BotResidence.TownScatter = settings.TownScatter ?? BotResidence.TownScatter;
        BotSettle.HomeShare = settings.HomeShare ?? BotSettle.HomeShare;
        BotSettle.SpreadWeight = settings.SpreadWeight ?? BotSettle.SpreadWeight;
        BotSettle.GuildPull = settings.GuildPull ?? BotSettle.GuildPull;
        BotSettle.LeastShops = settings.LeastShops ?? BotSettle.LeastShops;
        BotSettle.NeedsBank = settings.NeedsBank ?? BotSettle.NeedsBank;
        BotSettle.GateTowns = settings.GateTowns ?? BotSettle.GateTowns;
        BotRelocate.StretchHours = settings.StretchHours ?? BotRelocate.StretchHours;
        BotRelocate.PoorGold = settings.PoorGold ?? BotRelocate.PoorGold;
        BotRelocate.PoorProgress = settings.PoorProgress ?? BotRelocate.PoorProgress;
        BotRelocate.DeathsToMove = settings.DeathsToMove ?? BotRelocate.DeathsToMove;
        BotRelocate.DeathHours = settings.DeathHours ?? BotRelocate.DeathHours;
        BotRelocate.MoveEveryHours = settings.MoveEveryHours ?? BotRelocate.MoveEveryHours;
        BotRelocate.MoveOdds = settings.MoveOdds ?? BotRelocate.MoveOdds;
        BotRelocate.PendingMinutes = settings.PendingMinutes ?? BotRelocate.PendingMinutes;
        BotResettle.Prior = settings.Prior ?? BotResettle.Prior;
        BotRelocate.StayHome = settings.StayHome ?? BotRelocate.StayHome;
    }

    public override void Reset()
    {
        logger.Information("Residences, before the reload: {What}", BotResidence.Describe());

        _timer?.Stop();
        _timer = null;

        BotResidence.Forget();
        BotRelocate.Forget();
        BotMover.Forget();
        BotSettle.Forget();
    }

    private sealed class ResidenceTimer : Timer
    {
        public ResidenceTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => BotRelocate.Tick();
    }
}
