using System;
using Server.Engines.Craft;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Buy paper, write scrolls, keep what the book is short of and sell the rest. The scribe's chain.
///
/// <para>
/// <b>This is the first work in the project whose output only another bot can buy.</b> A mage vendor sells
/// the first three circles and nothing above them, so every scroll from the fourth circle up that exists on
/// this shard was written by somebody. Mining put metal out that nothing needed; sewing bought its cloth from
/// a shelf. This trade sits on both sides of a market made of bots: it buys herbs and paper from shopkeepers
/// and sells the one thing shopkeepers cannot supply.
/// </para>
///
/// <para>
/// <b>Filling its own book costs it nothing and is decided here, not priced anywhere.</b> A scroll it has
/// just written is worth what the market says; whether it keeps it or sells it does not change what it
/// produced. So the first one of a kind goes into its own book if the book lacks it, and every further one is
/// sold — and "collecting all the spells" turns out to be what happens to a scribe who gets good at writing,
/// rather than a goal anybody had to give it a price for.
/// </para>
///
/// <para>
/// <b>Mana is the throttle, and it is the engine's.</b> Four for the first circle, fifty for the eighth. A
/// mage with fifty Intelligence writes one top-circle scroll and then has to sit down, which is why this trade
/// and Meditation are on the same vector.
/// </para>
/// </summary>
public sealed class BotInscribe : BotDeed
{
    /// <summary>The ledger's key.</summary>
    public const string Trade = "inscribe";

    /// <summary>
    /// What a session at the pen is reckoned at per minute before experience corrects it.
    ///
    /// A little above the needle's fifty-five, and for a stated reason rather than a feeling: it is the same
    /// on-vector skill at full rate, the materials cost more, and what comes off it asks more. It is also the
    /// number most likely to be wrong, because the real pace is set by mana coming back — and that is exactly
    /// what the ledger is for.
    /// </summary>
    public static double Prior { get; set; } = 60.0;

    /// <summary>How long a session is expected to take.</summary>
    public static double WorkMinutes { get; set; } = 6.0;

    /// <summary>
    /// What a session written for somebody's want is reckoned at over practice: the same 1.6 the forge, the needle and the
    /// fletcher put on an order (<c>BotForge.Expects</c>). The money is real and reserved, and it is the difference between
    /// writing what will certainly be bought and writing what might be.
    /// </summary>
    public static double OrderLift { get; set; } = 1.6;

    /// <summary>How many blank scrolls a practice session buys in one go, and the most a session to order buys.</summary>
    public static int Batch { get; set; } = 20;

    /// <summary>How often an attempt is made. The engine's craft has its own timer besides this.</summary>
    public static int SwingMs { get; set; } = 3000;

    /// <summary>
    /// How many attempts may pass with nothing whatever changing before the session is given up.
    ///
    /// Generous, because a run of failed skill checks is ordinary and does consume blanks — what this catches
    /// is the other thing entirely: attempts that are not happening at all.
    /// </summary>
    public static int Patience { get; set; } = 10;

    /// <summary>
    /// How long a scribe will sit waiting for mana before it takes what it has written and goes.
    ///
    /// A session that stalls on an empty pool must end rather than wait for ever: the work is still finished
    /// and still measured, and a bot that has run itself dry has learned something the ledger should see.
    /// </summary>
    public static int PatienceMs { get; set; } = 60000;

    private enum Leg
    {
        Shop,
        Work,
        Market
    }

    private BaseVendor _shop;

    /// <summary>Times this errand has been sent on to another counter because the shelf emptied. See BotShops.Next.</summary>
    private int _repicks;

    private readonly int _price;

    private Leg _leg;

    private CraftItem _recipe;

    private Type _kind;

    private int _worth;

    private int _had;

    private int _scrolls;

    private int _swings;

    /// <summary>Attempts since anything at all changed — a scroll appeared, or a blank was spent.</summary>
    private int _fruitless;

    private int _blanks = -1;

