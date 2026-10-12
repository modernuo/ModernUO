using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Making things, as a module.
///
/// <para>
/// <see cref="BotPhase.World"/> and requires <c>Classes</c>, <c>Will</c> and <c>Shops</c> — the last because
/// the chain begins at a counter: a crafter with no way to buy cloth is a crafter with an opinion about
/// cloth.
/// </para>
///
/// <para>
/// <b>This is the module that makes the market a market.</b> Mining puts metal out; nothing bought it,
/// because nothing needed it. Sewing is the first trade that <em>buys</em> — and the moment a smith's chain
/// exists, the same shape turns a miner's ingots into somebody's input rather than somebody's pile.
/// </para>
/// </summary>
public sealed class BotCraftModule : BotModule
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotCraftModule));

    public override string Name => "Craft";

    public override BotPhase Phase => BotPhase.World;

    public override string[] Requires => ["Classes", "Will", "Shops"];

    public override void Start()
    {
        BotCraftConfig.Load();

        // What the craft system says to a bot that has no client to read it. See BotCraftEar.
        BotCraftEar.Listen();

        BotWill.Offer(new BotTailor());

        // Iron into things, and the board's orders before anything speculative. Until this the whole mining
        // chain ended at a bank box: nothing on the shard could turn an ingot into an object.
        BotWill.Offer(new BotSmith());

        // Iron into tools: the pickaxes, hatchets, hammers and kits every other trade wears out, which until
        // 28.09.2026 were the one thing on this shard no bot ever made. See BotTinkering.
        BotWill.Offer(new BotTinkerer());

        // Bows out of the island's own logs: the last weapon nobody made. Patrick's sixth point of 29.09.2026. See BotBowyer.
        BotWill.Offer(new BotBowyer());

        // Cloth and bandages out of the island's sheep: the tailors' cloth and everybody's bandages were the two things most
        // bought over counters (334 of 435 in twenty-five minutes of build 282). See BotWeave.
        BotWill.Offer(new BotWeaver());

        // Wood and feathers into arrows. Until this, arrows were the one consumable on this shard with no
        // source at all: the population was born with about nineteen hundred between thirteen shooters, the
        // provisioner keeps twenty at a time, and the rest of the supply was archers picking their own spent
        // shafts up off the ground one and two at a time. See BotFletching.
        BotWill.Offer(new BotFletcher());

        // The fletcher's feathers go out at a gold. See BotMarket.Staple.
        BotMarket.Staple(typeof(Server.Items.Feather));

        // Herbs and glass into bottles. Three classes have asked for Alchemy at a hundred since the day they
        // were written, every one of them carries a mortar and pestle issued on the strength of it, and until
        // now nothing anywhere could brew a thing. See BotFlask.
        BotWill.Offer(new BotAlchemist());

        // The shortest chain on the shard: a carcass, a skillet, a supper. No place, no fire, no walk.
        // See BotOven, and BotMeal for what eating one does.
        BotWill.Offer(new BotCook());

        // The first steps of a trade, bought from a shopkeeper who knows it. See BotTutor: nobody could start cooking.
        BotWill.Offer(new BotTutor());

        // A crafter's own material out of its own bank box for an order it passed over, before it digs (03.10.2026, econ13; Patrick:
        // "a crafter makes the order first, then goes to mine"). The auction's half of the rule is in BotWill. See BotCraftFirst.
        BotWill.Offer(new BotCraftFirst());

        logger.Information(
            "Craft ready: a tailor buys {Bolt} cloth at a time, works {Margin} points below its own skill, attempts every {Swing}ms, and asks {Price}gp a piece; a fletcher makes at least {Least} arrows at a time and opens them at {Arrow}gp, buying wood to match the feathers it holds because nobody anywhere sells a feather",
            BotSew.Bolt,
            BotThread.Margin,
            BotSew.SwingMs,
            BotSew.GoldPerPiece,
            BotFletching.LeastArrows,
            BotFletching.Worth
        );

        logger.Information(
            "Brewing ready: a brewer works {Margin} points below its own Alchemy, sets up once it holds {Least} bottles, buys as many empties as its herbs can fill (at least {Floor}, at most {Batch}) and opens a draught at {Worth}gp against the alchemist's fifteen; it brews only what the population drinks",
            BotFlask.Margin,
            BotFlask.LeastBottles,
            BotFlask.LeastBottles,
            BotFlask.Batch,
            BotFlask.Worth
        );

        logger.Information(
            "Crafters first (econ13): a crafter's winning dig gives way to work asked of it ({Running}); one short of material for an order walks to its own bank box within {Reach} tiles at {Claim}/min; a want's reservation lends a brewer its glass and herbs ({Lends}) for up to {Units} draughts it has {Chance:P0} of brewing, settled at the hand-over and called after {Call} minutes",
            BotCraftFirst.Running ? "on" : "off",
            BotCraftFirst.DrawReach,
            BotCraftFirst.DrawClaim,
            BotAdvance.Running ? "on" : "off",
            BotAdvance.MostUnits,
            BotAdvance.LeastChance,
            BotAdvance.CallMs / 60000
        );

        logger.Information(
            "Supplies from our own benches, sold hand to hand: a weaver walks to the flock with the least road per sheep within {Pasture} tiles and its leash, gathers up to {Wool} wool, spins and weaves at {Looms}, and keeps cloth over {Keep} to sell; a smith works at {Forges}; a tinker short of iron and a fletcher short of feathers say so aloud and wait for a carrier; what a maker makes for an order rides in its pack to whoever ordered it",
            BotWeaver.PastureReach,
            BotWeave.WoolAfield,
            BotLooms.WorkshopsInBritain ? "Britain's wheels and looms" : "the nearest wheel and loom",
            BotSew.Bolt * 2,
            BotSmith.WorkshopsInBritain ? "Britain's forges" : "the nearest forge"
        );

    }

    public override void Reset()
    {
        // The looms and the pastures hold references into the world that was just unloaded; the weaver's counters are its own.
        BotLooms.Forget();
        BotPastures.Forget();
        BotWeaver.Forget();
        BotTinkerer.Forget();
        BotTinker.Forget();
        BotTailor.Forget();
        BotSmith.Forget();
        BotFletcher.Forget();
        BotAlchemist.Forget();
        BotAdvance.Forget();
        BotCraftFirst.Forget();
        BotCook.Forget();
        BotBake.Forget();
        BotMeal.Forget();
        BotStores.Forget();
    }
}
