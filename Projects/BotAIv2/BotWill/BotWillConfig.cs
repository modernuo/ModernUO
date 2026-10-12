using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// What <c>Configuration/bot-will.json</c> is allowed to say. Everything optional; absent means the number
/// the code chose.
///
/// <para>
/// Its own file, like every other subsystem's. In the first version every knob on the shard lived in one
/// <c>bots.json</c>, so changing how readily a bot changes its mind was an edit to the file that also sets
/// the size of the population, and a typo in either half put the whole thing out.
/// </para>
///
/// <para>
/// <b>The one to look at first is <see cref="GoldPerSkillPoint"/>.</b> It is the exchange rate between the
/// two things this population is for, and every comparison between a smith's afternoon and a miner's goes
/// through it. Nothing else here changes behaviour as much.
/// </para>
/// </summary>
public sealed class BotWillSettings
{
    // ---- What work is worth. ---------------------------------------------------------------------

    /// <summary>Gold that one full point of skill is worth. The exchange rate; retune by watching.</summary>
    public double? GoldPerSkillPoint { get; set; }

    /// <summary>What dying costs, as minutes of the bot's life added to the work that killed it.</summary>
    public double? DeathMinutes { get; set; }

    /// <summary>What a point of skill is worth when it is not one this bot's class is for.</summary>
    public double? StrayFactor { get; set; }

    /// <summary>The shortest a piece of work may claim to have taken. Guards against instant successes.</summary>
    public double? LeastMinutes { get; set; }

    /// <summary>The most one settlement may claim per minute, either way.</summary>
    public double? MostPerMinute { get; set; }

    // ---- Holding on. -----------------------------------------------------------------------------

    /// <summary>How often a busy bot looks up to see whether anything better has appeared.</summary>
    public int? ReviewMs { get; set; }

    /// <summary>How soon a bot with nothing on looks again.</summary>
    public int? IdleMs { get; set; }

    /// <summary>How long fresh work is safe from being swapped out whatever the numbers say.</summary>
    public int? DwellMs { get; set; }

    /// <summary>The most fresh work may be safe for, however long it reckons itself.</summary>
    public int? DwellCapMs { get; set; }

    /// <summary>How long work may sit set aside before its reason is presumed stale.</summary>
    public int? AsideCapMs { get; set; }

    /// <summary>Whether work put down for a company's call is timed from the company letting the bot go, not from the put-down.</summary>
    public bool? Keep { get; set; }

    /// <summary>The longest work is kept for a company all told, whatever the company's length.</summary>
    public int? KeptCapMs { get; set; }

    /// <summary>How much better a new want must be to win.</summary>
    public double? SwitchMargin { get; set; }

    /// <summary>What the work in hand is worth for being underway.</summary>
    public double? Inertia { get; set; }

    /// <summary>How many times its own reckoning steadfast work is held against better offers. Nought switches the hold off.</summary>
    public double? CommitStretch { get; set; }

    /// <summary>The longest any work is held, however long it reckons itself.</summary>
    public int? CommitCapMs { get; set; }

    /// <summary>The share of the walk watchdog a walk may spend not closing before its hold is lifted.</summary>
    public double? TroubleShare { get; set; }

    /// <summary>Whether steadfast work put down for something that would not wait is taken up again afterwards.</summary>
    public bool? Resume { get; set; }

    /// <summary>The share of health below which a bot does not go back to what it put down.</summary>
    public double? ResumeHealth { get; set; }

    /// <summary>How soon taking a dropped kind of work up again counts as coming back to it.</summary>
    public int? ReturnMs { get; set; }

    /// <summary>What a bot's own trade is worth against its bare price. One switches it off. See BotCalling.</summary>
    public double? OwnTrade { get; set; }

    /// <summary>What another class's trade is worth against its bare price. One switches it off.</summary>
    public double? OtherTrade { get; set; }

    // ---- What bends an estimate. -----------------------------------------------------------------

