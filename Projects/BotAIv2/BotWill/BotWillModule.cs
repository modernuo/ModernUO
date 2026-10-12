using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Deciding as a module: reads its numbers, turns itself on, and says what the population will be judging
/// work by.
///
/// <para>
/// <see cref="BotPhase.World"/>, and not because starting needs the map — it does not. It is because only
/// the world phase is rewound when the world is replaced, and what this module accumulates is a count of
/// what a <em>population</em> is doing. A Settings-phase module's <see cref="Reset"/> is never called, so
/// the census would carry the undertakings of bots that no longer exist into the next world, and a count
/// that is never released does not look wrong: it looks like a busier shard.
/// </para>
///
/// <para>
/// Requires <c>Classes</c> — what a bot is for is a fact about its class, and an appraisal made against
/// nine classes that failed to load would be an appraisal of nothing.
/// </para>
///
/// <para>
/// Its switch earns its keep more than any other in this assembly. Turned off, everything else still runs —
/// squads form up, journeys walk, combat answers when hit — and nothing chooses anything, which separates
/// "the brain is wrong" from "the brain is covering for something else". In the first version those two
/// questions had the same symptom and the answer took an evening of watching a live shard.
/// </para>
/// </summary>
public sealed class BotWillModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotWillModule));

    public override string Name => "Will";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Classes"];

    public override void Start()
    {
        BotWillConfig.Load();

        BotWill.Deciding = true;

        // Every number in this line is a claim about what the population will do all night. The exchange rate
        // comes first because it decides whether this shard is full of miners or full of duellists, and it is
        // the cheapest possible place to notice that it was set to something absurd.
        logger.Information(
            "Will ready: a point of skill is worth {Gold} gold, dying costs {Death} minutes; work is reviewed every {Review}ms, held for what it reckons it needs between {Dwell}ms and {DwellCap}ms, and replaced only above ×{Margin} against ×{Inertia} for the work in hand; steadfast work is held for ×{Stretch} its own reckoning up to {CommitCap}ms unless its walk stops closing for {Trouble} of the watchdog, and is taken up again after what would not wait: {Resume} (not below {ResumeHealth} health); a bot's own trade is worth ×{Own} and another class's ×{Other}; a crowd bites {Crowd}, repetition {Repeat}",
            BotYield.GoldPerSkillPoint,
            BotYield.DeathMinutes,
            BotWill.ReviewMs,
            BotWill.DwellMs,
            BotWill.DwellCapMs,
            BotWill.SwitchMargin,
            BotAppraisal.Inertia,
            BotWill.CommitStretch,
            BotWill.CommitCapMs,
            BotWill.TroubleShare,
            BotWill.Resume,
            BotWill.ResumeHealth,
            BotCalling.OwnTrade,
            BotCalling.OtherTrade,
            BotAppraisal.CrowdBite,
            BotAppraisal.RepetitionBite
        );
    }

    /// <summary>
    /// A world reload is a different world. What the old population was doing is a count of undertakings held
    /// by bots that no longer exist, and a count that is never released does not look wrong — it looks like a
    /// busier shard.
    /// </summary>
    public override void Reset()
    {
        logger.Information("Will, before the reload: {State}", BotWill.Describe());

        BotWill.Deciding = false;
        BotWill.Reset();
    }

    public static string Summarise() => BotWill.Describe();
}
