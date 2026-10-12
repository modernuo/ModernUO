using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot with a hammer and some metal a turn at an anvil, and offers it the board's orders first.
///
/// <para>
/// <b>Orders before speculation, and the reason is arithmetic rather than politeness.</b> Something on the
/// board has money already down against it — <see cref="BotMarket.Ask"/> takes the payment when the want is
/// raised — so filling one is a sale that has already happened. Making something on spec is making something
/// that might sell. The auction is told the difference through <see cref="BotForge.Expects"/> and settles it
/// the same way it settles everything else, so a smith with nothing on the board still smiths.
/// </para>
///
/// <para>
/// <b>What it will not do is take an order it cannot fill.</b> A recipe beyond the bot's skill, or one
/// needing a material it has not got, is refused here rather than discovered at the anvil — because a taken
/// order that fails is a bot's coin held in escrow for nothing while the bot that paid it goes on fighting
/// with a broken sword.
/// </para>
/// </summary>
public sealed class BotSmith : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotSmith));

    /// <summary>How much metal makes a trip to the anvil worth taking at all.</summary>
    public static int LeastMetal { get; set; } = 6;

    /// <summary>
    /// How many pieces' worth of metal a smith must hold to take an order off the board. Two.
    ///
    /// <para>
    /// <b>One piece's worth was one swing's worth.</b> A miss costs half a piece, so a smith holding exactly the metal for
    /// the order had one try, and the stint ended "out of metal" at the first miss. Hale on build 201 (24.09.2026) took the
    /// same order for a plate gorget six times between 07:21 and 07:54 — "1 attempts, 0 made — out of metal", every one.
    /// The speculative choice already asks for <see cref="BotAnvil.Tries"/> pieces' worth (three) by dividing the pile.
    /// Two here and not three, because the metal board stocks a smith to <see cref="BotBullion.Enough"/> (twenty ingots):
    /// three plate gorgets' worth is thirty, and an order nobody can ever qualify for is money frozen on the board. Two
    /// carries two misses and then the piece.
    /// </para>
    /// </summary>
    public static int OrderPieces { get; set; } = 2;

    private static bool _saidNoSystem;

    private static bool _saidNoForge;

    private static bool _said;

    /// <summary>Every gate apart, with the denominator. There is no bucket called "other".</summary>
    public static long Asked { get; private set; }

    public static long NoHammer { get; private set; }

    public static long NoMetal { get; private set; }

    public static long NoForge { get; private set; }

    /// <summary>
    /// Whether a smith works only at a forge in Britain, where the island's workshops are (Patrick's order of 30.09.2026). Off, the
    /// forge nearest the smith, as before.
    /// </summary>
    public static bool WorkshopsInBritain { get; set; } = true;

    /// <summary>Smiths whose nearest reachable forge was not in Britain, and who therefore did not smith.</summary>
    public static long NotBritain { get; private set; }

    public static long ToOrder { get; private set; }

    /// <summary>Orders passed over for want of iron rather than for want of skill. Counted apart on purpose.</summary>
    public static long ShortOfMetal { get; private set; }

    /// <summary>Ingots taken back off the smith's own stall so that it could work at all.</summary>
    public static long Fetched { get; private set; }

    /// <summary>Smiths past the metal floor whose pack could still not fill any recipe their skill allows.</summary>
    public static long NothingAffordable { get; private set; }

    public static long OnSpec { get; private set; }

    // ---- The metal an order asks for, remembered for the smith's own metal board (03.10.2026, build next) --------------------

    /// <summary>
    /// How long a smith remembers the metal the dearest order it passed over would have taken. Ten minutes: the metal board's
    /// clock is a minute (<see cref="BotNeeds"/>) and a carrier's walk is under eight.
    /// </summary>
    public static int ShortMs { get; set; } = 600000;

    /// <summary>The most metal a smith stocks for an order, whatever the order asks: two plate chests' worth.</summary>
    public static int MostForOrder { get; set; } = 50;

    private static readonly Dictionary<Serial, (int Need, long Tick)> _short = [];

    /// <summary>Times a smith short of metal for an order was remembered as needing it, and ingots it took out of its own bank box for one.</summary>
    public static long Remembered { get; private set; }

    public static long FetchedForOrder { get; private set; }

    /// <summary>
    /// The metal the dearest order this smith passed over lately would have taken (its pieces at <see cref="OrderPieces"/>), or
    /// nought. Read by the metal board (<see cref="BotBullion"/>) and the porter's keep list (<c>BotUnload.Keeps</c>), so the smith
    /// orders and keeps that much rather than <see cref="BotBullion.Enough"/>.
    ///
    /// <para>
    /// <b>Three numbers on one shelf (measured 02-03.10.2026).</b> The metal board stocks a smith to twenty ingots
    /// (<see cref="BotBullion.Enough"/>), the porter keeps twenty in its pack and banks the rest, and an order is taken only with
    /// two pieces' worth in the pack: 36 for chain legs, 40 for a chain tunic, 50 for a plate chest. No smith held that, so the
    /// armour orders were passed over for want of iron 454, 681, 258 and 590 times in the sessions of 15:26, 16:51, 17:54 and
    /// 03:08, while the shops by the bank showed 366, 535 and 3,440 iron ingots, and every chain and plate line failed "maker
    /// short of material". The smith now asks the island for what the order needs, and keeps it.
    /// </para>
    /// </summary>
    public static int Short(Mobile body)
    {
        if (body == null || !_short.TryGetValue(body.Serial, out var known))
        {
            return 0;
        }

        if (Core.TickCount - known.Tick >= ShortMs)
        {
            _short.Remove(body.Serial);

            return 0;
        }

        return known.Need;
    }

    public string Name => "Smith";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        if (BotAnvil.Kit(body) == null)
        {
            NoHammer++;

            return null;
        }

        Asked++;

        if (BotAnvil.System == null)
        {
            // Content initialisation builds the craft systems, and anything that asks before that gets null.
            // Said once: a smith that never works is otherwise indistinguishable from a lazy one.
            if (!_saidNoSystem)
            {
                _saidNoSystem = true;

                logger.Error("The blacksmithing system does not exist yet, so nobody can forge");
            }

            return null;
        }

        // Its own metal out of its own bank box, at a bank, before it is told it has none — the fletcher's rule about its feathers
        // and the tailor's about its leather. See BotAnvil.Fetch for the ring of three gates this breaks.
        if (BotAnvil.Ingots(body, BotAnvil.Best(body, LeastMetal)) < LeastMetal)
        {
            Fetched += BotAnvil.Fetch(bot, body, LeastMetal);
        }

        // Any metal it can work, not iron alone. A smith standing on forty bronze ingots was being told it
        // had no metal, which is how a population that digs mostly bronze came to forge almost nothing.
        if (BotAnvil.Ingots(body, BotAnvil.Best(body, LeastMetal)) < LeastMetal)
        {
            NoMetal++;

            return null;
        }

        // A forge with an anvil beside it. BotGround only records the pair — the miner needs both to smelt —
        // so this is the same list, already surveyed, already reachable.
        //
        // <b>Britain's (01.10.2026; Patrick, 30.09: "all the workshops live in Britain").</b> The forge nearest the market's square rather
        // than the one nearest the smith, and only if it stands in Britain: a smith works where the island trades, beside the shops
        // its pieces sell at and the carriers that bring its metal. A smith with no reachable forge in Britain within its leash does
        // not smith (counted). The miner's smelting is not a workshop and stays at the forge nearest its seam — see BotDig.
        var square = WorkshopsInBritain ? BotMarket.Square(map) : Point3D.Zero;
        var smithy = BotGround.Fire(bot, square != Point3D.Zero ? square : body.Location);

        if (WorkshopsInBritain && smithy != Point3D.Zero && !BotMarket.InBritain(smithy))
        {
            NotBritain++;

            return null;
        }

        if (smithy == Point3D.Zero)
        {
            NoForge++;

            if (!_saidNoForge)
            {
                _saidNoForge = true;

                logger.Error("No forge with an anvil beside it is known within reach of the bots on {Map}", map);
            }

            return null;
        }

        // The board first. Nearest order this bot could actually make, and the money on it is already down.
        var order = Order(bot, body);

        if (order != null)
        {
            ToOrder++;
            order.Offered();
            Once(body, order);

            return new BotForge(map, smithy, order);
        }

        // Nothing the pack can pay for. A named nought rather than a silent return: this is the branch that
        // now catches what used to walk to the forge and report "out of metal" there, so if it ever reads
        // high next to a healthy NoMetal the two floors have drifted apart again.
        // A piece nobody ordered needs a place in the pack: daggers and maces do not stack (06.10.2026, work17). See BotRoom.SpecShut.
        if (BotRoom.SpecShut(body))
        {
            return null;
        }

        var spec = BotAnvil.Choose(body);

        if (spec == null)
        {
            NothingAffordable++;

            return null;
        }

        OnSpec++;

        // The kind it means to make goes with the offer, so the claim can read its glut (01.10.2026). See BotGlut.
        BotGlut.Spec(spec.ItemType);

        return new BotForge(map, smithy, null, spec.ItemType);
    }

    /// <summary>
    /// The most valuable standing order this bot could fill out of iron, or null.
    ///
    /// Worth rather than nearness: everything on this board is within a few minutes' walk of everything else,
    /// and what separates two orders is what they are paying.
    /// </summary>
    private static BotWant Order(IBotWilful bot, Mobile body)
    {
        var wants = BotMarket.Wants;

        BotWant best = null;
        var bestWorth = 0;

        // Raised once for the bot rather than once for every want it walked past — this counts against
        // Asked, which is a count of bots, and the tailor's twin of it read 567 against a denominator of 264.
        var starved = false;
        var starvedNeed = 0;
        var starvedWorth = 0;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (!want.IsOpen || ReferenceEquals(want.Buyer, bot))
            {
                continue;
            }

            var recipe = BotAnvil.Recipe(body, want.Kind);

            if (want.Worth <= bestWorth || recipe == null)
            {
                continue;
            }

            // Only an order a carrier from here could deliver, and not one this smith has already beaten out and is holding for
            // the walk (01.10.2026, build 384 review): the piece reaches the buyer by BotDeliver now, not off the bench. See
            // BotCourier.Answerable and BotCourier.Holding.
            if (!BotCourier.Answerable(bot, want) || BotCourier.Holding(bot, want))
            {
                continue;
            }

            // <b>Skill enough and metal enough are two different questions, and only the first was asked.</b>
            // Recipe() answers "is this bot good enough to make one"; it says nothing about whether there is
            // any iron in the pack to make it out of. The proposer's own gate is LeastMetal — six ingots,
            // which is enough to be worth walking to an anvil for — and a ringmail tunic wants eighteen. So a
            // smith holding anything between the two took the order, walked to the forge and failed "out of
            // metal", over and over: fifty-six of the hundred and eight attempts at a hauberk on the night of
            // 25.08.2026, against two that were actually made. And because orders are picked by what they are
            // worth, it reliably chose the dearest one on the board — which is the one needing the most iron.
            //
            // Two numbers on one shelf, and the band between them is where the whole armour trade sat all
            // night. The recipe knows what it costs; nothing had to be invented, only asked.
            var cost = BotCraftwork.Cost(recipe) * System.Math.Max(1, OrderPieces);

            if (BotAnvil.Ingots(body, BotAnvil.Best(body, cost)) < cost)
            {
                starved = true;
                want.Passed(BotPass.Material);

                // The dearest order it could not fill: the metal that one would take is what it asks the island for (build next).
                if (want.Worth > starvedWorth)
                {
                    starvedWorth = want.Worth;
                    starvedNeed = cost;
                }

                continue;
            }

            best = want;
            bestWorth = want.Worth;
        }

        if (starved && best == null)
        {
            ShortOfMetal++;
            Remember(bot, body, starvedNeed);

            // And for the walk to its own bank box, whatever the order takes (03.10.2026, econ13). See BotCraftFirst.
            BotCraftFirst.Short(body, BotAnvil.Best(body, starvedNeed), Math.Min(starvedNeed, MostForOrder));
        }

        return best;
    }

    /// <summary>
    /// The metal the dearest order it passed over would take, kept for <see cref="ShortMs"/>, and its own metal out of its bank box
    /// toward it when it stands at a bank. See <see cref="Short"/>.
    /// </summary>
    private static void Remember(IBotWilful bot, Mobile body, int need)
    {
        need = Math.Min(need, MostForOrder);

        if (need <= BotBullion.Enough)
        {
            return;
        }

        if (_short.Count > 4096)
        {
            _short.Clear();
        }

        _short[body.Serial] = (need, Core.TickCount);
        Remembered++;

        // Topped up to what the order takes, not that much more: a bank box moves what it is asked for, on top of the pack (review
        // of build next, F4). The metal the order is read in, as Order reads it.
        var metal = BotAnvil.Best(body, need);
        var lack = need - BotAnvil.Ingots(body, metal);

        if (lack > 0)
        {
            FetchedForOrder += BotMarket.FromBank(bot, metal, lack);
        }
    }

    private static void Once(Mobile body, BotWant order)
    {
        if (_said)
        {
            return;
        }

        _said = true;

        logger.Information(
            "{Name} is the first smith to take an order off the board: {Buyer} wants {Item} and has {Worth}gp down",
            body.Name,
            order.Buyer?.Self?.Name ?? "somebody",
            order.Label,
            order.Worth
        );
    }

    public static string Describe() =>
        Asked == 0
            ? "nobody with a hammer has been asked to forge"
            : $"{Asked} asked: {ToOrder} took an order off the board, {ShortOfMetal} passed one over for want of iron, {OnSpec} forged on spec, "
              + $"{NoMetal} short of metal ({Fetched} ingots taken out of their own bank box at a bank to stop being it), {NothingAffordable} with metal but not enough for any recipe they can work, {NoForge} with no forge in reach, {NotBritain} with no forge in Britain within their reach; {BotCraftwork.Unlikely} recipes passed over across the crafts for less than {BotCraftwork.LeastChance:P0} of making them; the board's metal reads {BotAnvil.Reading}, so an empty smith orders {BotAnvil.Metal.Name}; "
              + $"{Remembered} times a smith short of metal for the dearest order it passed over asked the island for that much (up to {MostForOrder} ingots, kept {ShortMs / 60000} minutes; {FetchedForOrder} ingots taken out of its own bank box for it), {_short.Count} smiths remembering one now; "
              + $"{BotForge.Describe()}";

    public static void Forget()
    {
        _said = false;
        _saidNoSystem = false;
        _saidNoForge = false;
        Asked = 0;
        NoHammer = 0;
        NoMetal = 0;
        NoForge = 0;
        NotBritain = 0;
        ToOrder = 0;
        ShortOfMetal = 0;
        Fetched = 0;
        NothingAffordable = 0;
        OnSpec = 0;
        Remembered = 0;
        FetchedForOrder = 0;
        _short.Clear();
        BotForge.Forget();
    }
}