    private int _kept;

    /// <summary>How many written scrolls have been dealt with — booked, sold or listed. See <see cref="Made"/>.</summary>
    private int _placed;

    private int _sold;

    private int _made;

    private bool _swung;

    private long _swungTick;

    private long _restingTick;

    /// <summary>What this session is for and what it buys first. See <see cref="BotQuillPlan"/>.</summary>
    private readonly BotQuillPlan _plan;

    /// <summary>Where the scribe stood when the session was chosen: the place of a session that buys nothing.</summary>
    private readonly Map _map;

    private readonly Point3D _here;

    /// <summary>Whether the first attempt's bookkeeping has been done. The recipe comes with the plan now, so it cannot be the sign.</summary>
    private bool _started;

    public BotInscribe(BaseVendor shop, int price, BotQuillPlan plan, Mobile scribe)
    {
        _shop = shop;
        _price = Math.Max(1, price);
        _plan = plan;
        _recipe = plan?.Recipe;
        _kind = plan?.Kind;
        _worth = plan?.Worth ?? 0;
        _map = scribe?.Map;
        _here = scribe?.Location ?? Point3D.Zero;
    }

    public override string Kind => Trade;

    /// <summary>Whether this session goes to the counter at all. A plan that buys nothing is written where the scribe stands.</summary>
    private bool Buys => _plan?.Buys ?? true;

    public override Map Map => Buys ? _shop?.Map : _map;

    /// <summary>
    /// The shop, for the whole life of the work, when there is paper or a herb to buy: the one fixed place in the chain. A
    /// session that buys nothing is written where the scribe stands (03.10.2026): it was priced as a walk to the counter it
    /// never made, and a pen needs no place.
    /// </summary>
    public override Point3D Where => Buys ? _shop?.Location ?? Point3D.Zero : _here;

    /// <summary>A session for somebody's want is an order; see <see cref="OrderLift"/>.</summary>
    public override double Expects => _plan?.For != null ? Prior * OrderLift : Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Inscribe, and it is on the mage's own vector — which is what makes this worth its time.</summary>
    public override SkillName? Trains => SkillName.Inscribe;

    /// <summary>
    /// What the session buys: the paper and the herbs its plan names, at the counter it walks to (<see cref="BotQuill.Plan"/>).
    /// It was twenty blanks whatever the session was for, held against a scribe with ten in its pack; see BotQuill.Supply.
    /// </summary>
    public override int Outlay => _plan?.Outlay ?? Batch * _price;

    /// <summary>Its blank scrolls are bought over a shopkeeper's counter. See <see cref="BotDeed.AtCounter"/>.</summary>
    public override bool AtCounter => Buys && _shop != null;

    /// <summary>
    /// For somebody's want, the whole of it: the money is reserved when the want is said (build 391) and paid at the meeting,
    /// and a scribe that needs coin should see that a paid scroll pays it (03.10.2026, as the forge's order does). Otherwise
    /// nothing is promised in coin — a practice scroll may end up filling a want, but at the moment the work is chosen that
    /// is a possibility rather than a plan.
    /// </summary>
    public override double Coin => _plan?.For != null ? 1.0 : 0.0;

    /// <summary>Whether this session is being written for that want. See <see cref="BotScribes.ClaimedByOther"/>.</summary>
    public bool Serves(BotWant want) => want != null && ReferenceEquals(_plan?.For, want) && _leg != Leg.Market;

    /// <summary>Counted as taken, and a want it writes for is claimed, so the next scribe asked writes another.</summary>
    public override void Taken(IBotWilful bot)
    {
        BotScribes.Took(_plan);

        if (_plan?.For != null)
        {
            BotScribes.Claim(_plan.For, bot);
        }
    }

