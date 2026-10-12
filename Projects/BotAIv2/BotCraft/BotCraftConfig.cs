using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-craft.json</c> is allowed to say. Everything optional.
///
/// What is not here: recipes, difficulties and yields. Those are the shard's craft system, and a file able
/// to disagree with it would be a file able to send a bot swinging at something it cannot make.
/// </summary>
public sealed class BotCraftSettings
{
    /// <summary>How far below its own skill a bot keeps its work.</summary>
    public double? Margin { get; set; }

    /// <summary>How much cloth is bought in one go.</summary>
    public int? Bolt { get; set; }

    /// <summary>How often an attempt is made, in milliseconds.</summary>
    public int? SwingMs { get; set; }

    /// <summary>What a finished piece is taken to be worth, and what the stall opens at.</summary>
    public int? GoldPerPiece { get; set; }

    /// <summary>What an afternoon at the needle is reckoned at per minute before experience corrects it.</summary>
    public double? Expects { get; set; }

    /// <summary>How long the sewing itself is expected to take.</summary>
    public double? WorkMinutes { get; set; }

    /// <summary>
    /// What an afternoon at an anvil is reckoned at per minute before experience corrects it.
    ///
    /// Added on 07.09.2026 with the rest of the smith's dials: this file was written when sewing was the
    /// only craft on the shard, and every number the forge runs on was reachable only by rebuilding. A
    /// number that cannot be moved on a living shard is a number nobody tries.
    /// </summary>
    public double? ForgePrior { get; set; }

    /// <summary>How long a stint at the anvil is expected to take.</summary>
    public double? ForgeMinutes { get; set; }

    /// <summary>How many recipes and metals the smith will try before giving a stint up.</summary>
    public int? AnvilTries { get; set; }

    /// <summary>What a stint of tinkering is reckoned at per minute, and how long it is expected to take.</summary>
    public double? TinkerPrior { get; set; }

    public double? TinkerMinutes { get; set; }

    /// <summary>How many of a tool one stint makes, and the fewest of a tool on the stalls before more is made.</summary>
    public int? TinkerBatch { get; set; }

    public int? TinkerLeastOnMarket { get; set; }

    /// <summary>Fewest iron ingots worth beginning a stint of tinkering with.</summary>
    public int? TinkerLeastMetal { get; set; }
}

/// <summary>Reads the craft file and moves the numbers it names.</summary>
public static class BotCraftConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCraftConfig));

    private const string ConfigPath = "Configuration/bot-craft.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotCraftSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotCraftSettings());

            logger.Information(
                "Wrote a starter craft file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotThread.Margin = settings.Margin ?? BotThread.Margin;

        BotSew.Bolt = settings.Bolt ?? BotSew.Bolt;
        BotSew.SwingMs = settings.SwingMs ?? BotSew.SwingMs;
        BotSew.GoldPerPiece = settings.GoldPerPiece ?? BotSew.GoldPerPiece;
        BotSew.Prior = settings.Expects ?? BotSew.Prior;
        BotSew.WorkMinutes = settings.WorkMinutes ?? BotSew.WorkMinutes;

        BotForge.Prior = settings.ForgePrior ?? BotForge.Prior;
        BotForge.WorkMinutes = settings.ForgeMinutes ?? BotForge.WorkMinutes;

        BotAnvil.Tries = settings.AnvilTries ?? BotAnvil.Tries;

        BotTinker.Prior = settings.TinkerPrior ?? BotTinker.Prior;
        BotTinker.WorkMinutes = settings.TinkerMinutes ?? BotTinker.WorkMinutes;
        BotTinker.Batch = settings.TinkerBatch ?? BotTinker.Batch;
        BotTinkerer.LeastOnMarket = settings.TinkerLeastOnMarket ?? BotTinkerer.LeastOnMarket;
        BotTinkerer.LeastMetal = settings.TinkerLeastMetal ?? BotTinkerer.LeastMetal;
    }
}
