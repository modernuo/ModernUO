using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-hunt.json</c> is allowed to say. Everything optional.
///
/// <para>
/// What is not here: what monsters exist, what they carry, how hard they hit, and where they stand. All of
/// that is the shard's own content, and a file able to disagree with it would be a file able to send a bot at
/// something it cannot beat. What a fight is worth is not here either — that is the ledger's, measured per
/// patch of ground.
/// </para>
/// </summary>
public sealed class BotHuntSettings
{
    /// <summary>How far a hunter looks for something to fight.</summary>
    public int? Reach { get; set; }

    /// <summary>How close something has to be before it is worth putting other work down for.</summary>
    public int? Notice { get; set; }

    /// <summary>How much stronger than itself a bot will deliberately set out after.</summary>
    public double? Daring { get; set; }

    /// <summary>How far a bot looks for a company and for something worth calling one against.</summary>
    public int? MusterReach { get; set; }

    /// <summary>How many others have to be free and able before a company is worth calling.</summary>
    public int? MusterLeast { get; set; }

    /// <summary>What fighting in company is reckoned at per minute before experience corrects it.</summary>
    public double? BandExpects { get; set; }

    /// <summary>How long a company that has nothing to fight is left standing before it is let go.</summary>
    public int? SquadIdleCapMs { get; set; }

    /// <summary>The share of health at which a fight is given up.</summary>
    public double? FleeAt { get; set; }

    /// <summary>The share of health needed before setting out for one.</summary>
    public double? FitAt { get; set; }

    /// <summary>How far from home a square the map calls dangerous may be and still be worth setting out for.</summary>
    public int? FearedReach { get; set; }

    /// <summary>What a fight is reckoned at per minute before experience corrects it.</summary>
    public double? Expects { get; set; }

    /// <summary>How long one hunt is expected to take.</summary>
    public double? WorkMinutes { get; set; }

    /// <summary>How full a pack may get with loot before the rest is left on the corpse.</summary>
    public double? FillFraction { get; set; }

    /// <summary>How near the ground a prowl has to get before the look counts as taken.</summary>
    public int? ProwlArriveWithin { get; set; }

    /// <summary>How long one bot keeps at a quarry whose health will not fall.</summary>
    public int? SlayNoProgressMs { get; set; }

    /// <summary>The ceiling on one solo hunt, walking and fighting together.</summary>
    public int? SlayCapMs { get; set; }

    /// <summary>Whether a band's company walks up to its quarry and gathers before it engages (build 388).</summary>
    public bool? BandGathers { get; set; }

    /// <summary>How long a band's leader waits at its quarry for the members behind it before engaging with whoever is there.</summary>
    public int? BandGatherMs { get; set; }

    /// <summary>How near its quarry a band's leader brings the company before it engages.</summary>
    public int? BandEngageTiles { get; set; }

    /// <summary>Within this of the leader a member counts as come up.</summary>
    public int? BandGatherTiles { get; set; }

    /// <summary>How far round the leader a free healer is looked for when the company levied has no medic.</summary>
    public int? BandMedicReach { get; set; }

    /// <summary>Whether the free bots within reach are weighed against the fight before a band is offered.</summary>
    public bool? MusterJudged { get; set; }

    /// <summary>Whether an archer whose arrow is due stands and shoots rather than back off (12.10.2026, w4).</summary>
    public bool? SlayStandsWhenDue { get; set; }

    /// <summary>How long a retreat may go without the quarry's health falling before the archer plants; 0 turns it off.</summary>
    public int? SlayRetreatMs { get; set; }

    /// <summary>A running bot's step afoot and mounted, in milliseconds, against which a quarry's pace is weighed.</summary>
    public int? SlayFootStepMs { get; set; }

    public int? SlayMountStepMs { get; set; }
}

/// <summary>Reads the hunt file and moves the numbers it names.</summary>
public static class BotHuntConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHuntConfig));

    private const string ConfigPath = "Configuration/bot-hunt.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotHuntSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotHuntSettings());

            logger.Information(
                "Wrote a starter hunt file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotQuarry.Reach = settings.Reach ?? BotQuarry.Reach;
        BotQuarry.Notice = settings.Notice ?? BotQuarry.Notice;
        BotQuarry.Daring = settings.Daring ?? BotQuarry.Daring;

        BotMuster.Reach = settings.MusterReach ?? BotMuster.Reach;
        BotMuster.Least = settings.MusterLeast ?? BotMuster.Least;
        BotBand.Prior = settings.BandExpects ?? BotBand.Prior;
        BotBand.Gathers = settings.BandGathers ?? BotBand.Gathers;
        BotBand.GatherMs = settings.BandGatherMs ?? BotBand.GatherMs;
        BotBand.EngageTiles = settings.BandEngageTiles ?? BotBand.EngageTiles;
        BotBand.GatherTiles = settings.BandGatherTiles ?? BotBand.GatherTiles;
        BotBand.MedicReach = settings.BandMedicReach ?? BotBand.MedicReach;
        BotMuster.Judged = settings.MusterJudged ?? BotMuster.Judged;
        BotSquad.IdleCapMs = settings.SquadIdleCapMs ?? BotSquad.IdleCapMs;

        BotSlay.FleeAt = settings.FleeAt ?? BotSlay.FleeAt;
        BotSlay.Prior = settings.Expects ?? BotSlay.Prior;
        BotSlay.WorkMinutes = settings.WorkMinutes ?? BotSlay.WorkMinutes;
        BotSlay.FillFraction = settings.FillFraction ?? BotSlay.FillFraction;

        BotHunter.FitAt = settings.FitAt ?? BotHunter.FitAt;
        BotHunter.FearedReach = settings.FearedReach ?? BotHunter.FearedReach;

        BotProwl.ArriveWithin = settings.ProwlArriveWithin ?? BotProwl.ArriveWithin;

        BotSlay.NoProgressMs = settings.SlayNoProgressMs ?? BotSlay.NoProgressMs;
        BotSlay.CapMs = settings.SlayCapMs ?? BotSlay.CapMs;

        BotSlay.StandsWhenDue = settings.SlayStandsWhenDue ?? BotSlay.StandsWhenDue;
        BotSlay.RetreatMs = settings.SlayRetreatMs ?? BotSlay.RetreatMs;
        BotSlay.FootStepMs = settings.SlayFootStepMs ?? BotSlay.FootStepMs;
        BotSlay.MountStepMs = settings.SlayMountStepMs ?? BotSlay.MountStepMs;
    }
}