    /// <summary>
    /// What was produced and <em>not</em> sold: scrolls kept for the book and scrolls left on a stall, and
    /// the ones written but not yet placed.
    ///
    /// A scroll that filled a want is paid for in coin the moment it is handed over, and the coin is already
    /// in the takings — counting it here as well would pay the scribe twice for one scroll.
    ///
    /// <para>
    /// <b>The last clause is why a dropped session used to read as a catastrophe.</b> Everything was counted
    /// in the placing leg, so a scribe whose work the auction chose against mid-page had bought paper, made
    /// scrolls, and declared nothing for them: on 02.09.2026 Calla 2 settled a dropped inscribe at "-100 in
    /// 1.3 min (-79/min): -100 coin, 0 made" with four scrolls in its pack, and Cedric at -198/min with
    /// four more. The scrolls were real and still in hand; the ledger was taught that writing loses money.
    /// Unplaced work is counted here at the price it will be asked for and taken back out as it is placed,
    /// so nothing is counted twice.
    /// </para>
    /// </summary>
    public override int Made => _made + BotGlut.Yield(_kind, Math.Max(0, _scrolls - _placed), _worth);

    public override string Stage => _leg switch
    {
        Leg.Shop => _plan?.Herbs is { Count: > 0 } ? "after paper and herbs" : "after paper",
        Leg.Work => $"writing {_kind?.Name ?? "something"}{For} ({_swings} attempts, {_scrolls} written)",
        _ => $"placing {_scrolls}"
    };

    /// <summary>Whose want, or which of its own, in the stage. Empty for practice.</summary>
    private string For =>
        _plan?.For?.Buyer?.Self is { } buyer ? $" for {buyer.Name}'s want" : _plan?.Own == true ? " for itself" : "";

    /// <summary>
    /// The way to the shopkeeper turned out not to exist.
    ///
    /// <para>
    /// <b>Nothing to bend to here, and something to write down — and it was the writing down that was
    /// missing.</b> What this undertaking carries was priced against <em>this</em> shopkeeper, so swapping in
    /// another one mid-errand would carry a stale price; failing is the honest answer. But the failure has to
    /// be filed under the <em>place's</em> name, because that is the word the shop lookup asks in. Filed under
    /// the undertaking's name — which is all <c>BotWill.Settle</c> can do — it is written and never read, and
    /// the next beat picks the same unreachable shopkeeper on distance alone. Calla walked at Gus thirty-one
    /// times in an hour on 26.08.2026 that way.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        if (_shop == null)
        {
            return false;
        }

        bot?.Resolve?.Ledger?.Beware(BotShops.ShopKind, _shop.Map, _shop.Location);

        return false;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        for (var guard = 0; guard < 6; guard++)
        {
            var doing = _leg switch
            {
                Leg.Shop => Shopping(bot, body),
                Leg.Work => Writing(bot, body),
                _ => Placing(bot, body)
            };

            if (doing.Kind != BotDoingKind.None)
            {
                return doing;
            }
        }

