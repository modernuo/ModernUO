using System;
using System.IO;
using Server.Json;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>The numbers of <c>Configuration/bot-crime.json</c>. A missing key keeps the code's value; keys are PascalCase.</summary>
public sealed class BotCrimeSettings
{
    public bool? Running { get; set; }

    public int? ThiefPerBots { get; set; }

    public int? PoorBelow { get; set; }

    public double? RecruitEveryMinutes { get; set; }

    public int? ReportsToPost { get; set; }

    public int? BountyBase { get; set; }

    public int? BountyMost { get; set; }

    public double? PostedHours { get; set; }

    public double? TermMinutes { get; set; }

    public int? MostDoublings { get; set; }

    public bool? Ransoms { get; set; }

    public int? RansomBase { get; set; }

    public int? RansomPerMinute { get; set; }

    public double? RepentHours { get; set; }

    public bool? Yields { get; set; }

    public double? YieldShare { get; set; }

    public bool? Cutpurses { get; set; }

    public int? StealsPerHour { get; set; }

    public double? ThiefEveryMinutes { get; set; }

    public bool? Ambushes { get; set; }

    public double? AmbushEveryMinutes { get; set; }

    public int? LeastCargo { get; set; }

    public int? MostParty { get; set; }

    public bool? Fences { get; set; }

    public double? FenceShare { get; set; }

    public int? TracesToPost { get; set; }

    /// <summary>life18a: the share of the road's carriers worth an ambush (the bar follows their cargo), and the bar's floor.</summary>
    public double? CargoShare { get; set; }

    public int? CargoFloor { get; set; }

    public bool? Bounties { get; set; }

    public double? BountyOdds { get; set; }

    public int? HuntersPer { get; set; }
}

