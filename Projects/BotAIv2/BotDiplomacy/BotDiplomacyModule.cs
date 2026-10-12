using System;
using System.Collections.Generic;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>The numbers of <c>Configuration/bot-diplomacy.json</c>. A missing key keeps the code's value; keys are PascalCase.</summary>
public sealed class BotDiplomacySettings
{
    /// <summary>Whether wars and alliances wait for a meeting. See BotParley.Running.</summary>
    public bool? Running { get; set; }

    /// <summary>Whether guilds deal with their towns. See BotBurgh.Running.</summary>
    public bool? Towns { get; set; }

    public int? MostMeetings { get; set; }

    public int? MostAudiences { get; set; }

    public double? OfferMinutes { get; set; }

    public double? MarchMinutes { get; set; }

    public double? HostWaitMinutes { get; set; }

    public double? WitnessWaitMinutes { get; set; }

    public double? JudgeWaitSeconds { get; set; }

    public double? LineSeconds { get; set; }

    public double? LongestMinutes { get; set; }

    public double? MeetEveryMinutes { get; set; }

    public double? RetryMinutes { get; set; }

    public int? ArriveTiles { get; set; }

    public int? HostCallTiles { get; set; }

    public double? WaryShare { get; set; }

    public double? Cordial { get; set; }

    public double? CallOdds { get; set; }

    public int? SeatTownTiles { get; set; }

    /// <summary>Whether a guild seated inside its town's walls is met at its seat rather than the town's hall. See BotParley.MeetAtSeat.</summary>
    public bool? MeetAtSeat { get; set; }

    /// <summary>A town's main building, by town name, as [x, y]. Everywhere else the bank.</summary>
    public Dictionary<string, int[]> Halls { get; set; }

    /// <summary>The model the duke and the towns' voice are asked with; empty for the population's own (BotOllama.Model).</summary>
    public string JudgeModel { get; set; }

    public string JudgeKeepAlive { get; set; }

    public int? JudgeTimeoutMs { get; set; }

    public double? PactHours { get; set; }

    public double? TradeDiscount { get; set; }

    public int? TradesForTerms { get; set; }

    /// <summary>What going to a meeting is worth a minute. See BotEnvoy.Prior.</summary>
    public double? Prior { get; set; }

    public double? AudienceEveryMinutes { get; set; }

    public int? LeastResidents { get; set; }

    public double? PriceStep { get; set; }

    public double? LeastFactor { get; set; }

    public double? MostFactor { get; set; }

    public int? FailsToExile { get; set; }

    public double? ExileHours { get; set; }

    public bool? ExileMovesSeat { get; set; }

    public double? TaskHours { get; set; }

    public double? ClearGain { get; set; }

    public double? ClearPresence { get; set; }

    public double? ClearOdds { get; set; }

    public double? GuardMinutes { get; set; }

    public int? SupplyUnits { get; set; }

    public int? TownReach { get; set; }
}

