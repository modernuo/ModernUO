using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-shops.json</c> is allowed to say. Everything optional.
///
/// No prices here either: what a shopkeeper charges is the shard's own business, scalars and all, and this
/// file only says how far a bot will walk and how empty a supply has to get.
/// </summary>
public sealed class BotShopsSettings
{
    /// <summary>How far around a bot one sweep looks for shopkeepers.</summary>
    public int? Reach { get; set; }

    /// <summary>How near a shopkeeper a bot must stand to trade.</summary>
    public int? CounterReach { get; set; }

    /// <summary>How many shopkeepers the population may remember.</summary>
    public int? MaxShops { get; set; }

    /// <summary>How far below its birth allowance a supply falls before the bot goes shopping.</summary>
    public double? Short { get; set; }

    /// <summary>What a trip to the shops is reckoned at per minute before experience corrects it.</summary>
    public double? Expects { get; set; }

    /// <summary>How long the errand itself is expected to take.</summary>
    public double? WorkMinutes { get; set; }

    /// <summary>
    /// How many minutes' walk from a counter makes a supply a want said aloud instead of a walk; nought never. See
    /// <c>BotShopper.AskFarMinutes</c>. (It replaced PeddleAfterMs, the minutes a stall sat unsold before its goods went to a
    /// shopkeeper, which went with the auction on 01.10.2026.)
    /// </summary>
    public double? AskFarMinutes { get; set; }

    /// <summary>How long a bot waits for a carrier to bring a supply it asked for aloud before it walks. See <c>BotShopper.AskFarMs</c>.</summary>
    public int? AskFarMs { get; set; }

    /// <summary>How long a bot holding some of a piled consumable waits for a carrier before it walks to a counter. See <c>BotShopper.PileMs</c>.</summary>
    public int? PileMs { get; set; }

    /// <summary>
    /// Whether the capital pays its price and draws loads to it (<c>BotCapital.Running</c>). Patrick's order of 30.09.2026,
    /// evening: Britain is the centre of trade.
    /// </summary>
    public bool? CapitalRunning { get; set; }

    /// <summary>The town that is the centre of trade, by its region's name (<c>BotCapital.Name</c>, "Britain").</summary>
    public string Capital { get; set; }

    /// <summary>
    /// What the capital's shopkeepers pay against their own price, the difference out of the city's treasury
    /// (<c>BotCapital.Premium</c>, 1.25). The one number here that is about a price, and it is a lever with a budget: the city's
    /// mint bounds it.
    /// </summary>
    public double? CapitalPremium { get; set; }

    /// <summary>How far from a bot the capital's price reaches in the choice of a buyer (<c>BotCapital.Reach</c>, 1000).</summary>
    public int? CapitalReach { get; set; }
}

/// <summary>Reads the shops file and moves the numbers it names.</summary>
public static class BotShopsConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotShopsConfig));

    private const string ConfigPath = "Configuration/bot-shops.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotShopsSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotShopsSettings());

            logger.Information(
                "Wrote a starter shops file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotShops.Reach = settings.Reach ?? BotShops.Reach;
        BotShops.CounterReach = settings.CounterReach ?? BotShops.CounterReach;
        BotShops.MaxShops = settings.MaxShops ?? BotShops.MaxShops;

        BotShopper.Short = settings.Short ?? BotShopper.Short;

        BotRestock.Prior = settings.Expects ?? BotRestock.Prior;
        BotShopper.AskFarMinutes = settings.AskFarMinutes ?? BotShopper.AskFarMinutes;
        BotShopper.AskFarMs = settings.AskFarMs ?? BotShopper.AskFarMs;
        BotShopper.PileMs = settings.PileMs ?? BotShopper.PileMs;
        BotRestock.WorkMinutes = settings.WorkMinutes ?? BotRestock.WorkMinutes;

        BotCapital.Running = settings.CapitalRunning ?? BotCapital.Running;
        BotCapital.Name = string.IsNullOrWhiteSpace(settings.Capital) ? BotCapital.Name : settings.Capital;
        BotCapital.Premium = settings.CapitalPremium ?? BotCapital.Premium;
        BotCapital.Reach = settings.CapitalReach ?? BotCapital.Reach;
    }
}
