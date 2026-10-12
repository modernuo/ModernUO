using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-botshops.json</c> is allowed to say. Everything optional; an empty file keeps the code's numbers.
///
/// <para>
/// <b>PascalCase, and it is not a style question.</b> The deserialiser matches these names as written, so a key in lower case
/// is not an error and not a warning — it is a value silently left at its default. The boot line the module writes is the only
/// proof of what the shops are running with.
/// </para>
/// </summary>
public sealed class BotShopkeepSettings
{
    /// <summary>Whether bots keep shops at all. Off, the market has no shops: goods go by carrier or to a shopkeeper.</summary>
    public bool? Running { get; set; }

    public double? ShiftMinutes { get; set; }

    public int? RestMs { get; set; }

    public int? OpenWorth { get; set; }

    public double? SellShare { get; set; }

    public int? LeastShifts { get; set; }

    public double? LeastClaim { get; set; }

    public double? MostClaim { get; set; }

    public int? BankTiles { get; set; }

    public int? PitchNear { get; set; }

    public int? PitchFar { get; set; }

    public int? Apart { get; set; }

    public int? PerBank { get; set; }

    public int? MostOpen { get; set; }

    public int? ServeReach { get; set; }

    public int? SeekTiles { get; set; }

    /// <summary>What a minute's walk to a shop is worth in gold. Follows <c>BotRestock.Prior</c> unless given.</summary>
    public double? WalkGold { get; set; }

    public int? CryMs { get; set; }

    public int? SayEveryMs { get; set; }
}

/// <summary>Reads the shops' file and moves the numbers it names; writes an empty starter file when there is none.</summary>
public static class BotShopkeepConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotShopkeepConfig));

    private const string ConfigPath = "Configuration/bot-botshops.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotShopkeepSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotShopkeepSettings());

            logger.Information("Wrote a starter shops file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotShopkeep.Running = settings.Running ?? BotShopkeep.Running;
        BotShopkeep.ShiftMinutes = settings.ShiftMinutes ?? BotShopkeep.ShiftMinutes;
        BotShopkeep.RestMs = settings.RestMs ?? BotShopkeep.RestMs;
        BotShopkeep.OpenWorth = settings.OpenWorth ?? BotShopkeep.OpenWorth;
        BotShopkeep.SellShare = settings.SellShare ?? BotShopkeep.SellShare;
        BotShopkeep.LeastShifts = settings.LeastShifts ?? BotShopkeep.LeastShifts;
        BotShopkeep.LeastClaim = settings.LeastClaim ?? BotShopkeep.LeastClaim;
        BotShopkeep.MostClaim = settings.MostClaim ?? BotShopkeep.MostClaim;
        BotShopkeep.BankTiles = settings.BankTiles ?? BotShopkeep.BankTiles;
        BotShopkeep.PitchNear = settings.PitchNear ?? BotShopkeep.PitchNear;
        BotShopkeep.PitchFar = settings.PitchFar ?? BotShopkeep.PitchFar;
        BotShopkeep.Apart = settings.Apart ?? BotShopkeep.Apart;
        BotShopkeep.PerBank = settings.PerBank ?? BotShopkeep.PerBank;
        BotShopkeep.MostOpen = settings.MostOpen ?? BotShopkeep.MostOpen;
        BotShopkeep.ServeReach = settings.ServeReach ?? BotShopkeep.ServeReach;
        BotShopkeep.SeekTiles = settings.SeekTiles ?? BotShopkeep.SeekTiles;
        BotShopkeep.CryMs = settings.CryMs ?? BotShopkeep.CryMs;
        BotShopkeepModule.SayEveryMs = settings.SayEveryMs ?? BotShopkeepModule.SayEveryMs;

        // Only when the file gives it: read back as "?? WalkGold" it would freeze today's BotRestock.Prior into it for good.
        if (settings.WalkGold is { } walk)
        {
            BotShopkeep.WalkGold = walk;
        }
    }
}
