using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>The numbers of <c>Configuration/bot-camp.json</c>. A missing key keeps the code's value; keys are PascalCase.</summary>
public sealed class BotCampSettings
{
    /// <summary>Whether bots light fires, join them and meet on the road at all. See BotCamp.Running.</summary>
    public bool? Running { get; set; }

    /// <summary>The share of the population that lights fires. See BotCamp.LighterShare.</summary>
    public double? LighterShare { get; set; }

    /// <summary>Minutes one bot waits between camps. See BotCamper.EveryMs.</summary>
    public double? CampEveryMinutes { get; set; }

    /// <summary>Minutes one bot waits between fires it joins. See BotBeckon.EveryMs.</summary>
    public double? JoinEveryMinutes { get; set; }

    /// <summary>How far a free bot may be from a fire and be asked to it. See BotCamp.JoinReach.</summary>
    public int? JoinReach { get; set; }

    /// <summary>The most fires at once. See BotCamp.MostFires.</summary>
    public int? MostFires { get; set; }

    /// <summary>The most seats round a fire. See BotCamp.MostAtFire.</summary>
    public int? MostAtFire { get; set; }

    /// <summary>Minutes a keeper keeps its fire. See BotCamp.SitMinutes.</summary>
    public double? SitMinutes { get; set; }

    /// <summary>Minutes a guest sits. See BotFireside.GuestMinutes.</summary>
    public double? GuestMinutes { get; set; }

    /// <summary>What making camp is worth a minute. See BotKindle.Prior.</summary>
    public double? CampPrior { get; set; }

    /// <summary>What a seat at a fire is worth a minute. See BotFireside.Prior.</summary>
    public double? JoinPrior { get; set; }

    /// <summary>The share of the inn's rate an hour by a fire gives back. See BotCamp.RestShare.</summary>
    public double? RestShare { get; set; }

    /// <summary>The share of a night by a fire that comes back as regeneration. See BotCamp.FireBuffShare.</summary>
    public double? FireBuffShare { get; set; }

    /// <summary>Whether a tired bot may leave the world by a fire. See BotCamp.SleepByFire.</summary>
    public bool? SleepByFire { get; set; }

    /// <summary>Whether bots stop to talk on the road. See BotMeetings.Running.</summary>
    public bool? Meetings { get; set; }

    /// <summary>The chance two passing bots stop. See BotMeetings.Chance.</summary>
    public double? MeetChance { get; set; }

    /// <summary>How near two bots must pass. See BotMeetings.Reach.</summary>
    public int? MeetReach { get; set; }

    /// <summary>Seconds between looks at one bot for a meeting. See BotMeetings.EveryMs.</summary>
    public double? MeetEverySeconds { get; set; }

    /// <summary>Minutes a bot goes after a word on the road before another. See BotMeetings.RestMs.</summary>
    public double? MeetRestMinutes { get; set; }

    /// <summary>Whether bots stop to talk inside towns too. See BotMeetings.InTowns.</summary>
    public bool? MeetInTowns { get; set; }

    /// <summary>Seconds between lines round a fire. See BotCampTalk.LineMs.</summary>
    public double? LineSeconds { get; set; }

    /// <summary>The most lines one bot says at one fire. See BotCampTalk.MostLinesEach.</summary>
    public int? MostLinesEach { get; set; }
}

