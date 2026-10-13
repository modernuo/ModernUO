using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-squad.json</c> may say. Everything optional.
///
/// Numbers only. The rules are not configurable and should not be: that blades stand in front of casters, that
/// a fight is judged by whether the target's health is falling, that nobody stands still — those are not
/// preferences, they are what four separate defects taught, and a file able to disagree with them is a file
/// able to reinstate them.
/// </summary>
public sealed class BotSquadSettings
{
    /// <summary>Most a squad may hold.</summary>
    public int? MaxSize { get; set; }

    /// <summary>
    /// The longest a single attack can take to come round again. The break-off window is this times
    /// <see cref="Blows"/> — set the parts rather than the product, so the squad's patience and its
    /// restationing clock cannot be tuned apart from one another.
    /// </summary>
    public int? SlowestBlowMs { get; set; }

    /// <summary>How many fruitless swings of the slowest weapon a company sits through before breaking off.</summary>
    public int? Blows { get; set; }

    /// <summary>The hard ceiling on one fight.</summary>
    public int? FightCapMs { get; set; }

    /// <summary>How long a company stands on something it cannot hit before giving it up. See BotSquad.BlindMs.</summary>
    public int? BlindMs { get; set; }

    /// <summary>How long a company goes without a fight before letting go of members idle in it.</summary>
    public int? RestCapMs { get; set; }

    /// <summary>How long a company with nothing to fight lasts at all.</summary>
    public int? IdleCapMs { get; set; }

    /// <summary>How many bots make a knot while sweeping.</summary>
    public int? KnotSize { get; set; }

    /// <summary>How far a knot goes from the anchor.</summary>
    public int? Spread { get; set; }

    /// <summary>How close a member has to be to be counted in a share-out.</summary>
    public int? Earshot { get; set; }

    /// <summary>How far around a member the squad looks when working out what is attacking it.</summary>
    public int? Reach { get; set; }

    /// <summary>Whether any fighter may call a company to clear a square where bots lately died (02.10.2026). See <see cref="BotClearer"/>.</summary>
    public bool? Clearings { get; set; }

    /// <summary>How far from where it stands a fighter looks for such a square.</summary>
    public int? ClearReach { get; set; }

    /// <summary>How many deaths a square needs, inside <see cref="ClearRecentMs"/>, to be offered.</summary>
    public int? ClearDeaths { get; set; }

    /// <summary>How lately the last of those deaths must have fallen, in milliseconds.</summary>
    public int? ClearRecentMs { get; set; }

    /// <summary>The main skill a fighter needs to call a clearing.</summary>
    public double? ClearLeastSkill { get; set; }

    /// <summary>How many times the strength a square asks a clearing's company brings before it marches.</summary>
    public double? ClearFit { get; set; }

    /// <summary>What a clearing is reckoned at per minute.</summary>
    public double? ClearExpects { get; set; }

    /// <summary>How near a road between towns a square must lie to count as on it.</summary>
    public int? ClearRoadWidth { get; set; }

    /// <summary>Whether a company whose fight is well in hand spares members to a dire cry near it. See <c>BotSquadSpare</c>.</summary>
    public bool? Spares { get; set; }

    /// <summary>The share of the company's strength its own enemy may come to for the fight to count as well in hand.</summary>
    public double? SpareShare { get; set; }

    /// <summary>How far from the thing on the crier a member may be to be spared to it.</summary>
    public int? SpareReach { get; set; }

    /// <summary>The most members spared to one cry.</summary>
    public int? MostSpared { get; set; }

    /// <summary>How long a spared member is left on the thing before the press takes it back, in milliseconds.</summary>
    public int? SpareMs { get; set; }

    /// <summary>Under this share of its health a member makes its company one that spares nobody (12.10.2026, w5).</summary>
    public double? SpareDyingShare { get; set; }
}

/// <summary>Reads the squad file and moves the numbers it names.</summary>
public static class BotSquadConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSquadConfig));

    private const string ConfigPath = "Configuration/bot-squad.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotSquadSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotSquadSettings());

            logger.Information(
                "Wrote a starter squad file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotSquad.MaxSize = settings.MaxSize ?? BotSquad.MaxSize;
        BotSquad.SlowestBlowMs = settings.SlowestBlowMs ?? BotSquad.SlowestBlowMs;
        BotSquad.Blows = settings.Blows ?? BotSquad.Blows;
        BotSquad.FightCapMs = settings.FightCapMs ?? BotSquad.FightCapMs;
        BotSquad.BlindMs = settings.BlindMs ?? BotSquad.BlindMs;
        BotSquad.RestCapMs = settings.RestCapMs ?? BotSquad.RestCapMs;
        BotSquad.IdleCapMs = settings.IdleCapMs ?? BotSquad.IdleCapMs;
        BotScatter.KnotSize = settings.KnotSize ?? BotScatter.KnotSize;
        BotScatter.Spread = settings.Spread ?? BotScatter.Spread;
        BotSpoils.Earshot = settings.Earshot ?? BotSpoils.Earshot;
        BotSquads.Reach = settings.Reach ?? BotSquads.Reach;

        // Clearings (02.10.2026). See BotClearer and BotHarrow.ClearFit.
        BotClearer.Running = settings.Clearings ?? BotClearer.Running;
        BotClearer.Reach = settings.ClearReach ?? BotClearer.Reach;
        BotClearer.LeastDeaths = settings.ClearDeaths ?? BotClearer.LeastDeaths;
        BotClearer.RecentMs = settings.ClearRecentMs ?? BotClearer.RecentMs;
        BotClearer.LeastSkill = settings.ClearLeastSkill ?? BotClearer.LeastSkill;
        BotClearer.RoadWidth = settings.ClearRoadWidth ?? BotClearer.RoadWidth;
        BotHarrow.ClearFit = settings.ClearFit ?? BotHarrow.ClearFit;
        BotHarrow.ClearPrior = settings.ClearExpects ?? BotHarrow.ClearPrior;

        // A company sparing members to a dire cry (06.10.2026, react18; bound to the file on 12.10.2026, w5). See BotSquadSpare.
        BotSquad.Spares = settings.Spares ?? BotSquad.Spares;
        BotSquad.SpareShare = settings.SpareShare ?? BotSquad.SpareShare;
        BotSquad.SpareReach = settings.SpareReach ?? BotSquad.SpareReach;
        BotSquad.MostSpared = settings.MostSpared ?? BotSquad.MostSpared;
        BotSquad.SpareMs = settings.SpareMs ?? BotSquad.SpareMs;
        BotSquad.SpareDyingShare = settings.SpareDyingShare ?? BotSquad.SpareDyingShare;
    }
}
