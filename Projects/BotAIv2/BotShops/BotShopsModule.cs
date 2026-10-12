using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Trading with the shopkeepers, as a module.
///
/// <para>
/// <see cref="BotPhase.World"/> — a shopkeeper is a mobile standing somewhere. Requires <c>Classes</c> (what
/// a bot is short of comes from its class's kit) and <c>Will</c> (it hands over a proposer).
/// </para>
///
/// <para>
/// This is the module that gives the population an <em>input</em> side. Until it existed the whole assembly
/// could only take things out of the ground: nothing could be bought, so no trade could depend on materials
/// it did not dig up itself.
/// </para>
///
/// <para>
/// <b>And both directions of the world's money live here.</b> Coin handed to a shopkeeper leaves the world;
/// coin taken from one is the only coin that enters it. Bots are born with none and nothing else mints any, so
/// without <see cref="BotPeddler"/> every piece of work that costs money to start fails on its first beat and
/// the only thing that can happen on the shard is digging. Which of the two dominates is the whole health of
/// the economy, and it is one subtraction: see the <c>bought for / sold for</c> line this module prints.
/// </para>
/// </summary>
public sealed class BotShopsModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotShopsModule));

    public override string Name => "Shops";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Classes", "Will"];

    public override void Start()
    {
        BotShopsConfig.Load();

        BotWill.Offer(new BotShopper());
        BotWill.Offer(new BotPeddler());

        // Asking the population for a replacement when something is wearing out. Until this, exactly one
        // thing on the shard ever raised a want and the board of needs was empty — see BotUpkeep.
        BotWill.Offer(new BotUpkeep());

        // A crafter with coin buys its metal instead of walking after it, which puts the order in front of
        // every miner on the shard — see BotBullion.
        BotWill.Offer(new BotBullion());


        logger.Information(
            "Shops ready: shopkeepers swept {Reach} tiles around the first bot to ask, traded with from {Counter} tiles, a supply is restocked once it falls below {Short:P0} of what the bot was born with (a counter {Far} minutes' walk or more off is asked of the island aloud first), and goods nobody on the island is asking for are carried to a counter out of the pack or, at a bank, out of the bank box",
            BotShops.Reach,
            BotShops.CounterReach,
            BotShopper.Short,
            BotShopper.AskFarMinutes
        );

        // The boot line is the only proof of what a dial is running with. See BotAskFirst.
        logger.Information(
            BotAskFirst.Running
                ? "Restocks of cloth and bandages, reagents, arrows and bolts and tools ask the island first when a bot within a carrier's reach holds them over its keep: a want at the counter's price, {Wait:F0} min for a carrier, then the walk; not again for {Again:F0} min; never for a bot under {Hurt:P0} health, in a fight or out of a supply it fights or mends with"
                : "Restocks do not ask the island first (BotAskFirst off; {Wait:F0} min, {Again:F0} min, {Hurt:P0} unused)",
            BotAskFirst.WaitMs / 60000.0,
            BotAskFirst.AgainMs / 60000.0,
            BotAskFirst.HurtShare
        );

        // The boot line is the only proof of what the capital is running with. See BotCapital.
        logger.Information(
            BotCapital.Running
                ? "The capital is {Name}: its shopkeepers pay ×{Premium:F2} of their price, the difference out of the city's treasury above {Reserve}gp; a load on a stall walks to them from within {Reach} tiles and the bot's own leash when that pays for the road at {Tile:F2}gp a tile, and a trader offered a journey takes its goods there"
                : "The capital's price is off: {Name} pays what every other town pays (×{Premium:F2} unused, {Reserve}gp, {Reach} tiles, {Tile:F2}gp a tile)",
            BotCapital.Name,
            BotCapital.Premium,
            BotCapital.Reserve,
            BotCapital.Reach,
            BotPeddler.PettyPerTile
        );
    }

    /// <summary>
    /// A world reload is a different world. Every remembered shopkeeper is a mobile of the world being
    /// replaced, and a reference to one of those is a reference to a deleted object.
    /// </summary>
    public override void Reset()
    {
        logger.Information("Shops, before the reload: {State}", BotShops.Describe());

        BotShops.Reset();
        BotShopper.Forget();
        BotAskFirst.Reset();
        BotPeddler.Forget();
        BotUpkeep.Forget();
        BotBullion.Forget();
        BotCapital.Forget();
    }

    public static string Summarise() => BotShops.Describe();
}