/// <summary>
/// Fires in the woods, company round them, and a word on the road, as a module: reads its numbers, puts its words into the
/// voice's bank, offers the camp and the seat to the auction, and runs the camp's clock — the fires fed and talked at, and
/// a slice of the population looked at for meetings — once a second. Patrick's point 6 of 29.09.2026. See <see cref="BotCamp"/>.
/// </summary>
public sealed class BotCampModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCampModule));

    private const string ConfigPath = "Configuration/bot-camp.json";

    /// <summary>The camp's clock. A second: a fire's flame is checked, fed and talked at on it, and a slice of the meetings.</summary>
    public static int TickMs { get; set; } = 1000;

    private static CampTimer _timer;

    public override string Name => "Camp";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will", "Movement"];

    public override void Start()
    {
        Load();

        var words = BotCampTalk.Phrases();

        if (BotCamp.Running)
        {
            BotWill.Offer(new BotCamper());
            BotWill.Offer(new BotBeckon());
        }

        _timer?.Stop();
        _timer = new CampTimer(TimeSpan.FromMilliseconds(Math.Max(100, TickMs)));
        _timer.Start();

        logger.Information(
            "Camp is {State}: {Share:P0} of the bots light fires in the wild when tired, bored or idle (once in {Every} min, at {CampPrior}/min and more as they tire), a free bot within {Reach} tiles is asked to a seat (at {JoinPrior}/min, once in {JoinEvery} min), "
            + "at most {Fires} fires of {Seats} seats, kept {Sit} min; a minute by a fire gives back play at {RestShare:P0} of the inn's rate, a tired bot secure at one sleeps there (buff {Buff:P0} of the rest); "
            + "meetings {Meet}: {Chance:P0} of two free bots passing within {MeetReach} tiles in the open stop to talk; {Words} occasions added to the voice's bank",
            BotCamp.Running ? "on" : "off",
            BotCamp.LighterShare,
            BotCamper.EveryMs / 60000,
            BotKindle.Prior,
            BotCamp.JoinReach,
            BotFireside.Prior,
            BotBeckon.EveryMs / 60000,
            BotCamp.MostFires,
            BotCamp.MostAtFire,
            BotCamp.SitMinutes,
            BotCamp.RestShare,
            BotCamp.FireBuffShare,
            BotMeetings.Running ? "on" : "off",
            BotMeetings.Chance,
            BotMeetings.Reach,
            words
        );
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var settings = JsonConfig.Deserialize<BotCampSettings>(path);

        if (settings == null)
        {
            JsonConfig.Serialize(path, new BotCampSettings());
            logger.Information("Wrote a starter camp file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotCamp.Running = settings.Running ?? BotCamp.Running;
        BotCamp.LighterShare = settings.LighterShare ?? BotCamp.LighterShare;
        BotCamper.EveryMs = settings.CampEveryMinutes is { } camp ? (int)(camp * 60000) : BotCamper.EveryMs;
        BotBeckon.EveryMs = settings.JoinEveryMinutes is { } join ? (int)(join * 60000) : BotBeckon.EveryMs;
        BotCamp.JoinReach = settings.JoinReach ?? BotCamp.JoinReach;
        BotCamp.MostFires = settings.MostFires ?? BotCamp.MostFires;
        BotCamp.MostAtFire = settings.MostAtFire ?? BotCamp.MostAtFire;
        BotCamp.SitMinutes = settings.SitMinutes ?? BotCamp.SitMinutes;
        BotFireside.GuestMinutes = settings.GuestMinutes ?? BotFireside.GuestMinutes;
        BotKindle.Prior = settings.CampPrior ?? BotKindle.Prior;
        BotFireside.Prior = settings.JoinPrior ?? BotFireside.Prior;
        BotCamp.RestShare = settings.RestShare ?? BotCamp.RestShare;
        BotCamp.FireBuffShare = settings.FireBuffShare ?? BotCamp.FireBuffShare;
        BotCamp.SleepByFire = settings.SleepByFire ?? BotCamp.SleepByFire;
        BotMeetings.Running = settings.Meetings ?? BotMeetings.Running;
        BotMeetings.Chance = settings.MeetChance ?? BotMeetings.Chance;
        BotMeetings.Reach = settings.MeetReach ?? BotMeetings.Reach;
        BotMeetings.EveryMs = settings.MeetEverySeconds is { } every ? (int)(every * 1000) : BotMeetings.EveryMs;
        BotMeetings.RestMs = settings.MeetRestMinutes is { } rest ? (int)(rest * 60000) : BotMeetings.RestMs;
        BotMeetings.InTowns = settings.MeetInTowns ?? BotMeetings.InTowns;
        BotCampTalk.LineMs = settings.LineSeconds is { } line ? (int)(line * 1000) : BotCampTalk.LineMs;
        BotCampTalk.MostLinesEach = settings.MostLinesEach ?? BotCampTalk.MostLinesEach;
    }

    public override void Reset()
    {
        logger.Information("Camp, before the reload: {Camp}", BotCamp.Describe());

        _timer?.Stop();
        _timer = null;

        BotCamp.Forget();
    }

    private sealed class CampTimer : Timer
    {
        private readonly int _ms;

        public CampTimer(TimeSpan interval) : base(interval, interval) => _ms = (int)interval.TotalMilliseconds;

        protected override void OnTick()
        {
            if (!BotCamp.Running)
            {
                return;
            }

            var now = Core.TickCount;

            BotCamp.Tick(now);
            BotMeetings.Tick(now, _ms);
        }
    }
}
