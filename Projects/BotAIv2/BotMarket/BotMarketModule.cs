using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The market as a module: reads its numbers, starts the wants' beat and the city's clock, and offers the carrier's walk.
///
/// <para>
/// <see cref="BotPhase.World"/> and requires only the will, whose proposers it adds to. Registered where the auction was, before
/// the population, so a reload clears the wants while their buyers still exist.
/// </para>
/// </summary>
public sealed class BotMarketModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotMarketModule));

    public override string Name => "Market";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Will"];

    public override void Start()
    {
        BotMarketConfig.Load();

        BotMarket.Start();

        // The city's treasury: the mint, the bounties, and its standing orders — served now at the bots' shops by Britain's
        // bank, where the keepers stand, instead of off stalls. See BotCity.
        BotCity.Start();

        BotWill.Offer(new BotCourier());

        // The buyer's half of hand to hand: it comes to meet the carrier (02.10.2026, Patrick's order). See BotRendezvous.
        BotWill.Offer(new BotMeeter());

        logger.Information(
            "Meetings {State}: a carrier within {Call} tiles of its buyer calls the meeting; the buyer walks to meet it, or both to the nearest guarded ground within {Safe} tiles when either stands in a hostile or deadly patrol or on a square asking more than it brings; the buyer gives the meeting {Meet} minutes and stands {Wait}s beside the carrier for the hand-over; a buyer that walks on about its work stands where it is for a carrier within {Stand} tiles of it, {StandMs}ms a beat and at most {StandMost}s a walk, and a carrier that comes no nearer to its buyer in {Losing}s of walking after it lets it go",
            BotRendezvous.Running ? "on" : "off",
            BotRendezvous.CallTiles,
            BotRendezvous.SafeReach,
            BotRendezvous.MeetMs / 60000,
            BotRendezvous.WaitMs / 1000,
            BotRendezvous.StandTiles,
            BotRendezvous.StandMs,
            BotRendezvous.StandMostMs / 1000,
            BotDeliver.LosingMs / 1000
        );

        logger.Information(
            "The market is open by the bank in {Town}, hand to hand: wants are said aloud and hold no gold; a want unanswered for {Raise}ms asks {Step:P0} louder up to ×{Most} of its opening, if its buyer could pay the louder price, and gives up there; a carrier holding what is wanted walks up to {Reach} tiles to the buyer for anything worth {Least}gp or more and hands it over within {Meet} tiles; nothing changes hands between two bots that are not standing together",
            BotCapital.Name,
            BotMarket.RaiseMs,
            BotMarket.RaiseStep,
            BotMarket.MostMultiple,
            BotCourier.Reach,
            BotCourier.LeastGold,
            BotHandover.MeetTiles
        );

        // The reservation, the dead buyer's want, and the lines' clocks (02.10.2026, build 391). See BotMarket.Reserved and BotLines.
        logger.Information(
            "Wants reserve their buyer's money: a want is said only if what the buyer's other wants hold leaves enough for it, and everything else a bot spends is read off what is left; a dead buyer keeps its wants, asked again {Grace} minutes after it rises; a raise the buyer cannot reserve leaves the want at its price for up to {Age} minutes; a line is met if every unit arrives within {Window} minutes, and taken back or unpaid inside its first {Kept} minutes it is kept out; the board is {Restored}",
            BotMarket.RisenGraceMs / 60000,
            BotMarket.MostAgeMs / 60000,
            BotLines.WindowMs / 60000,
            BotLines.GraceMs / 60000,
            BotMarket.RestoredWants > 0 ? $"read back from the save with {BotMarket.RestoredWants} wants" : "new"
        );
    }

    /// <summary>A world reload is a different world: every want goes with the buyers that said it.</summary>
    public override void Reset()
    {
        logger.Information("The market, before the reload: {State}", BotMarket.Describe());

        BotMarket.Reset();
        BotCity.Stop();
        BotCity.Forget();
    }
}
