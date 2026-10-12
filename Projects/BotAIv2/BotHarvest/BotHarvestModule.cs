using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Getting a living out of the ground, as a module: reads its numbers and offers the trade to the decision
/// layer.
///
/// <para>
/// <see cref="BotPhase.World"/>, because everything it knows is a place. Requires <c>Will</c>, which it
/// hands a proposer, and <c>Classes</c>, because a gatherer's pickaxe comes from its kit.
/// </para>
///
/// <para>
/// <b>This is the first module that gives the population something to want</b>, and it is deliberately the
/// gatherer's chain rather than the fighter's: dig, melt, bank exercises every part of the machinery at
/// once — stages that survive an interruption, a named skill that only counts when the work finishes, goods
/// that are worth something before they are sold, and a definition of finished that is somewhere other than
/// where the work happened. A hunt would have exercised none of the last three.
/// </para>
///
/// <para>
/// Its switch is worth having for the plainest reason: with it off, the population has nothing to do and
/// says so in the census. That is the cheapest way to tell "the brain is not choosing" from "there is
/// nothing to choose".
/// </para>
/// </summary>
public sealed class BotHarvestModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHarvestModule));

    public override string Name => "Harvest";

    public override BotPhase Phase => BotPhase.World;

    /// <summary>
    /// <b>Population is here because of one line, and the line was silently doing nothing without it.</b>
    /// Prospecting the named lode needs to know which facet the population lives on, and that is settled by
    /// the population's own configuration — which runs in this same phase and, before this was declared, ran
    /// <em>after</em>. So the sweep was handed a null map, returned nought, and said nothing. A declared
    /// dependency that cannot be met is a named failure at boot; an undeclared one is a feature that quietly
    /// is not there, which is exactly what happened.
    /// </summary>
    public override string[] Requires => ["Classes", "Will", "Population"];

    public override void Start()
    {
        BotHarvestConfig.Load();

        // One sweep of the named lode, so that there is ore on the board somewhere no bot has yet been.
        // See BotGround.Lode for the closed circle this breaks.
        BotGround.Prospect(BotPopulation.Home);

        BotWill.Offer(new BotMiner());

        // The other kind of harvest, and the only one that costs nothing at all: the world leaves reagents
        // lying about and refills them, and the population has been walking over them since it was raised.
        BotWill.Offer(new BotForager());

        // The sage's trip to the woods. Registered here rather than with the spells because what it produces
        // is ground goods, and because the one thing it must not become is a second forager: see BotHerbs.
        BotWill.Offer(new BotHerbalist());

        // Wood, and the skill had been on the Gatherer's sheet since the class was written with no errand
        // anywhere that swung an axe. It matters now because an arrow is a shaft and a feather, and a shaft
        // is a log — see BotTimber.
        BotWill.Offer(new BotWoodsman());

        logger.Information(
            "Harvest ready: a trip is reckoned at {Expects} a minute over {Minutes} minutes, an ingot at {Ingot} gold; the ground is swept {Reach} tiles around the first bot to ask, at most {Sweeps} times",
            BotDig.Prior,
            BotDig.WorkMinutes,
            BotDig.GoldPerIngot,
            BotGround.Reach,
            BotGround.MaxSurveys
        );

        // Taken from the engine here rather than in a field initialiser, because the mining system is built
        // by content initialisation and a value read before that would be the fallback for the life of the
        // shard. Said out loud because it is the number that decides whether a rested seam comes back full
        // or barren, and it was wrong by half for as long as it was ours. See BotOre.RespawnMs.
        // The ear on the harvest system own sentences. Opened before anybody swings, and closed in Reset,
        // or a world reload leaves two ears on one head. See BotHeard.
        BotHeard.Listen();

        logger.Information(
            "A worked-out seam rests {Rest}s, which is the engine's own longest refill for an ore bank plus a minute; a rest shorter than that hands the next miner a barren hole",
            BotGround.RestFromEngine() / 1000
        );

        logger.Information(
            "Foraging ready: anything the world calls a reagent, picked up from {Reach} tiles away while a pack is under {Full:P0} full, reckoned at {Expects} a minute and put on the board at {Guess}gp a piece",
            BotForage.Reach,
            BotForage.FillFraction,
            BotForage.Prior,
            BotForage.Guess
        );
    }

    /// <summary>
    /// A world reload is a different world. Every seam, fire and counter on record is a place in the world
    /// that has just been replaced, and a remembered forge is a bot walking to an empty field.
    /// </summary>
    public override void Reset()
    {
        logger.Information("Harvest, before the reload: {State}", BotGround.Describe());

        BotHeard.Forget();
        BotGround.Reset();
        BotMiner.Forget();
        BotWoodsman.Forget();

        // <b>Dead since it was written, and nothing said so.</b> BotHerbalist.Forget exists, resets the
        // picking counters and the trade's own tallies with them, and had no caller anywhere in the
        // assembly — so the Gathering line's herb clause was the one number in the summary that survived a
        // world reload while every other trade's went back to nought. Found while working out why two
        // consecutive readings of it were identical, which is a question that would not have been asked at
        // all if the rest of the block had not reset in step.
        BotHerbalist.Forget();
    }

    public static string Summarise() => BotGround.Describe();
}
