using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-harvest.json</c> is allowed to say. Everything optional; absent means the
/// number the code chose.
///
/// <para>
/// What is deliberately <b>not</b> here: which tiles are ore, what a vein contains, how much a swing
/// yields, and how many ingots a pile of ore becomes. Those are the engine's, and a configuration file able
/// to disagree with the engine about them is a file able to send a population to dig a beach — which the
/// first version did, all night, because two of its tests said sand was workable and the engine did not
/// agree.
/// </para>
/// </summary>
public sealed class BotHarvestSettings
{
    // ---- The sweep: how the population comes to know where things are. --------------------------

    /// <summary>How far around a bot one sweep looks.</summary>
    public int? SweepReach { get; set; }

    /// <summary>How coarsely the sweep samples for rock. Forges are always looked for on every tile.</summary>
    public int? SweepStride { get; set; }

    /// <summary>How far apart two rock tiles have to be to be remembered as two seams.</summary>
    public int? SeamSpacing { get; set; }

    /// <summary>How far apart two forges have to be to count as two workshops.</summary>
    public int? PlaceSpacing { get; set; }

    /// <summary>How near a forge an anvil must stand for the pair to be a workshop.</summary>
    public int? AnvilReach { get; set; }

    public int? MaxSeams { get; set; }

    /// <summary>Milliseconds a bot's answer to "which seam" stands before the list is walked for it again.</summary>
    public int? SeamAskEveryMs { get; set; }

    public int? MaxPlaces { get; set; }

    /// <summary>How many sweeps the population may run in one world.</summary>
    public int? MaxSurveys { get; set; }

    // ---- Ore. ----------------------------------------------------------------------------------

    /// <summary>How far a bot looks around itself for something worth swinging at.</summary>
    public int? LookReach { get; set; }

    /// <summary>How near a forge the ore has to be to go into it.</summary>
    public int? FireReach { get; set; }

    /// <summary>Ore enough to be worth carrying to a fire.</summary>
    public int? WorthSmelting { get; set; }

    // ---- The trip. -----------------------------------------------------------------------------

    /// <summary>What a trip is expected to come to per minute before experience corrects it.</summary>
    public double? Expects { get; set; }

    /// <summary>How long the digging itself is expected to take, in minutes.</summary>
    public double? WorkMinutes { get; set; }

    /// <summary>What an ingot is taken to be worth. A stand-in until the shard sets prices.</summary>
    public int? GoldPerIngot { get; set; }

    /// <summary>How full a pack may get before the bot heads for a fire.</summary>
    public double? FillFraction { get; set; }

    /// <summary>Ore enough to head for a fire whatever the scales say.</summary>
    public int? TargetOre { get; set; }

    /// <summary>How many fruitless swings before a tile is treated as spent.</summary>
    public int? DryLimit { get; set; }

    /// <summary>How near a counter is near enough to put things away.</summary>
    public int? CounterReach { get; set; }

    /// <summary>How many times a trip may pick somewhere else before giving up.</summary>
    public int? MaxBends { get; set; }

    /// <summary>How many emptied rocks one trip works through before taking what it has and going.</summary>
    public int? MaxSpent { get; set; }
}

/// <summary>Reads the harvest file and moves the numbers it names.</summary>
public static class BotHarvestConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHarvestConfig));

    private const string ConfigPath = "Configuration/bot-harvest.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotHarvestSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotHarvestSettings());

            logger.Information(
                "Wrote a starter harvest file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotGround.Reach = settings.SweepReach ?? BotGround.Reach;
        BotGround.Stride = settings.SweepStride ?? BotGround.Stride;
        BotGround.SeamSpacing = settings.SeamSpacing ?? BotGround.SeamSpacing;
        BotGround.PlaceSpacing = settings.PlaceSpacing ?? BotGround.PlaceSpacing;
        BotGround.AnvilReach = settings.AnvilReach ?? BotGround.AnvilReach;
        BotGround.MaxSeams = settings.MaxSeams ?? BotGround.MaxSeams;
        BotGround.AskEveryMs = settings.SeamAskEveryMs ?? BotGround.AskEveryMs;
        BotGround.MaxPlaces = settings.MaxPlaces ?? BotGround.MaxPlaces;
        BotGround.MaxSurveys = settings.MaxSurveys ?? BotGround.MaxSurveys;

        BotOre.Reach = settings.LookReach ?? BotOre.Reach;
        BotOre.FireReach = settings.FireReach ?? BotOre.FireReach;
        BotOre.WorthSmelting = settings.WorthSmelting ?? BotOre.WorthSmelting;

        BotDig.Prior = settings.Expects ?? BotDig.Prior;
        BotDig.WorkMinutes = settings.WorkMinutes ?? BotDig.WorkMinutes;
        BotDig.GoldPerIngot = settings.GoldPerIngot ?? BotDig.GoldPerIngot;
        BotDig.FillFraction = settings.FillFraction ?? BotDig.FillFraction;
        BotDig.TargetOre = settings.TargetOre ?? BotDig.TargetOre;
        BotDig.DryLimit = settings.DryLimit ?? BotDig.DryLimit;
        BotDig.CounterReach = settings.CounterReach ?? BotDig.CounterReach;
        BotDig.MaxBends = settings.MaxBends ?? BotDig.MaxBends;
        BotDig.MaxSpent = settings.MaxSpent ?? BotDig.MaxSpent;
    }
}
