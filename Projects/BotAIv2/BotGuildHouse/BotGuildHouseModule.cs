using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>The numbers of <c>Configuration/bot-guildhouse.json</c>. A missing key keeps the code's value.</summary>
public sealed class BotGuildHouseSettings
{
    /// <summary>Whether guilds settle in town buildings at all. See BotGuildHouses.Running.</summary>
    public bool? Running { get; set; }

    /// <summary>The most guild houses in a town other than Britain. See BotGuildHouses.MostPerTown.</summary>
    public int? MostPerTown { get; set; }

    /// <summary>The most guild houses in Britain. See BotGuildHouses.MostInBritain.</summary>
    public int? MostInBritain { get; set; }

    /// <summary>What a house in another town costs. See BotGuildHouses.TownPrice.</summary>
    public int? TownPrice { get; set; }

    /// <summary>What a house in Britain costs. See BotGuildHouses.BritainPrice.</summary>
    public int? BritainPrice { get; set; }

    /// <summary>Whether Britain waits for the hall. See BotGuildHouses.BritainAfterHall.</summary>
    public bool? BritainAfterHall { get; set; }

    /// <summary>Whether only towns fit to live in are settled in. See BotGuildHouses.FitTownsOnly.</summary>
    public bool? FitTownsOnly { get; set; }

    /// <summary>Whether a bare seat moves into a building. See BotGuildHouses.Rehouse.</summary>
    public bool? Rehouse { get; set; }

    /// <summary>How often the guilds are looked over, in milliseconds. See BotGuildHouses.LookMs.</summary>
    public int? LookMs { get; set; }

    /// <summary>Looks a guild must be gone before its house is emptied. See BotGuildHouses.GoneLooks.</summary>
    public int? GoneLooks { get; set; }

    /// <summary>How far round a house's heart a member is born or rises. See BotGuildHouses.Spread.</summary>
    public int? Spread { get; set; }

    /// <summary>How often the free buildings are counted again, in milliseconds. See BotGuildHouses.RecountMs.</summary>
    public int? RecountMs { get; set; }

    /// <summary>How often the module's clock turns, in milliseconds. See BotGuildHouseModule.TickMs.</summary>
    public int? TickMs { get; set; }

    /// <summary>The least floor of a guild house, in tiles. See BotHouseSurvey.MinArea.</summary>
    public int? MinArea { get; set; }

    /// <summary>The most floor of a guild house, in tiles. See BotHouseSurvey.MaxArea.</summary>
    public int? MaxArea { get; set; }

    /// <summary>The least share of the floor under a roof. See BotHouseSurvey.RoofShare.</summary>
    public double? RoofShare { get; set; }

    /// <summary>The most ways into a guild house. See BotHouseSurvey.MostEntrances.</summary>
    public int? MostEntrances { get; set; }

    /// <summary>Loop milliseconds one slice of the survey may take. See BotHouseSurvey.SliceMs.</summary>
    public double? SliceMs { get; set; }

    /// <summary>Whether the survey is kept in Data/bot-guild-houses.bin. See BotHouseSurvey.Cache.</summary>
    public bool? Cache { get; set; }
}

