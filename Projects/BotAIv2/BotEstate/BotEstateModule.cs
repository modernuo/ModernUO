using System;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// The guilds' halls, as a module.
///
/// <para>
/// <see cref="BotPhase.World"/> and after <c>Population</c>, and that order is not a preference: the first
/// thing this module does is hand every hall in the world back to the guild that owns it, and a guild has no
/// leader until the population has been built and enrolled. Started earlier it would adopt nothing, every
/// hall on the island would stay ownerless, and this era condemns an ownerless house.
/// </para>
/// </summary>
public sealed class BotEstateModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotEstateModule));

    public override string Name => "Estate";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Classes", "Will", "Population"];

    public override void Start()
    {
        BotEstateConfig.Load();
        BotTownGround.Load();

        // The world first: whatever was raised on a previous evening is standing out there right now with a
        // deleted owner, and every minute before this runs is a minute it is decaying.
        BotEstate.Adopt();

        // And whatever wars the engine still holds from last night, which have no score and no clock
        // here: ended, so the session starts at peace. See BotWar.Reconcile.
        BotWar.Reconcile();

        BotWill.Offer(new BotSteward());

        // The guild's own company for the guild's own dire ground. See BotReeve: Patrick's fourth Quad idea of
        // 16.09.2026.
        BotWill.Offer(new BotReeve());

        // The second half of an estate: a hall is bought once, and then fitted out one bench at a time for
        // as long as the guild can afford it. See BotFitter.
        BotWill.Offer(new BotFitter());

        // And the third: a shopkeeper of the guild's own, once the workshop is finished. See BotHirer.
        BotWill.Offer(new BotHirer());

        // And the fourth, which is what makes the third worth 1,252gp: something on the shopkeeper's shelf.
        // See BotSupplier.
        BotWill.Offer(new BotSupplier());

        // And the yard round the hall, which is what a hall makes of the ground. See BotLand and BotBailiff:
        // the claim is a preference and the eviction it makes possible is deliberately almost nothing.
        BotWill.Offer(new BotBailiff());

        // The toll's patrol: a fighter of a patrolling guild walks to a stranger hunting its land and tells it the hunting is
        // tolled. See BotToll and BotTollman, Patrick's order of 26.09.2026.
        BotWill.Offer(new BotTollman());

        // The bailiff's second half, and it only ever fires where a war has been declared. See BotFeud.
        BotWill.Offer(new BotFeuder());

        // What a lost war costs: the beaten guild's hall goes outside the winner's yard. See BotExile.
        BotWill.Offer(new BotRemover());

        // A guild that has filled its hall moves into a bigger one. See BotHallKind and BotEnlarger (26.09.2026).
        BotWill.Offer(new BotEnlarger());

        // Houses of the population's own: bought by a bot that has done well, gone home to when bored. See BotAbode —
        // Patrick's order of 26.09.2026. The houses already standing are found by their signs first.
        BotAbode.Adopt();
        BotWill.Offer(new BotAbodeBuyer());
        BotWill.Offer(new BotReposer());

        // A guild's second house on its far land: a place to rise, and a yard for the toll. See BotOutpost (26.09.2026).
        BotOutpost.Adopt();
        BotWill.Offer(new BotOutposter());

        // Claiming squares of the island, and standing in one until it is yours. See BotClaim.
        BotWill.Offer(new BotHolder());

        logger.Information(
            "Estate ready: a guild raises a hall at {Price}gp levied off its chest and then its members, each keeping {Keep}gp back, up to {Max} on the island; plots are looked for between {Near} and {Far} tiles of home, {Apart} apart, {Budget} put to the engine at a time; {Dues}",
            BotEstate.Price,
            BotEstate.Keep,
            BotEstate.MaxHalls,
            BotPlot.Near,
            BotPlot.Far,
            BotPlot.Apart,
            BotPlot.Budget,
            BotDues.Running
                ? $"members pay {BotDues.Share:P0} of what their work brings in into their guild's chest, up to {BotDues.Ceiling}gp a chest, and a chest {(BotDues.Saving ? "keeps the price of the hall (or of a house in Britain) back from everyday draws" : "keeps nothing back")}"
                : "no dues are paid"
        );

        logger.Information(
            "Halls found in the world: {Adopted} taken back and handed to their guilds; fittings {Fittings}, and a guild's benches are its own — nobody outside it is offered one",
            BotEstate.Adopted,
            BotFittings.Running ? "on" : "off"
        );

        logger.Information(
            "Land and standing: a hall claims {Reach} tiles, worth x{Home} to its own and x{Abroad} to anybody else; guilds fall out below {Enmity} and make peace above {Amity}, and war is {War}",
            BotLand.Reach,
            BotLand.Home,
            BotLand.Abroad,
            BotRegard.Enmity,
            BotRegard.Amity,
            BotRegard.Warring ? "ON — bots of guilds at war may strike and loot each other" : "off, so a bad enough quarrel is counted and nothing more"
        );

        logger.Information(
            "Wars: won at {Kills} dead or {Loot}gp of plunder, no peace before {Least} minutes, judged at {Longest}, a truce of {Truce} after, one declaration a guild per {Every} minutes and {Most} at a time; an enemy on a guild's own ground is answered from {Defend} tiles at {DefendPrior}/min; opinions forget {Mend} a drift",
            BotWar.Kills,
            BotWar.Loot,
            BotWar.LeastMs / 60000,
            BotWar.LongestMs / 60000,
            BotWar.TruceMs / 60000,
            BotWar.DeclareEveryMs / 60000,
            BotWar.MostWars,
            BotFeud.Defend,
            BotQuarrel.DefendPrior,
            BotRegard.Mend
        );

        logger.Information(
            "Seats: {Seats}; a hall is at home within {Settled} tiles of its seat, members are born and rise beside their hall, and a hall further off is carried to the seat",
            BotSeat.Tell(),
            BotSeat.Settled
        );

        _furnishTimer?.Stop();
        _furnishTimer = new FurnishTimer(TimeSpan.FromSeconds(60));
        _furnishTimer.Start();
    }

    public override void Reset()
    {
        BotEstate.Forget();
        BotPlot.Forget();
        BotSteward.Forget();
        BotFitter.Forget();
        BotHirer.Forget();
        BotSupplier.Forget();
        BotOffice.Forget();
        BotLand.Forget();
        BotBounds.Forget();
        BotRegard.Forget();
        BotBailiff.Forget();
        BotFeuder.Forget();
        BotQuarrel.Forget();
        BotFeud.Forget();
        BotExile.Forget();
        BotWar.Forget();
        BotSeat.Forget();
        BotClaim.Forget();
        BotTownGround.Forget();
        BotHolder.Forget();
        BotReeve.Forget();
        BotChest.Forget();
        BotDues.Forget();
        BotToll.Forget();
        BotTollman.Forget();
        BotHold.Forget();
        BotRemover.Forget();
        BotRemove.Forget();
        BotEnlarger.Forget();
        BotAbode.Forget();
        BotAbodeBuyer.Forget();
        BotReposer.Forget();
        BotOutpost.Forget();
        BotOutposter.Forget();
        BotShelf.Forget();
        BotFittings.Forget();
        BotFurnish.Forget();

        _furnishTimer?.Stop();
        _furnishTimer = null;
    }

    /// <summary>The furnishing clock: one house looked at a minute. See <see cref="BotFurnish"/>.</summary>
    private static Timer _furnishTimer;

    private sealed class FurnishTimer : Timer
    {
        public FurnishTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => BotFurnish.Tick(Core.TickCount);
    }
}