/// <summary>
/// Meetings between guilds and with the towns, as a module: reads its file, fills in the meetings' words, offers the envoy's
/// errand, and runs the meetings' clock. Patrick's order of 29.09.2026, evening. See <see cref="BotParley"/>.
///
/// <para>
/// <b>World phase, after the population, the will and the travel</b>, because it offers work, walks to towns the travel module
/// reads, and asks where guilds live. Switched off (<c>bots.diplomacy.enabled</c> or <c>Running</c> in the file), a grievance
/// declares war and a friendship makes an alliance at once, exactly as before; the agreements already sworn keep their effect
/// on the market until they run out, since they are read wherever goods change hands.
/// </para>
/// </summary>
public sealed class BotDiplomacyModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotDiplomacyModule));

    private const string ConfigPath = "Configuration/bot-diplomacy.json";

    private static Timer _timer;

    private static bool _resolved;

    public override string Name => "Diplomacy";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will", "Travel"];

    public override void Start()
    {
        Load();

        BotDuke.Phrases();
        BotWill.Offer(new BotHerald());

        // A guard a town set is offered as work (30.09.2026, build 339). See BotTownWatch.
        BotWill.Offer(new BotTownWatch());

        BotParley.Active = BotParley.Running;
        _resolved = false;

        logger.Information(
            "Diplomacy is {State}: no war and no alliance without a meeting at the host's town; {Most} meetings between guilds and {Audiences} with towns at a time, an envoy offered for {Offer} min and walking up to {March} min, the host waited {Host} min and a witness {Witness} min, the duke's word {Judge}s after the hearing or by rule; the duke's word stands {Every} min ({Retry} after nothing); peace asked at {Wary:F0}, trade at {Cordial:F0}; agreements for {Pact:0.#}h, trade {Discount:P0} off. "
            + "Towns: {Towns} — an audience every {Audience} min per guild and town, tasks due in {TaskHours:0.#}h, prices ±{Step:P1} a task between ×{Least:F2} and ×{MostF:F2}, put out for {Exile:0}h after {Fails} failures in a row",
            BotParley.Running ? "on" : "off",
            BotParley.MostMeetings,
            BotParley.MostAudiences,
            BotParley.OfferMs / 60000,
            BotParley.MarchMs / 60000,
            BotParley.HostWaitMs / 60000,
            BotParley.WitnessWaitMs / 60000,
            BotParley.JudgeWaitMs / 1000,
            BotParley.MeetEveryMs / 60000,
            BotParley.RetryMs / 60000,
            BotRegard.Enmity * BotParley.WaryShare,
            BotParley.Cordial,
            BotPact.Hours,
            BotPact.TradeDiscount,
            BotBurgh.Running ? "on" : "off",
            BotBurgh.EveryMs / 60000,
            BotTownTask.Hours,
            BotBurgh.Step,
            BotBurgh.Least,
            BotBurgh.Most,
            BotBurgh.ExileHours,
            BotBurgh.FailsToExile
        );

        _timer?.Stop();
        _timer = new DiplomacyTimer(TimeSpan.FromMilliseconds(Math.Max(250, BotParley.BeatMs)));
        _timer.Start();
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotDiplomacySettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotDiplomacySettings());
            logger.Information("Wrote a starter diplomacy file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotParley.Running = settings.Running ?? BotParley.Running;
        BotBurgh.Running = settings.Towns ?? BotBurgh.Running;
        BotParley.MostMeetings = settings.MostMeetings ?? BotParley.MostMeetings;
        BotParley.MostAudiences = settings.MostAudiences ?? BotParley.MostAudiences;
        BotParley.OfferMs = Ms(settings.OfferMinutes, 60000) ?? BotParley.OfferMs;
        BotParley.MarchMs = Ms(settings.MarchMinutes, 60000) ?? BotParley.MarchMs;
        BotParley.HostWaitMs = Ms(settings.HostWaitMinutes, 60000) ?? BotParley.HostWaitMs;
        BotParley.WitnessWaitMs = Ms(settings.WitnessWaitMinutes, 60000) ?? BotParley.WitnessWaitMs;
        BotParley.JudgeWaitMs = Ms(settings.JudgeWaitSeconds, 1000) ?? BotParley.JudgeWaitMs;
        BotParley.LineMs = Ms(settings.LineSeconds, 1000) ?? BotParley.LineMs;
        BotParley.LongestMs = Ms(settings.LongestMinutes, 60000) ?? BotParley.LongestMs;
        BotParley.MeetEveryMs = Ms(settings.MeetEveryMinutes, 60000) ?? BotParley.MeetEveryMs;
        BotParley.RetryMs = Ms(settings.RetryMinutes, 60000) ?? BotParley.RetryMs;
        BotParley.ArriveTiles = settings.ArriveTiles ?? BotParley.ArriveTiles;
        BotParley.HostCallTiles = settings.HostCallTiles ?? BotParley.HostCallTiles;
        BotParley.WaryShare = settings.WaryShare ?? BotParley.WaryShare;
        BotParley.Cordial = settings.Cordial ?? BotParley.Cordial;
        BotParley.CallOdds = settings.CallOdds ?? BotParley.CallOdds;
        BotParley.SeatTownTiles = settings.SeatTownTiles ?? BotParley.SeatTownTiles;
        BotParley.MeetAtSeat = settings.MeetAtSeat ?? BotParley.MeetAtSeat;
        BotParley.JudgeModel = string.IsNullOrWhiteSpace(settings.JudgeModel) ? BotParley.JudgeModel : settings.JudgeModel.Trim();
        BotParley.JudgeKeepAlive = string.IsNullOrWhiteSpace(settings.JudgeKeepAlive) ? BotParley.JudgeKeepAlive : settings.JudgeKeepAlive.Trim();
        BotParley.JudgeTimeoutMs = settings.JudgeTimeoutMs ?? BotParley.JudgeTimeoutMs;

        if (settings.Halls != null)
        {
            BotParley.Halls = new Dictionary<string, int[]>(settings.Halls, StringComparer.OrdinalIgnoreCase);
        }

        BotPact.Hours = settings.PactHours ?? BotPact.Hours;
        BotPact.TradeDiscount = settings.TradeDiscount ?? BotPact.TradeDiscount;
        BotDuke.TradesForTerms = settings.TradesForTerms ?? BotDuke.TradesForTerms;
        BotEnvoy.Prior = settings.Prior ?? BotEnvoy.Prior;
        BotBurgh.EveryMs = Ms(settings.AudienceEveryMinutes, 60000) ?? BotBurgh.EveryMs;
        BotBurgh.LeastResidents = settings.LeastResidents ?? BotBurgh.LeastResidents;
        BotBurgh.Step = settings.PriceStep ?? BotBurgh.Step;
        BotBurgh.Least = settings.LeastFactor ?? BotBurgh.Least;
        BotBurgh.Most = settings.MostFactor ?? BotBurgh.Most;
        BotBurgh.FailsToExile = settings.FailsToExile ?? BotBurgh.FailsToExile;
        BotBurgh.ExileHours = settings.ExileHours ?? BotBurgh.ExileHours;
        BotBurgh.ExileMovesSeat = settings.ExileMovesSeat ?? BotBurgh.ExileMovesSeat;
        BotTownTask.Hours = settings.TaskHours ?? BotTownTask.Hours;
        BotTownTask.ClearGain = settings.ClearGain ?? BotTownTask.ClearGain;
        BotTownTask.ClearPresence = settings.ClearPresence ?? BotTownTask.ClearPresence;
        BotTownTask.ClearOdds = settings.ClearOdds ?? BotTownTask.ClearOdds;
        BotTownTask.GuardMinutes = settings.GuardMinutes ?? BotTownTask.GuardMinutes;
        BotTownTask.SupplyUnits = settings.SupplyUnits ?? BotTownTask.SupplyUnits;
        BotTownReport.Reach = settings.TownReach ?? BotTownReport.Reach;
    }

    private static int? Ms(double? value, int unit) => value == null ? null : (int)Math.Max(0, Math.Min(int.MaxValue, value.Value * unit));

    public override void Reset()
    {
        logger.Information("Diplomacy, before the reload: {What}", BotParley.Describe());

        _timer?.Stop();
        _timer = null;
        _resolved = false;

        BotParley.Active = false;
        BotParley.Forget();
        BotHerald.Forget();
        BotPact.Forget();
        BotBurgh.Forget();
        BotGrievances.Forget();
    }

    private static void Tick()
    {
        // The tasks read back off the save are given their towns once the towns are read. See BotBurgh.Resolve.
        if (!_resolved && BotTowns.Surveyed)
        {
            _resolved = true;
            BotBurgh.Resolve();
        }

        BotParley.Beat();
    }

    private sealed class DiplomacyTimer : Timer
    {
        public DiplomacyTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => Tick();
    }
}