    /// <summary>How hard a crowd already doing this puts a bot off.</summary>
    public double? CrowdBite { get; set; }

    /// <summary>The least a crowded piece of work may be discounted to.</summary>
    public double? LeastRoom { get; set; }

    /// <summary>How hard having done this here lately puts a bot off.</summary>
    public double? RepetitionBite { get; set; }

    /// <summary>What is left of work in a place where it lately went badly.</summary>
    public double? Suspicion { get; set; }

    // ---- Feelings. -------------------------------------------------------------------------------

    /// <summary>How much boredom an idle minute adds.</summary>
    public double? BoredomPerMinute { get; set; }

    /// <summary>How much boredom a hundred gold-equivalent of takings lifts.</summary>
    public double? ReliefPerHundred { get; set; }

    /// <summary>Where boredom starts changing what a bot picks rather than only being reported.</summary>
    public double? Restless { get; set; }

    // ---- Memory. ---------------------------------------------------------------------------------

    /// <summary>How many tiles across a remembered patch of ground is.</summary>
    public int? BandSize { get; set; }

    /// <summary>How many places one bot remembers.</summary>
    public int? MaxPlaces { get; set; }

    /// <summary>How much a proposer's own claim is worth, measured in settlements.</summary>
    public double? PriorWeight { get; set; }

    /// <summary>How many settlements a row may count for before the claim stops being heard.</summary>
    public int? Confidence { get; set; }

    /// <summary>How much of a fresh outcome replaces what was known.</summary>
    public double? Smoothing { get; set; }

    /// <summary>How long until half of "I have done this a lot here lately" wears off.</summary>
    public int? SpinHalfLifeMs { get; set; }

    /// <summary>How long a place stays under suspicion after work there ended badly.</summary>
    public int? CautionMs { get; set; }

    // ---- The ladder, and the log. ----------------------------------------------------------------

    /// <summary>The share of maximum health below which nothing else matters.</summary>
    public double? FailingFraction { get; set; }

    /// <summary>How long after being hit a bot still counts as under attack.</summary>
    public int? HuntedMs { get; set; }

    /// <summary>How often the population's decisions are summarised in the log.</summary>
    public int? CensusMs { get; set; }

    /// <summary>Whether every commitment and every settlement is logged as it happens.</summary>
    public bool? Chatty { get; set; }

    // ---- The breaker's ground rests (01.10.2026, build 388). See BotBreaker.GroundDeaths. ----

    public int? GroundDeaths { get; set; }

    public int? GroundWindowMs { get; set; }

    public int? GroundHalf { get; set; }

    public int? GroundRestMs { get; set; }

    public int? GroundStep { get; set; }

    public int? GroundHome { get; set; }

    public double? GroundCompany { get; set; }
}