/// <summary>
/// The underworld's trades and the town's law, as a module: reads its numbers, offers the four pieces of work — the cutpurse's
/// turn in a crowd, a bandit's place in an ambush, the fence's trade and the bounty — and says at boot what it will do. Its
/// clock is the outlaws' own (<c>BotOutlaw</c>'s, once a second), and its counters are written at the end of the
/// <c>Underworld:</c> line. See <see cref="BotCrime"/>.
/// </summary>
public sealed class BotCrimeModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCrimeModule));

    private const string ConfigPath = "Configuration/bot-crime.json";

    public override string Name => "Crime";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Population", "Will", "Hunt", "Market"];

    public override void Start()
    {
        Load();

        BotWill.Offer(new BotCutpurse());
        BotWill.Offer(new BotHighwayman());
        BotWill.Offer(new BotFencer());
        BotWill.Offer(new BotBountyBoard());

        logger.Information(
            "Crime is {State}: thieves {Thieves} (one per {Per} bots, {PerHour} tries an hour across the island, each thief every {Every} minutes, recruits under {Poor}gp); ambushes {Ambush} (one party of {Least} to {Most} at a time, every {AmbushEvery} minutes, cargo of {Cargo}gp or more until the roads are measured, then the top {CargoShare:P0} of what carriers carry and never under {CargoFloor}gp, laid on the carrier's own road ahead of it within {MostAhead} tiles, never in a guarded town, the band {Muster}); the fence {Fence} at {Share:P0}, {Traces} traces post a seller; {Reports} reports post a name for {Posted} hours, a bounty of {Base}gp and the damage up to {MostBounty}gp out of the town's purse, hunted {Bounties} by fighters {Odds} times as strong, {Hunters} to a head; a term of {Term} minutes doubling for a repeat up to {Doublings} times, ransoms {Ransoms} at {RansomBase}gp and {PerMinute}gp a minute left, repentant after {Repent} hours clean; a wanted thief yields at {Yield:P0}: {Yields}",
            BotCrime.Running ? "on" : "off",
            BotCutpurse.Running ? "on" : "off",
            BotCrime.ThiefPerBots,
            BotCutpurse.PerHour,
            BotCutpurse.EveryMs / 60000,
            BotCrime.PoorBelow,
            BotHighway.Running ? "on" : "off",
            BotHighway.LeastParty,
            BotHighway.MostParty,
            BotHighway.EveryMs / 60000,
            BotHighway.LeastCargo,
            BotHighway.CargoShare,
            BotHighway.CargoFloor,
            BotHighway.MostAhead,
            BotHighway.Press ? $"pressed in as a company's call presses (life19), struck only {BotHighway.LeastParty} together" : "offered the ambush at auction",
            BotFencer.Running ? "on" : "off",
            BotFencer.FenceShare,
            BotFencer.TracesToPost,
            BotCrime.ReportsToPost,
            BotCrime.PostedMs / 3600000.0,
            BotCrime.BountyBase,
            BotCrime.BountyMost,
            BotBountyBoard.Running ? "on" : "off",
            BotBountyBoard.Odds,
            BotBountyBoard.HuntersPer,
            BotCrime.TermMs / 60000,
            BotCrime.MostDoublings,
            BotCrime.Ransoms ? "on" : "off",
            BotCrime.RansomBase,
            BotCrime.RansomPerMinute,
            BotCrime.RepentMs / 3600000.0,
            BotCrime.YieldShare,
            BotCrime.Yields ? "on" : "off"
        );
    }

    private static void Load()
    {
        var path = Path.Combine(Core.BaseDirectory, ConfigPath);
        var s = JsonConfig.Deserialize<BotCrimeSettings>(path);

        if (s == null)
        {
            JsonConfig.Serialize(path, new BotCrimeSettings());
            logger.Information("Wrote a starter crime file to {Path}; every number stays as the code has it", ConfigPath);

            return;
        }

        BotCrime.Running = s.Running ?? BotCrime.Running;
        BotCrime.ThiefPerBots = s.ThiefPerBots ?? BotCrime.ThiefPerBots;
        BotCrime.PoorBelow = s.PoorBelow ?? BotCrime.PoorBelow;
        BotCrime.RecruitEveryMs = s.RecruitEveryMinutes is { } recruit ? (int)(recruit * 60000) : BotCrime.RecruitEveryMs;
        BotCrime.ReportsToPost = s.ReportsToPost ?? BotCrime.ReportsToPost;
        BotCrime.BountyBase = s.BountyBase ?? BotCrime.BountyBase;
        BotCrime.BountyMost = s.BountyMost ?? BotCrime.BountyMost;
        BotCrime.PostedMs = s.PostedHours is { } posted ? (int)(posted * 3600000) : BotCrime.PostedMs;
        BotCrime.TermMs = s.TermMinutes is { } term ? (int)(term * 60000) : BotCrime.TermMs;
        BotCrime.MostDoublings = s.MostDoublings ?? BotCrime.MostDoublings;
        BotCrime.Ransoms = s.Ransoms ?? BotCrime.Ransoms;
        BotCrime.RansomBase = s.RansomBase ?? BotCrime.RansomBase;
        BotCrime.RansomPerMinute = s.RansomPerMinute ?? BotCrime.RansomPerMinute;
        BotCrime.RepentMs = s.RepentHours is { } repent ? (int)(repent * 3600000) : BotCrime.RepentMs;
        BotCrime.Yields = s.Yields ?? BotCrime.Yields;
        BotCrime.YieldShare = s.YieldShare ?? BotCrime.YieldShare;
        BotCutpurse.Running = s.Cutpurses ?? BotCutpurse.Running;
        BotCutpurse.PerHour = s.StealsPerHour ?? BotCutpurse.PerHour;
        BotCutpurse.EveryMs = s.ThiefEveryMinutes is { } every ? (int)(every * 60000) : BotCutpurse.EveryMs;
        BotHighway.Running = s.Ambushes ?? BotHighway.Running;
        BotHighway.EveryMs = s.AmbushEveryMinutes is { } ambush ? (int)(ambush * 60000) : BotHighway.EveryMs;
        BotHighway.LeastCargo = s.LeastCargo ?? BotHighway.LeastCargo;
        BotHighway.CargoShare = s.CargoShare ?? BotHighway.CargoShare;
        BotHighway.CargoFloor = s.CargoFloor ?? BotHighway.CargoFloor;
        BotHighway.MostParty = s.MostParty ?? BotHighway.MostParty;
        BotFencer.Running = s.Fences ?? BotFencer.Running;
        BotFencer.FenceShare = s.FenceShare ?? BotFencer.FenceShare;
        BotFencer.TracesToPost = s.TracesToPost ?? BotFencer.TracesToPost;
        BotBountyBoard.Running = s.Bounties ?? BotBountyBoard.Running;
        BotBountyBoard.Odds = s.BountyOdds ?? BotBountyBoard.Odds;
        BotBountyBoard.HuntersPer = s.HuntersPer ?? BotBountyBoard.HuntersPer;
    }

    public override void Reset()
    {
        logger.Information("Crime, before the reload: {Crime}", BotCrime.Describe());
        BotCrime.Forget();
    }
}
