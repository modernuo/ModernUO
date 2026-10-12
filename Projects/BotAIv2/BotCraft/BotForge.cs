using System;
using System.Collections.Generic;
using Server.Engines.Craft;
using Server.Items;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// Beating iron into something, at a forge, and handing it to whoever asked for it.
///
/// <para>
/// <b>The trade that was missing, and its absence is why ore went nowhere.</b> Mining has worked for days:
/// dig, smelt, carry the metal to a counter. What happened to the metal after that was nothing at all —
/// ingots went into bank boxes and onto stalls, and no bot on the shard could turn one into a thing. The
/// crafter class has carried a smith's hammer since it was written, and the comment beside it says in as
/// many words what a smith without work becomes: <em>a bot with an opinion about metal</em>. This is the
/// work.
/// </para>
///
/// <para>
/// <b>An order off the board comes first, and that is the whole point of building it now.</b>
/// <see cref="BotUpkeep"/> puts a bot's worn-out sword on the board with the money already down, and until
/// this existed there was nobody who could fill it — the order would stand until it timed out and the coin
/// would come back. A smith that reads the board turns "somebody needs a blade" into a blade, which is the
/// one loop this economy has never closed.
/// </para>
///
/// <para>
/// Three stages, and the third is the one the first version always forgot: go to a forge, beat the metal,
/// <em>hand the thing over</em>. A hauberk in the maker's own backpack has helped nobody.
/// </para>
/// </summary>
public sealed class BotForge : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotForge));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "forge";

    /// <summary>What smithing is reckoned at per minute before the ledger corrects it.</summary>
    /// <summary>
    /// What an afternoon at an anvil is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// <b>Raised from 55 on the night of 07.09.2026, because at 55 the work was never once taken.</b> An hour
    /// of a shard whose whole craft is held by four thinking bots produced `51 forged on spec` - fifty-one
    /// offers - and `took on forge: 0`. Mining was reckoned at 60 to 67 and won every single time, so no bot
    /// ever stood at an anvil, and the summary line said "no stint at an anvil has ended yet" for an hour.
    /// </para>
    ///
    /// <para>
    /// <b>And it could not correct itself, which is the part worth remembering.</b> The ledger revises a
    /// trade's expectation from what it actually paid - but only for work that has been done. An opening
    /// number set too low is therefore self-sealing: the work never wins, so it never pays, so there is
    /// nothing to revise it with. Within fifteen minutes of the dial being moved to 120 on the live shard the
    /// first daggers were coming off the anvil at 940 and 1300 a minute, which is where the honest number
    /// was all along - twenty times the guess that was keeping it off the menu.
    /// </para>
    ///
    /// <para>
    /// Left at 120 rather than at the measured 1000: the ledger climbs from here on its own now that the work
    /// is being taken, and an opening number set to the best case would be the same mistake pointing the
    /// other way.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 120.0;

    /// <summary>How long a stint at the anvil is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 6.0;

    /// <summary>
    /// How long between attempts.
    ///
    /// <para>
    /// <b>Longer than the engine's own lock, and for a fortnight it was shorter.</b> <c>CraftItem.Craft</c> takes an
    /// action lock and holds it for one craft effect at the system's delay — 1.25 s for every craft system on this
    /// shard — and answers a call inside it with "you must wait to perform another action" and nothing else. At a
    /// second between swings every second swing landed inside the lock, was refused in silence, and was counted here as
    /// a swing: the ear (build 174) heard 836 of those in three hours on 21.09.2026, against 435 honest failures, and
    /// the smith's twenty-four attempts were twelve. Hale's "24 attempts, 0 made, iron intact" of 20.09 was this and a
    /// run of bad luck on the other twelve. The tailor and the scribe swing every three seconds and never met it.
    /// </para>
    /// </summary>
    public static int SwingMs { get; set; } = 1300;

    /// <summary>How many attempts one stint may make before it takes what it has and stops.</summary>
    public static int MaxSwings { get; set; } = 24;

    /// <summary>What a piece of ironwork is offered at when nothing on the shard has priced one.</summary>
    public static int Guess { get; set; } = 45;

    /// <summary>
    /// How stints at the anvil ended. Every ending apart, with the denominator, and no bucket called "other".
    ///
    /// <para>
    /// Kept because the proposer's counters say what smiths were <em>offered</em> and nothing at all about
    /// what became of it. Between them they made the shape of this trade's waste invisible: the summary read
    /// sixteen forging on spec, and only a grep of the whole log showed that fifty-two of seventy-eight
    /// stints ended with nothing and thirty-eight of those were out of metal. A number worth changing a
    /// threshold over should not have to be grepped for.
    /// </para>
    /// </summary>
    public static long Stints { get; private set; }

    /// <summary>Stints that ended with something on the anvil.</summary>
    public static long Wrought { get; private set; }

    /// <summary>Pieces made across all of them.</summary>
    public static long Pieces { get; private set; }

    /// <summary>Stints the metal ran out under. The one this trade was losing to. See BotAnvil.Tries.</summary>
    public static long RanOut { get; private set; }

    /// <summary>Stints that used every swing they had and made nothing.</summary>
    public static long Fruitless { get; private set; }

    /// <summary>Stints given up at a forge the engine would not smith at, or with no road to one.</summary>
    public static long NoPlace { get; private set; }

    /// <summary>Smiths refused beside a forge who walked round to the tile between it and its anvil. See BotAnvil.Between.</summary>
    public static long Rounded { get; private set; }

    private enum Leg
    {
        Walk,
        Work,
        Hand
    }

    private readonly Map _map;

    private readonly Point3D _smithy;

    /// <summary>What was asked for, when this stint is filling somebody's order. Null when it is speculative.</summary>
    private readonly BotWant _order;

    /// <summary>The want this is made for, or null on spec. Read by <see cref="BotCraftFirst"/> (03.10.2026, econ13).</summary>
    public BotWant Order => _order;

    private Leg _leg;

    /// <summary>Whether the tile between the forge and its anvil has been looked for. Once a stint. See BotAnvil.Between.</summary>
    private bool _rounded;

    /// <summary>The tile between the forge and its anvil this smith is walking round to, or nought.</summary>
    private Point3D _stand;

    private CraftItem _recipe;

    private Type _kind;

    private int _swings;

    /// <summary>The metal this piece is being beaten out of. See BotAnvil.Best.</summary>
    private Type _metal;

    /// <summary>
    /// The metal of the last swing, which is the metal of whatever that swing made: <see cref="_metal"/> is asked again every beat,
    /// and a delivery between the swing and the count could change it. See <see cref="Book"/>.
    /// </summary>
    private Type _swungMetal;

    /// <summary>What this stint made, by metal. Null until something is made.</summary>
    private Dictionary<Type, int> _byMetal;

    /// <summary>
    /// Every piece beaten out since the world loaded, by the metal it was beaten out of (01.10.2026, Patrick's point 2: "check again
    /// that crafters can make armour from the other metals"). Until then no line on the shard named the metal of a crafted piece,
    /// so whether a single coloured helm had ever come off an anvil could not be read anywhere.
    /// </summary>
    private static readonly Dictionary<Type, long> _piecesByMetal = [];

    private int _made;

    /// <summary>
    /// Pieces of the kind already in the pack when the work began, which this work did not make. See <see cref="Working"/>.
    ///
    /// <para>
    /// <b>Every other craft had a baseline and this one did not.</b> Sewing, fletching, brewing and cooking count what
    /// the pack gained; the forge counted what the pack held. Edda carried her own war fork, bound to her and kept in
    /// the pack when she wielded something better, and somebody wanted war forks: from 10:18 to about 12:00 on 25.09.2026
    /// she "made" that fork 20,494 times, three a second — the order sent her to the anvil, the count said one was
    /// made, the hand-over offered it to the want and to the market, both turned a bound thing away, and the work
    /// finished paid 69 and was taken again. It carried the shard's completion to 99.4% for the hour and the forge's
    /// learned worth to 276 a minute.
    /// </para>
    /// </summary>
    private int _had = -1;

    /// <summary>Pieces the work began with in the pack, left out of what it made. See <see cref="_had"/>.</summary>
    public static long Preowned { get; private set; }

    private int _handed;

    private bool _swung;

    private long _swungTick;

    /// <summary>
    /// What the smith meant to make when the stint was offered on spec, for the claim only: the anvil chooses again when it
    /// gets there, since the pack may have changed on the way. See <see cref="BotGlut.Claim"/>.
    /// </summary>
    private readonly Type _spec;

    public BotForge(Map map, Point3D smithy, BotWant order = null, Type spec = null)
    {
        _map = map;
        _smithy = smithy;
        _order = order;
        _kind = order?.Kind;
        _spec = spec;
    }

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _smithy;

    /// <summary>
    /// An order is worth more than speculation, and by exactly the amount somebody has already put down.
    ///
    /// Not a bonus invented here: the money is real, it is in escrow, and it is the difference between making
    /// something that will certainly be bought and making something that might be.
    ///
    /// <para>
    /// <b>And speculation on a kind the island is full of is worth less still (01.10.2026, Patrick's point 8).</b> The claim for a
    /// stint on spec reads the glut of the kind it means to make, so a smith sees the pile at the moment of choosing and not three
    /// stints later when the ledger has learned it; hunting or a company wins the choice on the same arithmetic as always.
    /// </para>
    /// </summary>
    public override double Expects => _order != null ? Prior * 1.6 : BotGlut.Claim(_kind ?? _spec, Prior);

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => BotAnvil.Skill;

    /// <summary>Mostly goods until they are sold, which is what Made is for.</summary>
    public override double Coin => _order == null ? 0.4 : 1.0;

    /// <summary>What was beaten out, at the island's price for what somebody asks for and the surplus price for the rest. See <see cref="BotGlut.Yield"/>.</summary>
    public override int Made => BotGlut.Yield(_kind, _made, Guess);

    public override string Stage =>
        _leg switch
        {
            Leg.Walk => _order == null ? "off to a forge" : $"off to a forge to make {_kind?.Name}",
            Leg.Work => $"beating out {_kind?.Name ?? "iron"} ({_swings} attempts, {_made} made)",
            _ => $"handing over {_kind?.Name}"
        };

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        return _leg switch
        {
            Leg.Walk => Walking(bot, body),
            Leg.Work => Working(bot, body),
            _ => Handing(bot, body)
        };
    }

    private BotDoing Walking(IBotWilful bot, Mobile body)
    {
        // Asked of the engine rather than of the distance to the remembered point: what matters is whether a
        // hammer will work here, and only the engine knows that. See BotAnvil.AtASmithy.
        if (BotAnvil.AtASmithy(body))
        {
            _leg = Leg.Work;

            return BotDoing.Work("at the anvil");
        }

        // <b>Standing at the remembered forge, and the engine still says no.</b> That is the end of the road
        // for this forge and it has to be said out loud, because the alternative is what used to happen: the
        // walk was aimed two tiles out, arriving satisfied it, the smithy test did not, and the undertaking
        // answered the identical walk order for ever — a piece of work that is both immortal and invisible,
        // which is the failure this project has paid for more than once.
        //
        // Written down under the <em>place's</em> name, "fire", which is the word BotGround.Fire asks in.
        // Filed under the undertaking's name it would be written and never read, and the next beat would
        // choose this same unusable forge again on distance alone.
        //
        // <b>But first round to the anvil's side, once.</b> Beside the forge is not always within reach of its anvil —
        // Britain's upper smithy has the anvil two tiles north of the forge, and a smith arriving from the south stands
        // three from it. See BotAnvil.Between.
        if (!_rounded && body.InRange(_smithy, 1))
        {
            _rounded = true;
            _stand = BotAnvil.Between(_map, _smithy, body.Location);

            if (_stand != Point3D.Zero && (_stand.X != body.X || _stand.Y != body.Y))
            {
                Rounded++;
            }
            else
            {
                _stand = Point3D.Zero;
            }
        }

        if (_stand != Point3D.Zero && (_stand.X != body.X || _stand.Y != body.Y))
        {
            return BotDoing.Walk(_map, _stand, BotArrival.Exactly, "round to the anvil side of the forge");
        }

        if (_stand != Point3D.Zero || body.InRange(_smithy, 1))
        {
            Refuse(bot);

            // And for every smith, not only this one: the anvil the engine will not take is a fact about the forge. See
            // BotGround.UnfitMs.
            BotGround.Unfit(_smithy);

            Stints++;
            NoPlace++;

            return BotDoing.Failed(
                $"no anvil the engine will accept within {BotAnvil.Reach} of the forge at ({_smithy.X}, {_smithy.Y})"
            );
        }

        // Beside it, not two tiles off. The engine wants an anvil <em>and</em> a forge within
        // BotAnvil.Reach of the body, with line of sight to both; aiming at the far edge of that was how a
        // bot came to be told it was at a smithy while standing where no hammer would work.
        return BotDoing.Walk(_map, _smithy, BotArrival.Beside, "to a forge");
    }

    /// <summary>This forge is no use to this bot. Kept off its list for a while — see BotLedger.Beware.</summary>
    private void Refuse(IBotWilful bot) => bot?.Resolve?.Ledger?.Beware(BotGround.FireKind, _map, _smithy);

    /// <summary>
    /// The way to the forge turned out not to exist. Nothing to bend to — a forge is picked as the nearest
    /// known one, so choosing again would choose the same one — but the refusal is filed under the place's
    /// name so that the next choice is a different forge rather than this one again.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        Refuse(bot);

        return false;
    }

    private BotDoing Working(IBotWilful bot, Mobile body)
    {
        var tool = BotAnvil.Kit(body);

        if (tool == null)
        {
            // <b>A hammer worn out on the anvil is the end of a shift, not a failure, when the shift made
            // something.</b> Every "failed at forge ... no hammer" of 22.09.2026 had made two to four blades
            // first — "3 attempts, 2 made … 90 made" — and was counted with the smiths who made nothing.
            return _made > 0
                ? BotDoing.Done($"the hammer wore out after {_made} made")
                : BotDoing.Failed("no hammer");
        }

        if (!BotAnvil.AtASmithy(body))
        {
            _leg = Leg.Walk;

            return Walking(bot, body);
        }

        _recipe ??= _order == null ? BotAnvil.Choose(body) : BotAnvil.Recipe(body, _order.Kind);

        if (_recipe == null)
        {
            return BotDoing.Failed(_order == null ? "nothing worth making" : $"cannot make {_kind?.Name}");
        }

        _kind ??= _recipe.ItemType;

        // Counted out of the pack rather than believed from the attempts. See BotCraftwork.Swing: an attempt
        // is not an item, and the first version's tally said forty-four made about a smith that had made
        // nothing.
        // Counted before and after, because the difference is the only thing on the shard that says a swing
        // just worked — and that is the one moment the free craft may be asked for. See BotCraftwork.Bonus.
        var had = _made;

        // Only what the pack gained since the work began. See _had.
        if (_had < 0)
        {
            _had = BotAnvil.Made(body, _kind);

            if (_had > 0)
            {
                Preowned++;
            }
        }

        _made = Math.Max(0, BotAnvil.Made(body, _kind) - _had);

        if (_made > had)
        {
            BotCraftwork.Produced(_kind, _made - had);
            _made += BotCraftwork.Bonus(body, _kind);
            Book(_made - had);
        }

        if (_made > 0 && (_order != null || _swings >= MaxSwings))
        {
            _leg = Leg.Hand;

            return Handing(bot, body);
        }

        // <b>Its own iron back off the market before giving up for want of iron.</b> A smith lists the metal
        // it is not using, and then a recipe wants more than the pack holds — so the bot that owns exactly
        // the ingots it needs stands at the anvil and reports "out of metal", while the order it raises to
        // replace them is refused by the market on the grounds that it is selling them. This is the tailor's
        // leather, one trade along: see the note in BotSew.Buying. Reclaiming leaves the stall standing and
        // empty, so the price it learned survives.
        var cost = BotCraftwork.Cost(_recipe);

        // <b>Which metal, asked every beat rather than once.</b> A smith may start a piece in iron and finish
        // it in bronze because a delivery arrived — and it should, because the same swing in a better metal
        // is a better piece for nothing. See BotAnvil.Best: dearest the skill will take and the pack can
        // fill, falling back to iron.
        //
        // <b>Asked for a round's worth first, and only then for one piece's worth.</b> Best takes the
        // dearest metal that covers what it is given, so asking it for one piece is asking it to prefer the
        // pile it will exhaust: a smith with thirty iron and ten bronze was sent to beat an eight-ingot
        // cutlass out of the bronze, missed once, and reported "out of metal" standing on thirty iron. The
        // two numbers are one number — see BotAnvil.Tries, which is what Choose picks against as well.
        _metal = BotAnvil.Best(body, cost * BotAnvil.Tries);

        if (BotAnvil.Ingots(body, _metal) < cost * BotAnvil.Tries)
        {
            _metal = BotAnvil.Best(body, cost);
        }

        // Its own metal out of its own bank box, when the forge stands by a bank (01.10.2026: the box is reached only at a bank; the
        // stall this took its metal back off was reached from anywhere).
        if (_swings < MaxSwings && BotAnvil.Ingots(body, _metal) < cost && BotMarket.FromBank(bot, _metal, cost * BotAnvil.Tries) > 0)
        {
            logger.Information(
                "{Name} took its own {Metal} out of its bank box to make {Item}",
                body.Name,
                _metal.Name,
                _kind?.Name
            );
        }

        if (_swings >= MaxSwings || BotAnvil.Ingots(body, _metal) < cost)
        {
            if (_made > 0)
            {
                _leg = Leg.Hand;

                return Handing(bot, body);
            }

            Stints++;

            if (_swings >= MaxSwings)
            {
                Fruitless++;
            }
            else
            {
                RanOut++;
            }

            // With the engine's own last word, when it has one. "Nothing came of the iron" is what this bot can see;
            // why is something only the craft system knows, and until 21.09.2026 nothing could hear it say so.
            return BotDoing.Failed((_swings >= MaxSwings ? "nothing came of the iron" : "out of metal") + BotCraftEar.Why(body));
        }

        if (_swung && Core.TickCount - _swungTick < SwingMs)
        {
            return BotDoing.Work($"beating out {_kind?.Name}");
        }

        _swung = true;
        _swungTick = Core.TickCount;
        _swings++;

        _swungMetal = _metal;

        BotAnvil.Swing(body, _recipe, tool, _metal);

        return BotDoing.Work($"beating out {_kind?.Name}");
    }

    /// <summary>
    /// What was made, counted: what an order is waiting for, and the rest.
    ///
    /// <para>
    /// <b>Nothing leaves the bench any more (01.10.2026, Patrick's order of 30.09: every order hand to hand).</b> The piece made to
    /// somebody's order stays in the pack for the walk to whoever ordered it — the carrier's offer (<see cref="BotCourier"/>) comes
    /// next, and the order is answered first because it is the want the piece matches — and the rest waits for the porter: the bank
    /// box, the smith's own shift at the shop by Britain's bank, or a shopkeeper.
    /// </para>
    /// </summary>
    private BotDoing Handing(IBotWilful bot, Mobile body)
    {
        // Never what is bound to the bot: neither a want nor a stall will take it. See BotBinding.Refuses.
        var goods = BotAnvil.Gather(body, _kind);

        goods.RemoveAll(item => BotBinding.IsBound(item, bot?.Bond));

        if (goods.Count == 0)
        {
            Stints++;
            Fruitless++;

            return BotDoing.Done($"{_swings} attempts at {_kind?.Name} and nothing to show");
        }

        // Whoever wanted it — the order this was made for if there is one, and otherwise anybody who has said a want for the same
        // thing while the hammer was going: counted, and carried to them by the walk that comes next.
        var want = _order ?? BotMarket.Demand(bot, _kind);
        var made = 0;

        for (var i = 0; i < goods.Count; i++)
        {
            made += Math.Max(1, goods[i].Amount);
        }

        var filled = want == null ? 0 : Math.Min(made, want.Payable);
        var listed = made - filled;

        // The metal in the line (01.10.2026, Patrick's point 2): "2 PlateGorget of Bronze" is the only place a coloured piece is seen.
        var metals = Metals();

        logger.Information(
            "{Name} beat out {Made} {Item} of {Metal} in {Swings} attempts: {Filled} for somebody's order, to be carried to them, and {Listed} kept",
            body.Name,
            _made,
            _kind?.Name ?? "iron",
            metals,
            _swings,
            filled,
            listed
        );

        _handed = filled + listed;

        Stints++;
        Wrought++;
        Pieces += _made;

        return BotDoing.Done($"{_made} {_kind?.Name} of {metals} made, {filled} for an order and {listed} kept");
    }

    /// <summary>Pieces this beat found made, booked under the metal of the swing that made them. See <see cref="_swungMetal"/>.</summary>
    private void Book(int pieces)
    {
        if (pieces <= 0)
        {
            return;
        }

        var metal = _swungMetal ?? _metal ?? BotAnvil.Base;

        _byMetal ??= [];
        _byMetal[metal] = _byMetal.GetValueOrDefault(metal) + pieces;
        _piecesByMetal[metal] = _piecesByMetal.GetValueOrDefault(metal) + pieces;
    }

    /// <summary>The metal of this stint's pieces for its ending: "Bronze", or "Bronze (2) and Iron (1)" when a delivery changed it.</summary>
    private string Metals()
    {
        if (_byMetal == null || _byMetal.Count == 0)
        {
            return BotAnvil.Named(_swungMetal ?? _metal);
        }

        if (_byMetal.Count == 1)
        {
            foreach (var only in _byMetal.Keys)
            {
                return BotAnvil.Named(only);
            }
        }

        using var said = ValueStringBuilder.Create(64);

        var i = 0;

        foreach (var (metal, pieces) in _byMetal)
        {
            said.Append(i == 0 ? "" : i == _byMetal.Count - 1 ? " and " : ", ");
            said.Append(BotAnvil.Named(metal));
            said.Append(" (");
            said.Append(pieces);
            said.Append(')');
            i++;
        }

        return said.ToString();
    }

    /// <summary>Pieces beaten out since the world loaded by metal, iron first and the dearest last, with the coloured share.</summary>
    private static string ByMetal()
    {
        var metals = BotAnvil.System?.CraftSubRes;

        if (_piecesByMetal.Count == 0 || metals == null)
        {
            return "no piece beaten out yet";
        }

        using var said = ValueStringBuilder.Create(160);

        long all = 0;
        long coloured = 0;

        for (var i = 0; i < metals.Count; i++)
        {
            var metal = metals.GetAt(i);

            if (metal?.ItemType == null || !_piecesByMetal.TryGetValue(metal.ItemType, out var pieces))
            {
                continue;
            }

            all += pieces;

            if (metal.RequiredSkill > 0.0)
            {
                coloured += pieces;
            }

            if (said.Length > 0)
            {
                said.Append(", ");
            }

            said.Append(BotAnvil.Named(metal.ItemType));
            said.Append(' ');
            said.Append(pieces);
        }

        said.Append($" ({coloured} of {all} in a coloured metal)");

        return said.ToString();
    }

    public static string Describe() =>
        Stints == 0
            ? "no stint at an anvil has ended yet"
            : $"{Stints} stints at an anvil ended: {Wrought} with {Pieces} pieces beaten out, {RanOut} out of metal, "
              + $"{Fruitless} that used every swing and made nothing, {NoPlace} that found no anvil the engine would take, "
              + $"{Rounded} that walked round the forge to its anvil's side, {Preowned} that began with pieces of the kind already in the pack and left them out of what they made; "
              + $"pieces by metal: {ByMetal()}; hammers by the best metal their Blacksmithy opens: {BotAnvil.Hands()}; {BotCraftEar.Describe()}";

    public static void Forget()
    {
        Stints = 0;
        Wrought = 0;
        Pieces = 0;
        RanOut = 0;
        Fruitless = 0;
        NoPlace = 0;
        Rounded = 0;
        _piecesByMetal.Clear();
    }
}
