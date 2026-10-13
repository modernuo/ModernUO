using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Fighting for a living, as a module.
///
/// <para>
/// <see cref="BotPhase.World"/> and requires <c>Classes</c>, <c>Will</c> and <c>Auction</c> — the last
/// because what comes off a corpse goes onto the market before it goes to a counter.
/// </para>
///
/// <para>
/// <b>This is the module that gives the world money.</b> Before it, every coin on the shard was a coin that
/// already existed: bots are born with none, trade between them only moves it about, and a shopkeeper's
/// counter was a place where it left. So every piece of work that cost something to begin failed on its first
/// beat, and the only thing that could happen was digging, which is free. A monster's purse is where gold
/// comes from, and everything else — the crafter paid for a blade, the scribe paid for a scroll, the miner
/// paid for ore — is that same gold moving one step further from the field.
/// </para>
/// </summary>
public sealed class BotHuntModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHuntModule));

    public override string Name => "Hunt";

    public override BotPhase Phase => BotPhase.World;

    /// <summary>
    /// <c>Spells</c> joins the list because a fight now casts: the book a caster throws from is that
    /// subsystem's, and asking it anything before it has started is asking an empty shelf.
    /// </summary>
    public override string[] Requires => ["Classes", "Will", "Market", "Spells"];

    public override void Start()
    {
        BotHuntConfig.Load();

        // The cry, the stand and the stopwatch read Configuration/bot-combat.json (12.10.2026, w5). See BotCombatConfig.
        BotCombatConfig.Load();

        BotWill.Offer(new BotHunter());

        // Taming: a class that lists Animal Taming brings a beast to heel, and the beast fights beside it. See BotTame.
        BotWill.Offer(new BotTamerProposer());

        // Picking spent arrows back up. Priced low on purpose: it is what an archer does when there is
        // nothing better in front of it.
        BotWill.Offer(new BotGleaner());

        // The missing end of a fight nobody chose. See BotPickings: since self-defence became a reflex, a bot
        // kills things without an undertaking attached, and an undertaking is what used to empty the body.
        BotWill.Offer(new BotPicker());

        // Going through a chest standing out in the world — a camp's, a ruin's. Patrick's order of
        // 08.09.2026: an orc camp is worth clearing and the chest in it was worth nothing to a population
        // that had no notion of a container which is not a corpse, a stall or its own pack. See BotPlunder.
        BotWill.Offer(new BotPlunderer());

        // Walking a prisoner out of a camp and home. Patrick's order of 08.09.2026. Offered to anybody: a
        // prisoner does not care what trade its escort practises. See BotFreedom.
        BotWill.Offer(new BotLiberator());

        // Calling a company against what one bot must refuse. This is the caller the squad subsystem has been
        // waiting for since it was written — see BotMuster.
        BotWill.Offer(new BotMuster());

        // Going to somebody's aid, and hitting back at whatever is on you. The second of these is the first
        // thing ever offered on the Hunted rung, which BotWill has been complaining is unserved since it was
        // written — a bot with something chewing on it simply held whatever it was doing.
        BotWill.Offer(new BotRescuer());
        BotWill.Offer(new BotDefender());

        // And the stand on the failing rung: the fight carried on by a bot being killed with no flight on offer (06.10.2026).
        BotWill.Offer(new BotDefender(last: true));

        // The clock that turns a murderer's hour and a cell's sentence, and rolls the fighters for the rarest thought on
        // the shard — which is pressed on them by BotRobber, not offered through the auction. Patrick's order of
        // 15.09.2026; see BotOutlaw.
        BotOutlaw.Start();

        logger.Information(
            // <b>One name per hole, and using {Reach} twice made this line report 8 where the shard runs
            // 50.</b> Serilog binds by name, not by position: two tokens with the same name take the same
            // value, the last one written wins, and the argument meant for the token further along is left
            // over with nothing to fill. The result rendered as a sentence that read perfectly and was wrong
            // about the single number the whole hunt is built on — in the one place this project treats as
            // the source of truth about what the shard is actually running.
            "The hunt is on: quarry looked for {Reach} tiles out and taken up to ×{Daring} of our own power, anything inside {Notice} tiles worth dropping other work for, set out above {Fit:P0} health and given up below {Flee:P0} or when outnumbered; ground to look over is picked beyond {Beyond} tiles and walked to within {Arrive}",
            BotQuarry.Reach,
            BotQuarry.Daring,
            BotQuarry.Notice,
            BotHunter.FitAt,
            BotSlay.FleeAt,
            BotQuarry.Reach,
            BotProwl.ArriveWithin
        );

        logger.Information(
            "One bot leaves a fight it is not winning: {NoProgress}ms without the quarry's health falling, and {Cap}ms all told from taking it on",
            BotSlay.NoProgressMs,
            BotSlay.CapMs
        );

        logger.Information(
            "A cry for help carries {Carries} tiles and stands {Holds}ms; anybody above {Fit:P0} health within {Reach} tiles may answer, and a bot hits back at whatever is on it inside {Near} tiles; a dire cry (under {Dire:P0}) summons up to {Most} at once{Summons}, a steady one its nearest free neighbour within {NearReach} tiles when the thing is {NearOdds:P0} of its own strength or more{NearCalls}; a bot refused the fight for the odds with no flight on offer takes the stand{Stands}; one unanswered cry in {Sample} is said with who was in earshot",
            BotCry.Carries,
            BotCry.HoldsMs,
            BotRescuer.FitAt,
            BotRescuer.Reach,
            BotDefender.Reach,
            BotCry.DireAt,
            BotCry.MostCalled,
            BotCry.Summons ? "" : " (off)",
            BotCry.NearReach,
            BotCry.NearOdds,
            BotCry.NearCalls ? "" : " (off)",
            BotDefender.Stands ? "" : " (off)",
            BotReflex.SampleEvery
        );

        logger.Information(
            "Companies may be called: anything beyond one bot but within ×{Tolerance} of everybody inside {Reach} tiles, when at least {Least} others are free",
            BotThreat.Tolerance,
            BotMuster.Reach,
            BotMuster.Least
        );

        // Every number that decides whether a chest is opened, because a rule that silently refuses is a
        // rule nobody can argue with: three of the four gates below are admissions that this population has
        // no Lockpicking and no Remove Trap, and the day it has either, these lines are where that shows.
        logger.Information(
            "Chests are worth going through: looked for {Reach} tiles out once a minute, reached into from {Touch} tiles, broken off when something living is within {Danger}; locked ones, trapped ones and any standing inside a town are passed over, and an emptied one is left alone {Emptied}ms",
            BotPlunder.Reach,
            BotPlunder.Touch,
            BotPlunder.Danger,
            BotPlunder.EmptiedMs
        );

        logger.Information(
            "Prisoners are worth freeing: heard {Reach} tiles out once a minute, accepted from {Touch} tiles, and walked to {Town} whatever town the engine picked for them — passed over only when the cage is further than {Roam} tiles from it; the escort turns back for a prisoner more than {Lag} tiles behind until it is within {Rejoin}, and gives up on one that gets no nearer home in {Stuck}ms; the engine pays around {Reward}gp, and refuses anybody who escorted somebody else inside five minutes",
            BotFreedom.Reach,
            BotFreedom.Touch,
            BotFreedom.Town,
            BotFreedom.Roam,
            BotFreedom.Lag,
            BotFreedom.Rejoin,
            BotFreedom.StuckMs,
            BotFreedom.Reward
        );
    }

    /// <summary>What the population has learned about who is worth fighting.</summary>
    public static string Summarise() =>
        $"{BotQuarry.Describe()}; {BotPlunder.Describe()}; {BotFreedom.Describe()}";

    public override void Reset()
    {
        BotHunter.Forget();
        BotMuster.Forget();
        BotBand.Forget();
        BotRescuer.Forget();
        BotOutlaw.Stop();
        BotOutlaw.Forget();
        BotRobber.Forget();
        BotManhunt.Forget();
        BotLawful.Forget();
        BotRaid.Forget();
        BotInquest.Forget();
        BotBrawl.Forget();
        BotDuel.Forget();
        BotArms.Forget();
        BotSlay.ForgetBows();

        // Cries name bots of a population that is being replaced.
        BotCry.Forget();

        // Claims name creatures of the world being replaced.
        BotQuarry.Forget();

        // And chests are items of it.
        BotPlunder.Forget();
        BotPlunderer.Reset();

        // And prisoners are mobiles of it.
        BotFreedom.Forget();
        BotLiberator.Reset();
    }
}