/// <summary>
/// Guild houses in the towns, as a module: reads its file, surveys the towns' empty buildings on its own clock, puts every
/// house of the save back, and looks the guilds over once a minute. Patrick's idea of 29.09.2026, evening. See
/// <see cref="BotGuildHouses"/>.
///
/// <para>
/// <b>After the estate, the travel and the residences</b>: a house is a seat (<c>BotSeat</c>, the estate's), it is chosen
/// among the towns the gates join to home (<c>BotTowns</c>, the travel's) and fit to live in (<c>BotSettle</c>, the
/// residences'), and it moves its members' residence. Switched off (<c>bots.guildhouse.enabled</c>, or <c>Running</c> in the
/// file), no guild has a house and every seat is what it was before.
/// </para>
/// </summary>
public sealed class BotGuildHouseModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotGuildHouseModule));

    private const string ConfigPath = "Configuration/bot-guildhouse.json";

    /// <summary>How often the module's clock turns: a slice of the survey while it runs, then the look's own clock.</summary>
    public static int TickMs { get; set; } = 100;

    private static Timer _timer;

    public override string Name => "GuildHouse";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Estate", "Travel", "Residence"];

    public override void Start()
    {
        Load();

        logger.Information(
            "Guild houses are {State}: a guild settles in an empty town building — a floor of {Least} to {Most} tiles, {Roof:P0} of it roofed, at most {Ways} ways in — {PerTown} to a town{Fit}, {TownPrice}; in Britain {InBritain} at most, for {Britain}gp paid to the city{After}; a door goes into every doorway and a sign on the wall, and the guild's seat, birth and rising move there; {Houses}",
            BotGuildHouses.Running ? "on" : "off",
            BotHouseSurvey.MinArea,
            BotHouseSurvey.MaxArea,
            BotHouseSurvey.RoofShare,
            BotHouseSurvey.MostEntrances,
            BotGuildHouses.MostPerTown,
            BotGuildHouses.FitTownsOnly ? " fit to live in" : "",
            BotGuildHouses.TownPrice > 0 ? $"for {BotGuildHouses.TownPrice}gp" : "for nothing",
            BotGuildHouses.MostInBritain,
            BotGuildHouses.BritainPrice,
            BotGuildHouses.BritainAfterHall ? " once its hall stands" : "",
            BotGuildHouses.Tell()
        );

        _timer?.Stop();
        _timer = null;

        if (!BotGuildHouses.Running)
        {
            return;
        }

        BotGuildHouses.Started = true;

        // The towns' boxes are what the survey walks; read now if the travel and residence modules have not already.
        BotTowns.Ensure(BotPopulation.Home);

        _timer = new GuildHouseTimer(TimeSpan.FromMilliseconds(Math.Max(20, TickMs)));
        _timer.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotGuildHouseSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotGuildHouseSettings());
            logger.Information("Wrote a starter guild house file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotGuildHouses.Running = settings.Running ?? BotGuildHouses.Running;
        BotGuildHouses.MostPerTown = settings.MostPerTown ?? BotGuildHouses.MostPerTown;
        BotGuildHouses.MostInBritain = settings.MostInBritain ?? BotGuildHouses.MostInBritain;
        BotGuildHouses.TownPrice = settings.TownPrice ?? BotGuildHouses.TownPrice;
        BotGuildHouses.BritainPrice = settings.BritainPrice ?? BotGuildHouses.BritainPrice;
        BotGuildHouses.BritainAfterHall = settings.BritainAfterHall ?? BotGuildHouses.BritainAfterHall;
        BotGuildHouses.FitTownsOnly = settings.FitTownsOnly ?? BotGuildHouses.FitTownsOnly;
        BotGuildHouses.Rehouse = settings.Rehouse ?? BotGuildHouses.Rehouse;
        BotGuildHouses.LookMs = settings.LookMs ?? BotGuildHouses.LookMs;
        BotGuildHouses.GoneLooks = settings.GoneLooks ?? BotGuildHouses.GoneLooks;
        BotGuildHouses.Spread = settings.Spread ?? BotGuildHouses.Spread;
        BotGuildHouses.RecountMs = settings.RecountMs ?? BotGuildHouses.RecountMs;
        TickMs = settings.TickMs ?? TickMs;
        BotHouseSurvey.MinArea = settings.MinArea ?? BotHouseSurvey.MinArea;
        BotHouseSurvey.MaxArea = settings.MaxArea ?? BotHouseSurvey.MaxArea;
        BotHouseSurvey.RoofShare = settings.RoofShare ?? BotHouseSurvey.RoofShare;
        BotHouseSurvey.MostEntrances = settings.MostEntrances ?? BotHouseSurvey.MostEntrances;
        BotHouseSurvey.SliceMs = settings.SliceMs ?? BotHouseSurvey.SliceMs;
        BotHouseSurvey.Cache = settings.Cache ?? BotHouseSurvey.Cache;
    }

    public override void Reset()
    {
        logger.Information("Guild houses, before the reload: {What}", BotGuildHouses.Describe());

        _timer?.Stop();
        _timer = null;

        BotGuildHouses.Forget();
        BotHouseSurvey.Forget();
    }

    private sealed class GuildHouseTimer : Timer
    {
        public GuildHouseTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => BotGuildHouses.Tick(BotPopulation.Home);
    }
}