        return BotDoing.Failed("could not settle on a next step");
    }

    private BotDoing Shopping(IBotWilful bot, Mobile body)
    {
        // Asked again at the counter rather than trusted from the proposal: a fight in between burns herbs, and a pack may
        // have been emptied by a death or filled by a porter. See BotQuill.Supply.
        var paper = PaperWanted(body);

        if (paper <= 0 && !HerbsLacking(body))
        {
            _leg = Leg.Work;

            return default;
        }

        if (_shop == null || _shop.Deleted || _shop.Map == null || _shop.Map == Map.Internal)
        {
            return BotDoing.Failed("the mage's shop is gone");
        }

        if (!body.InRange(_shop.Location, BotShops.CounterReach))
        {
            // The distance the work itself asks for, on the line above. See BotArrival.Beside.
            // Followed rather than aimed at: a shopkeeper wanders. See BotPeddle for the whole reason.
            return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(BotShops.CounterReach), $"to {_shop.Name} for blank scrolls");
        }

        if (paper > 0 && BotShops.Buy(bot, _shop, typeof(BlankScroll), paper, out var refused) <= 0)
        {
            // The shelf emptied by whoever got here first: on to the next counter with blank scrolls. See BotShops.Next.
            // 18 inscriptions failed "the shelf holds no BlankScroll at any price" on 16.09.2026, four of them by three
            // scribes inside 37 seconds at 23:07 on build 97.
            var next = BotShops.Next(bot, _shop, typeof(BlankScroll), ref _repicks);

            if (next != null)
            {
                _shop = next;

                return BotDoing.Walk(next.Map, next, BotArrival.Within(BotShops.CounterReach), $"on to {next.Name} for blank scrolls");
            }

            // The fourth and last counter in the project. All four said something different and none of them
            // said which of the six ways a purchase can fail had happened.
            return BotDoing.Failed(refused ?? "no blank scrolls to be had");
        }

        BuyHerbs(bot, body);

        _leg = Leg.Work;

        return default;
    }

    /// <summary>
    /// Blank scrolls to buy now. Practice as it always was: a batch when the pack holds none, nothing when it holds some. To
    /// order: the attempts the plan reckoned, less what the pack holds.
    /// </summary>
    private int PaperWanted(Mobile body)
    {
        var held = BotQuill.Blanks(body);

        if (_plan?.Order != true)
        {
            return held > 0 ? 0 : Math.Max(1, Batch);
        }

        return Math.Max(0, _plan.Attempts - held);
    }

    /// <summary>Whether a session to order is short of a herb its attempts burn. Practice buys no herbs.</summary>
    private bool HerbsLacking(Mobile body)
    {
        var pack = body?.Backpack;
        var resources = _recipe?.Resources;

        if (_plan?.Order != true || pack == null || resources == null)
        {
            return false;
        }

        for (var i = 0; i < resources.Count; i++)
        {
            var res = resources[i];

            if (res.ItemType != null && res.ItemType != typeof(BlankScroll)
                && pack.GetAmount(res.ItemType) < _plan.Attempts * Math.Max(1, res.Amount))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The herbs a session to order burns, bought at the paper's counter (03.10.2026). A herb the shelf does not hold is left:
    /// the writing stops when the pack is short of it, as it always has, and the session ends at the market with what it wrote.
    /// </summary>
    private void BuyHerbs(IBotWilful bot, Mobile body)
    {
        var pack = body?.Backpack;
        var resources = _recipe?.Resources;

        if (_plan?.Order != true || pack == null || resources == null)
        {
            return;
        }

        for (var i = 0; i < resources.Count; i++)
        {
            var res = resources[i];

            if (res.ItemType == null || res.ItemType == typeof(BlankScroll))
            {
                continue;
            }

            var lacking = _plan.Attempts * Math.Max(1, res.Amount) - pack.GetAmount(res.ItemType);

            if (lacking > 0)
            {
                BotScribes.Bought(BotShops.Buy(bot, _shop, res.ItemType, lacking));
            }
        }
    }

    /// <summary>How many of this kind the bot keeps for itself (<see cref="BotUnload.Keeps"/>): nought when none.</summary>
    private int Allowed(IBotWilful bot) =>
        _kind != null && BotUnload.Keeps(bot).TryGetValue(_kind, out var allowed) ? Math.Max(0, allowed) : 0;

    /// <summary>Whether this batch ended because the pen wore through. Said in the ending, which is where it is counted.</summary>
    private bool _worn;

    private BotDoing Writing(IBotWilful bot, Mobile body)
    {
        var pen = BotQuill.Pen(body);

        // Only before the first attempt: after it, a pen that is gone is a pen that wore through. See below.
        if (pen == null && _swings == 0)
        {
            return BotDoing.Failed("nothing to write with");
        }

        // The recipe comes with the plan (03.10.2026): the session writes what it was chosen for, and what it bought the herbs
        // for. Chosen here only for a session made without one.
        if (!_started)
        {
            if (_recipe == null)
            {
                var plan = BotQuill.Plan(bot, _shop, _price);

                if (plan == null)
                {
                    return BotDoing.Failed("no spell it can write with the herbs it has");
                }

                _recipe = plan.Recipe;
                _kind = plan.Kind;
                _worth = plan.Worth;
            }

            _started = true;
            _had = BotQuill.Held(body, _kind);
        }

        // What the last attempt produced, counted before the next one is made. A failure produces nothing and
        // is supposed to: that is what the blank and the herbs are paying for.
        var have = BotQuill.Held(body, _kind);

        if (have > _had)
        {
            _scrolls += have - _had;
            BotCraftwork.Produced(_kind, have - _had);
            _had = have;
        }

        // Written to order: the order is written, and what paper and herbs are left stay for the next session.
        if (_plan?.Order == true && _scrolls >= Math.Max(1, _plan.Units))
        {
            _leg = Leg.Market;

            return default;
        }

        // <b>A pen that wears through ends the batch, not the errand.</b> The engine gives a pen 25 to 75 uses and
        // destroys it on the last, so a scribe at speed loses one every page or two — and this said "nothing to write
        // with" and failed, with the scrolls just written still in the pack and unplaced. Ilsa ended eleven
        // inscriptions that way in build 41's 45 minutes, carrying up to 449 of scrolls; each failure taught the
        // ledger that writing fails, and the scrolls waited for some later errand to be sold. The last attempt is let
        // land first, since a craft resolves a moment after the swing, and then what was written goes to market
        // exactly as it does when the paper runs out.
        if (pen == null)
        {
            if (_swung && Core.TickCount - _swungTick < SwingMs)
            {
                return BotDoing.Work("writing");
            }

            _worn = true;
            _leg = Leg.Market;

            return default;
        }

        // To order, without the handful kept back to fight with — but only for the attempts the plan reckoned. Past them (a run of
        // bad rolls with a batch of paper in the pack) the handful is kept again, as practice keeps it: an order may dip the herbs
        // a few under it, never write the pack bare (the review of this build). See BotQuill.Stocked.
        var reserve = _plan?.Order == true && _swings < _plan.Attempts ? 0 : BotQuill.Reserve;

        if (BotQuill.Blanks(body) <= 0 || !BotQuill.Stocked(body, _recipe, reserve))
        {
            _leg = Leg.Market;

            return default;
        }

        // The engine refuses the attempt outright below the cost, so this is asked before swinging rather
        // than discovered from a message nobody reads.
        if (body.Mana < _recipe.Mana)
        {
            if (_restingTick == 0)
            {
                _restingTick = Core.TickCount;
            }
            else if (Core.TickCount - _restingTick >= PatienceMs)
            {
                _leg = Leg.Market;

                return default;
            }

            return BotDoing.Work("resting for mana");
        }

        _restingTick = 0;

        if (_swung && Core.TickCount - _swungTick < SwingMs)
        {
            return BotDoing.Work("writing");
        }

        // Nothing is changing hands.
        //
        // <b>An unbounded "still writing" is the one shape of state this project refuses to have, and it had
        // one.</b> The engine's craft can decline silently — the action lock is held, the skill check comes
        // out at nil, a resource is not where it expected — and it declines by returning, with a message to a
        // client that a bot does not have. So a mage born with twenty blanks never walks to a shop, sits down
        // to write, and stays there: no scroll is produced, no blank is spent, no mana is drawn, and there is
        // nothing in the log at all. Two mages spent sixteen minutes doing that in plain sight.
        var blanks = BotQuill.Blanks(body);

        if (_blanks >= 0 && blanks == _blanks && have == _had)
        {
            if (++_fruitless >= Patience)
            {
                _leg = Leg.Market;

                return BotDoing.Done(
                    $"wrote nothing in {_swings} attempts — {blanks} blanks, {body.Mana} mana, {_recipe.Mana} needed"
                );
            }
        }
        else
        {
            _fruitless = 0;
        }

        _blanks = blanks;

        _swings++;
        _swung = true;
        _swungTick = Core.TickCount;

        BotQuill.Swing(body, _recipe, pen);

        return BotDoing.Work("writing");
    }

    /// <summary>
    /// Where each scroll goes, in the order that makes the trade worth having.
    ///
    /// <para>
    /// Its own book first, and only for the first one of a kind — after that the book already has the spell
    /// and the question does not arise. The rest stays in the pack (01.10.2026, every sale hand to hand): what somebody's
    /// want waits for is carried to it by the walk that comes next (<see cref="BotCourier"/>), and the rest goes with the porter
    /// to the bank box, to be sold at the scribe's shift by the bank or to a shopkeeper. Counted as made either way.
    /// </para>
    /// </summary>
    private BotDoing Placing(IBotWilful bot, Mobile body)
    {
        if (_scrolls <= 0)
        {
            // The paper is gone and nothing came of it. Finished rather than failed: the attempts were real,
            // the skill checks happened, and what it cost is what learning a trade costs.
            return BotDoing.Done($"{_swings} attempts, nothing came of it{(_worn ? ", the pen worn through" : "")}");
        }

        var written = BotQuill.Gather(body, _kind);

        // <b>Written for somebody's want, the want comes first (03.10.2026).</b> The book took the first of a kind it lacked,
        // and a session sized to one scroll for a healer's line would have written it into the scribe's own book and left the
        // line standing. The book takes one only of what the pack holds beyond the want and beyond what the bot keeps of the
        // kind (BotUnload.Keeps), which the carrier never walks. Practice and its own orders: the book first, as always.
        var keeps = Allowed(bot);
        var order = _plan?.For is { IsOpen: true } open && BotMarket.Stands(open) ? open : null;
        var spare = order == null
            ? _scrolls
            : keeps >= int.MaxValue ? 0 : Math.Max(0, BotHandover.Carried(bot, _kind) - keeps - order.Payable);

        for (var i = 0; i < written.Count; i++)
        {
            var scroll = written[i];

            // Writing one into the book takes <em>one</em> off the stack. The rest of the stack is still a
            // stack of scrolls and still has to be placed — falling through to the market here rather than
            // going round the loop is the difference between selling two and quietly carrying them for ever.
            if (spare > 0 && BotGrimoire.Write(body, scroll))
            {
                spare--;
                _kept++;
                _placed++;
                _made += _worth;

                // It wrote the thing it was queueing for. Whatever was standing on the market asking for it
                // is asking for nothing, and the money behind it comes back.
                BotMarket.Withdrawn(bot, _kind, BotWithdraw.Have);
            }

            if (scroll.Deleted || scroll.Amount <= 0)
            {
                continue;
            }

            var left = scroll.Amount;

            // The island's price for what somebody asks for, the surplus price for the rest (01.10.2026). A scroll written into
            // the bot's own book above is counted whole: it is used, not sold. See BotGlut.Yield.
            _made += BotGlut.Yield(_kind, left, _worth);
            _placed += left;
        }

        // <b>For somebody's want: only what the carrier will walk (03.10.2026, the review of this build).</b> This counted every
        // scroll a want matched, so a scroll the bot's own keep absorbed read "for somebody's want" and was never carried. Now the
        // pack's surplus over the keep, up to what the want asks; the line is answered when it is handed over, which is counted
        // where it happens (BotScribes.Handed, from BotMarket.Received).
        var surplus = keeps >= int.MaxValue ? 0 : Math.Max(0, BotHandover.Carried(bot, _kind) - keeps);
        var asked = order ?? BotMarket.Demand(bot, _kind);

        _sold = asked == null ? 0 : Math.Min(surplus, asked.Payable);

        BotScribes.Wrote(_plan, _sold, _scrolls);

        return BotDoing.Done($"{_scrolls} {_kind?.Name} in {_swings} attempts, {_kept} into its own book, {_sold} for somebody's want, to be carried to it{(For.Length > 0 ? "; the session written" + For : "")}{(_worn ? ", the pen worn through" : "")}");
    }
}