/// <summary>Reads the decision file and moves the numbers it names.</summary>
public static class BotWillConfig
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWillConfig));

    private const string ConfigPath = "Configuration/bot-will.json";

    public static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotWillSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotWillSettings());

            logger.Information(
                "Wrote a starter decision file to {Path}; every number stays as the code has it",
                ConfigPath
            );

            return;
        }

        BotYield.GoldPerSkillPoint = settings.GoldPerSkillPoint ?? BotYield.GoldPerSkillPoint;
        BotYield.DeathMinutes = settings.DeathMinutes ?? BotYield.DeathMinutes;
        BotYield.StrayFactor = settings.StrayFactor ?? BotYield.StrayFactor;
        BotYield.LeastMinutes = settings.LeastMinutes ?? BotYield.LeastMinutes;
        BotYield.MostPerMinute = settings.MostPerMinute ?? BotYield.MostPerMinute;

        BotWill.ReviewMs = settings.ReviewMs ?? BotWill.ReviewMs;
        BotWill.IdleMs = settings.IdleMs ?? BotWill.IdleMs;
        BotWill.DwellMs = settings.DwellMs ?? BotWill.DwellMs;
        BotWill.DwellCapMs = settings.DwellCapMs ?? BotWill.DwellCapMs;
        BotWill.AsideCapMs = settings.AsideCapMs ?? BotWill.AsideCapMs;
        BotWill.Keep = settings.Keep ?? BotWill.Keep;
        BotWill.KeptCapMs = settings.KeptCapMs ?? BotWill.KeptCapMs;
        BotWill.SwitchMargin = settings.SwitchMargin ?? BotWill.SwitchMargin;
        BotWill.CommitStretch = settings.CommitStretch ?? BotWill.CommitStretch;
        BotWill.CommitCapMs = settings.CommitCapMs ?? BotWill.CommitCapMs;
        BotWill.TroubleShare = settings.TroubleShare ?? BotWill.TroubleShare;
        BotWill.Resume = settings.Resume ?? BotWill.Resume;
        BotWill.ResumeHealth = settings.ResumeHealth ?? BotWill.ResumeHealth;
        BotWill.ReturnMs = settings.ReturnMs ?? BotWill.ReturnMs;
        BotCalling.OwnTrade = settings.OwnTrade ?? BotCalling.OwnTrade;
        BotCalling.OtherTrade = settings.OtherTrade ?? BotCalling.OtherTrade;
        BotWill.CensusMs = settings.CensusMs ?? BotWill.CensusMs;
        BotWill.Chatty = settings.Chatty ?? BotWill.Chatty;

        BotAppraisal.Inertia = settings.Inertia ?? BotAppraisal.Inertia;
        BotAppraisal.CrowdBite = settings.CrowdBite ?? BotAppraisal.CrowdBite;
        BotAppraisal.LeastRoom = settings.LeastRoom ?? BotAppraisal.LeastRoom;
        BotAppraisal.RepetitionBite = settings.RepetitionBite ?? BotAppraisal.RepetitionBite;
        BotAppraisal.Suspicion = settings.Suspicion ?? BotAppraisal.Suspicion;

        BotUrges.BoredomPerMinute = settings.BoredomPerMinute ?? BotUrges.BoredomPerMinute;
        BotUrges.ReliefPerHundred = settings.ReliefPerHundred ?? BotUrges.ReliefPerHundred;
        BotUrges.Restless = settings.Restless ?? BotUrges.Restless;

        BotLedger.BandSize = settings.BandSize ?? BotLedger.BandSize;
        BotLedger.MaxPlaces = settings.MaxPlaces ?? BotLedger.MaxPlaces;
        BotLedger.PriorWeight = settings.PriorWeight ?? BotLedger.PriorWeight;
        BotLedger.Confidence = settings.Confidence ?? BotLedger.Confidence;
        BotLedger.Smoothing = settings.Smoothing ?? BotLedger.Smoothing;
        BotLedger.SpinHalfLifeMs = settings.SpinHalfLifeMs ?? BotLedger.SpinHalfLifeMs;
        BotLedger.CautionMs = settings.CautionMs ?? BotLedger.CautionMs;

        BotLadder.FailingFraction = settings.FailingFraction ?? BotLadder.FailingFraction;
        BotLadder.HuntedMs = settings.HuntedMs ?? BotLadder.HuntedMs;

        BotBreaker.GroundDeaths = settings.GroundDeaths ?? BotBreaker.GroundDeaths;
        BotBreaker.GroundWindowMs = settings.GroundWindowMs ?? BotBreaker.GroundWindowMs;
        BotBreaker.GroundHalf = settings.GroundHalf ?? BotBreaker.GroundHalf;
        BotBreaker.GroundRestMs = settings.GroundRestMs ?? BotBreaker.GroundRestMs;
        BotBreaker.GroundStep = settings.GroundStep ?? BotBreaker.GroundStep;
        BotBreaker.GroundHome = settings.GroundHome ?? BotBreaker.GroundHome;
        BotBreaker.GroundCompany = settings.GroundCompany ?? BotBreaker.GroundCompany;
    }
}
