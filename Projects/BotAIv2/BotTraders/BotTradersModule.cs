using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The traders as a module (01.10.2026, build 385): reads <c>Configuration/bot-traders.json</c>, offers the haul
/// (<see cref="BotHauler"/>), and writes the <c>Traders:</c> line every <see cref="BotTrading.SayEveryMs"/>.
/// </summary>
public sealed class BotTradersModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTradersModule));

    public override string Name => "Traders";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Will", "Market", "Shops", "Shopkeep"];

    private static Timer _timer;

    public override void Start()
    {
        BotTradersConfig.Load();

        BotWill.Offer(new BotHauler());

        logger.Information(
            "Traders are {State}: neutral in wars ({Neutral}); a trader takes spoken wants a minute old or more, buys them from its own stock, a bot's shop, a shopkeeper in any town, or the bots of its own town for a buyer no carrier reaches ({Gather}s at the bank), at {Least:P0} over cost or the buyer's own offer, carriage the split {Alt:P0} of what it saves the buyer's own walk, never {Most:P0} over cost; up to {Orders} buyers a haul, {Units} things; sails when worth {Sail}gp or owning a ship; every order written in its book",
            BotTrading.Running ? "trading" : "idle",
            BotTrading.NeutralInWars,
            BotTrading.GatherMs / 1000,
            BotTrading.LeastMarkup,
            BotTrading.CarriageShare,
            BotTrading.MostMarkup,
            BotTrading.MostOrders,
            BotTrading.MostUnits,
            BotTrading.SailWealth
        );

        _timer?.Stop();
        _timer = new TradersTimer(TimeSpan.FromMilliseconds(Math.Max(10000, BotTrading.SayEveryMs)));
        _timer.Start();
    }

    public override void Reset()
    {
        logger.Information("Traders, before the reload: {State}", BotTrading.Describe());

        _timer?.Stop();
        _timer = null;

        BotTrading.Forget();
    }

    private sealed class TradersTimer : Timer
    {
        public TradersTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => logger.Information("Traders: {What}", BotTrading.Describe());
    }
}

/// <summary><c>Configuration/bot-traders.json</c>: every key optional, PascalCase, nothing written means the code's own number.</summary>
public sealed class BotTradersSettings
{
    public bool? Running { get; set; }

    public bool? NeutralInWars { get; set; }

    public int? LeastCapital { get; set; }

    public int? LeastProfit { get; set; }

    public double? LeastMarkup { get; set; }

    public double? MostMarkup { get; set; }

    public double? CarriageShare { get; set; }

    public double? LeastClaim { get; set; }

    public double? MostClaim { get; set; }

    public int? FreshMs { get; set; }

    public int? LookMs { get; set; }

    public int? MostLooked { get; set; }

    public int? MostOrders { get; set; }

    public int? BatchTiles { get; set; }

    public int? MostUnits { get; set; }

    public int? NearTiles { get; set; }

    public int? ChaseMs { get; set; }

    public int? MostHaulMs { get; set; }

    public int? DeadWaitMs { get; set; }

    public int? GatherMs { get; set; }

    public double? GatherYield { get; set; }

    public bool? Sails { get; set; }

    public int? SailWealth { get; set; }

    public int? SayEveryMs { get; set; }

    public int? BookHistory { get; set; }

    public int? ShunMs { get; set; }

    public int? GatherShunMs { get; set; }

    public int? RoadStep { get; set; }

    public double? RoadDare { get; set; }

    /// <summary>The largest share of the traders that may be resting at once (build 388, Patrick's word of 01.10: at least half in the world).</summary>
    public double? RestShare { get; set; }

    /// <summary>How near the end of its rest a trader may be brought back early when more than that share is resting.</summary>
    public int? RestEarlyMinutes { get; set; }

    /// <summary>What a trader keeps in its pocket; the rest is put in its account the moment it is paid (build 388).</summary>
    public int? PocketFloat { get; set; }

    /// <summary>The least above the float worth putting away in one go.</summary>
    public int? PutAwayLeast { get; set; }
}

public static class BotTradersConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotTradersConfig));

    private const string ConfigPath = "Configuration/bot-traders.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotTradersSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotTradersSettings());

            logger.Information("Wrote a starter traders file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotTrading.Running = settings.Running ?? BotTrading.Running;
        BotTrading.NeutralInWars = settings.NeutralInWars ?? BotTrading.NeutralInWars;
        BotTrading.LeastCapital = settings.LeastCapital ?? BotTrading.LeastCapital;
        BotTrading.LeastProfit = settings.LeastProfit ?? BotTrading.LeastProfit;
        BotTrading.LeastMarkup = settings.LeastMarkup ?? BotTrading.LeastMarkup;
        BotTrading.MostMarkup = settings.MostMarkup ?? BotTrading.MostMarkup;
        BotTrading.CarriageShare = settings.CarriageShare ?? BotTrading.CarriageShare;
        BotTrading.LeastClaim = settings.LeastClaim ?? BotTrading.LeastClaim;
        BotTrading.MostClaim = settings.MostClaim ?? BotTrading.MostClaim;
        BotTrading.FreshMs = settings.FreshMs ?? BotTrading.FreshMs;
        BotTrading.LookMs = settings.LookMs ?? BotTrading.LookMs;
        BotTrading.MostLooked = settings.MostLooked ?? BotTrading.MostLooked;
        BotTrading.MostOrders = settings.MostOrders ?? BotTrading.MostOrders;
        BotTrading.BatchTiles = settings.BatchTiles ?? BotTrading.BatchTiles;
        BotTrading.MostUnits = settings.MostUnits ?? BotTrading.MostUnits;
        BotTrading.NearTiles = settings.NearTiles ?? BotTrading.NearTiles;
        BotTrading.ChaseMs = settings.ChaseMs ?? BotTrading.ChaseMs;
        BotTrading.MostHaulMs = settings.MostHaulMs ?? BotTrading.MostHaulMs;
        BotTrading.DeadWaitMs = settings.DeadWaitMs ?? BotTrading.DeadWaitMs;
        BotTrading.GatherMs = settings.GatherMs ?? BotTrading.GatherMs;
        BotTrading.GatherYield = settings.GatherYield ?? BotTrading.GatherYield;
        BotTrading.Sails = settings.Sails ?? BotTrading.Sails;
        BotTrading.SailWealth = settings.SailWealth ?? BotTrading.SailWealth;
        BotTrading.SayEveryMs = settings.SayEveryMs ?? BotTrading.SayEveryMs;
        BotTrading.BookHistory = settings.BookHistory ?? BotTrading.BookHistory;
        BotTrading.ShunMs = settings.ShunMs ?? BotTrading.ShunMs;
        BotTrading.GatherShunMs = settings.GatherShunMs ?? BotTrading.GatherShunMs;
        BotTrading.RoadStep = settings.RoadStep ?? BotTrading.RoadStep;
        BotTrading.RoadDare = settings.RoadDare ?? BotTrading.RoadDare;
        BotTrading.RestShare = settings.RestShare ?? BotTrading.RestShare;
        BotTrading.RestEarlyMinutes = settings.RestEarlyMinutes ?? BotTrading.RestEarlyMinutes;
        BotTrading.PocketFloat = settings.PocketFloat ?? BotTrading.PocketFloat;
        BotTrading.PutAwayLeast = settings.PutAwayLeast ?? BotTrading.PutAwayLeast;
    }
}
