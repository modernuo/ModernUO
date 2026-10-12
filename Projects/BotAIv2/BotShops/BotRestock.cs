using System;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Going to a shop and buying what the bot has run out of.
///
/// <para>
/// <b>Maintenance is the one kind of work a takings-per-minute measure prices badly, and this is how it is
/// handled honestly.</b> Buying creates no wealth: coin becomes goods. So the takings are declared as what
/// was paid — <see cref="Made"/> equals the bill — which makes the trip come out at roughly nothing per
/// minute rather than at a loss. It is therefore never <em>punished</em> and never preferred over work that
/// actually produces something, which is exactly the right place for an errand to the shops.
/// </para>
///
/// <para>
/// The one number that does real work here is <see cref="Outlay"/>: it is what the decision layer measures
/// need against, so a bot that cannot afford its own bandages is a bot that feels short of money — and stops
/// feeling short the moment it can.
/// </para>
/// </summary>
public sealed class BotRestock : BotDeed
{
    /// <summary>The ledger's key. A kind of work, not one shop or one thing.</summary>
    public const string Trade = "restock";

    /// <summary>
    /// What an errand to the shops is reckoned at before experience corrects it. Low on purpose: it produces
    /// nothing, and the only reason to do it is that something has run out.
    /// </summary>
    public static double Prior { get; set; } = 12.0;

    /// <summary>How long the errand itself is expected to take once the bot is there.</summary>
    public static double WorkMinutes { get; set; } = 2.0;

    /// <summary>The shopkeeper, or, once the guild's shelf turned out bare, the one the errand went on to. See <see cref="FellThrough"/>.</summary>
    private BaseVendor _shop;

    /// <summary>Times this errand has been sent on to another counter because the shelf emptied. See BotShops.Next.</summary>
    private int _repicks;

    private readonly Type _wanted;

    /// <summary>What it is after. Read by <see cref="BotCraftFirst"/>: a crafter's restock of its own trade's material (03.10.2026, econ13).</summary>
    public Type Wanted => _wanted;

    private readonly int _amount;

    private int _price;

    /// <summary>
    /// Errands that found their guild's shelf bought out when they got to it and went on to a shopkeeper selling the
    /// thing instead of failing.
    /// </summary>
    public static long FellThrough { get; private set; }

    /// <summary>Working tools bound as they were bought, so that a death does not send the bot back for another.</summary>
    public static long ToolsBound { get; private set; }

    private int _bought;

    private int _paid;

    public BotRestock(BaseVendor shop, Type wanted, int amount, int price)
    {
        _shop = shop;
        _wanted = wanted;
        _amount = Math.Max(1, amount);
        _price = Math.Max(1, price);
    }

    /// <summary>
    /// The same errand again, at the bot's own guild's counter.
    ///
    /// <para>
    /// <b>Another place a bandage can come from, and the reason the hall was worth building.</b> A shopkeeper
    /// needs a walk to town, and this needs a walk across the guild's own yard — so it is where the errand lands
    /// whenever the guild has thought to stock the thing. The gold has already left the world, at the town counter,
    /// when the guild bought it; what changes hands here is one member paying its own guild, which is coin staying in
    /// the population.
    /// </para>
    ///
    /// <para>
    /// <b>What is bought is the lot, not the count, and the lot is looked up on arrival rather than held.</b>
    /// A shelf sells packets — twenty bandages tied together — and somebody else may have taken the packet
    /// while this bot was walking. Carrying the merchant and the kind instead of the item means the errand
    /// takes whatever the shelf has when it gets there, and only fails when the shelf is bare.
    /// </para>
    /// </summary>
    public BotRestock(PlayerVendor merchant, Type wanted, int amount, int price)
    {
        _merchant = merchant;
        _wanted = wanted;
        _amount = Math.Max(1, amount);
        _price = Math.Max(1, price);
    }

    private PlayerVendor _merchant;

    /// <summary>
    /// The same errand again, at another bot's shop by the bank (29.09.2026). See <c>BotShopkeep</c>.
    ///
    /// <para>
    /// <b>The market's place a supply comes from: both sides standing together.</b> The walk is to
    /// the keeper itself, followed rather than aimed at; the purchase moves the goods out of the keeper's bank box and pack into
    /// this bot's pack and the coin the other way (<c>BotShopkeep.Serve</c>). A shop that has shut or sold out by the time the
    /// bot arrives sends a supplies errand on to a shopkeeper, as the guild's bare shelf does. <paramref name="forWant"/> is a
    /// spoken want's errand (<c>BotPatron</c>): bought at the keeper's price, never over the want's offer, and the want comes down
    /// at the till by what was bought.
    /// </para>
    /// </summary>
    public BotRestock(BotStorefront front, Type wanted, int amount, int price, bool forWant = false)
    {
        _front = front;
        _wanted = wanted;
        _amount = Math.Max(1, amount);
        _price = Math.Max(1, price);
        _forWant = forWant;
    }

    private BotStorefront _front;

    private readonly bool _forWant;

    /// <summary>The recall a far counter may be reached by instead of the walk. See <see cref="BotRecall"/>.</summary>
    private BotRecall _recall;

    public override string Kind => Trade;

    public override Map Map => _shop?.Map ?? _merchant?.Map ?? _front?.Map;

    public override Point3D Where => _shop?.Location ?? _merchant?.Location ?? _front?.Location ?? Point3D.Zero;

    /// <summary>
    /// What this errand is claimed at, when the shopper's own price is the wrong one for it.
    ///
    /// <para>
    /// <b>An errand to the shops is priced to lose, on purpose, and that is right for every errand but one.</b>
    /// <see cref="Prior"/> is 12 a minute because buying creates nothing; a bot restocks when it has nothing better to
    /// do or cannot work without the thing. The band's order is neither: the keeper is an Architect whose own work
    /// pays hundreds a minute, so a trip to buy the band's bandages lost every auction it was ever entered in — Hale
    /// took no shopping errand at all in the first half hour of build 141 while the chest stood empty. The band's own
    /// errands are all claimed like this, above ordinary work and unpaid, because nobody is paying for them and they
    /// still have to happen. See BotFence.Prior, BotStash.Prior, BotFetch.Prior.
    /// </para>
    /// </summary>
    public double? Claim { get; set; }

    /// <summary>
    /// What a trip for the next tool is claimed at, bought while the last one still works. See <c>BotOutfit.SpareAt</c>.
    ///
    /// <para>
    /// <b>Above every trade's own price and below everything that is not a trade.</b> At <see cref="Prior"/> the trip
    /// loses the auction to the dig it keeps going — 12 a minute against 30 to 150 — so the tool's end came inside the
    /// dig, and the dig failed for it. A hundred and eighty is over the priors of digging (45), smithing (120), sewing
    /// (55) and fletching (90), and under a rescue, a brawl or a stake (200) and a rally (400): the one purchase that
    /// comes before the next job, and never before a friend in trouble.
    /// </para>
    /// </summary>
    public static double Spare { get; set; } = 180.0;

    public override double Expects => Claim ?? Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Nothing. Handing coin over a counter teaches a bot nothing at all.</summary>
    public override SkillName? Trains => null;

    /// <summary>
    /// What it will cost. The one number the decision layer measures need against — a want's walk to a bot's shop too, since a
    /// want holds no gold any more (01.10.2026): the buyer pays at the till out of its own pack and account.
    /// </summary>
    public override int Outlay => _amount * _price;

    /// <summary>
    /// At a shopkeeper's counter, when that is where it is buying — or at a bot's shop, whose goods come into the pack the same
    /// way and bounce off a pack that is full (29.09.2026). See <see cref="BotDeed.AtCounter"/>.
    /// </summary>
    public override bool AtCounter => _shop != null || _front != null;

    /// <summary>Not a penny comes back. This is the spending half of a living.</summary>
    public override double Coin => 0.0;

    /// <summary>
    /// Not about money, and not to be refused for failing to earn any — which is what the class note above
    /// always meant, and what the ledger quietly stopped honouring.
    ///
    /// <para>
    /// <b>"Never punished" held on paper and not in the auction.</b> The note says a trip to the shops comes out at
    /// about nothing a minute because the goods are booked at what they cost; in practice a stall that sold for
    /// more than its worth, a lot shared with somebody else's order, or a packet bought at a counter whose price had
    /// moved all settle below nought, and the ledger then says so about the place for good. On the morning of
    /// 14.09.2026 the auction refused this errand 721 times in 108 minutes with "restock is expected to pay" a
    /// negative amount, while "nothing to write with", "nothing to brew with" and "nothing to dig with" were among
    /// the commonest failures on the island. Supplies are the preconditions of the trades that pay; a planner would
    /// value them by what they enable, and the least this can do is stop pricing them by what they cost.
    /// </para>
    ///
    /// <para>
    /// Unpaid work is priced at its typed claim — twelve a minute, still below anything productive — and is gated by
    /// its proposer, which only offers the errand when something the class needs has actually run out. The outlay
    /// check is untouched: a bot that cannot afford the packet is still refused before any of this.
    /// </para>
    /// </summary>
    public override bool Unpaid => true;

    /// <summary>
    /// Put down for a flight or a rescue and taken up again after, rather than dropped: a bot short of bandages is
    /// still short of them when the fight is over. Seven of the ten restock drops in the first quarter hour of build
    /// 269 were "interrupted by flee" or "interrupted by rescue". No clock inside, so nothing to put right on resuming.
    /// </summary>
    public override bool Steadfast => true;

    /// <summary>Goods are worth what they cost, so the errand comes out at about nothing rather than a loss.</summary>
    public override int Made => _paid;

    public override string Stage => _bought > 0 ? $"bought {_bought} {_wanted?.Name}" : $"after {_amount} {_wanted?.Name}";

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
        // <b>The guild's own counter learns the same lesson under the same name.</b> The hall branch came after 26.08.2026
        // and this returned early for it, so a hall whose door could not be reached wrote nothing the chooser reads: Edda
        // Ashdown was sent to The Needle's counter six times in two seconds on 14.09.2026, "no way through to Zane inside
        // a house", until the breaker rested the work. See BotShopper, which now asks this caution of the hall as well.
        if (_merchant != null)
        {
            bot?.Resolve?.Ledger?.Beware(BotShops.ShopKind, _merchant.Map, _merchant.Location);

            return false;
        }

        // A bot's shop learns it under its own word at its pitch, which is the word BotShopkeep.Best asks in.
        if (_front != null)
        {
            bot?.Resolve?.Ledger?.Beware(BotShopkeep.Kind, _front.Map, _front.Location);

            return false;
        }

        if (_shop == null)
        {
            return false;
        }

        bot?.Resolve?.Ledger?.Beware(BotShops.ShopKind, _shop.Map, _shop.Location);

        return false;
    }

    /// <summary>Whether the cloth bought is cut into bandages the moment it is paid for. See <c>BotShopper.CutsBandages</c>.</summary>
    public bool Cuts { get; set; }

    /// <summary>Bandages cut at the counter out of cloth bought instead of them.</summary>
    public static long CutInto { get; private set; }

    /// <summary>Cuts what was bought, if it is cloth for bandages, and says so.</summary>
    private string Cut(IBotWilful bot, string done)
    {
        if (!Cuts || _wanted != typeof(Cloth) || _bought <= 0)
        {
            return done;
        }

        var made = BotWeave.Bandages(bot?.Self, _bought);
        CutInto += made;

        return made > 0 ? $"{done}, cut into {made} bandages" : done;
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        // A bot's shop by the bank (29.09.2026): walk to the keeper and buy at its till. See BotShopkeep.Visit. A shop gone or
        // sold out sends a supplies errand on to a shopkeeper, as the guild's bare shelf does below, at that counter's price.
        if (_front != null)
        {
            var visit = BotShopkeep.Visit(bot, _front, _wanted, _amount, _price, _forWant, out var got, out var cost, out var onward);

            if (onward != null)
            {
                FellThrough++;

                _front = null;
                _shop = onward;
                _price = Math.Max(1, BotShops.Price(onward, _wanted));

                return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(BotShops.CounterReach), $"on to {_shop.Name} for {_wanted?.Name}");
            }

            if (got <= 0)
            {
                return visit;
            }

            _bought = got;
            _paid = cost;

            (bot as BotMobile)?.Rearm();

            return BotDoing.Done(Cut(bot, visit.Note));
        }

        if (_merchant != null)
        {
            if (_merchant.Deleted || _merchant.Map == null || _merchant.Map == Map.Internal)
            {
                return BotDoing.Failed("the guild's merchant is gone");
            }

            if (!body.InRange(_merchant.Location, BotShelf.Reach))
            {
                return BotDoing.Walk(
                    _merchant.Map,
                    _merchant,
                    BotArrival.Within(BotShelf.Reach),
                    $"to the counter of {body.Guild?.Name ?? "the guild"}"
                );
            }

            var lot = BotShelf.Offer(_merchant, _wanted, out var asking);

            if (lot == null)
            {
                // <b>Bought out while it walked, and a shopkeeper still sells it.</b> The guild's shelf is the middle
                // place a supply can come from; members share it, and on build 45 eight errands after sulphurous ash
                // reached their counter to find the last packet gone — Ulwin twice, Bertram twice — and each bot took
                // the same errand again a second later, now to a shopkeeper. Going on there is that second errand
                // without the failure. The price is read again at the new counter, which is what the note on Bend asks
                // of any swap. Once: from a shopkeeper the errand ends as it always did. See BotShops.Next.
                var next = BotShops.Nearest(bot, _wanted);

                if (next != null)
                {
                    FellThrough++;

                    _merchant = null;
                    _shop = next;
                    _price = Math.Max(1, BotShops.Price(next, _wanted));

                    return BotDoing.Walk(
                        _shop.Map,
                        _shop,
                        BotArrival.Within(BotShops.CounterReach),
                        $"on to {_shop.Name} for {_wanted?.Name}"
                    );
                }

                return BotDoing.Failed($"the guild's shelf has no {_wanted?.Name} left on it");
            }

            _bought = BotShelf.Take(body, _merchant, lot, out var declined);

            if (_bought <= 0)
            {
                return BotDoing.Failed(declined ?? "the guild's merchant would not sell it");
            }

            _paid = asking;

            (bot as BotMobile)?.Rearm();

            return BotDoing.Done(Cut(bot, $"{_bought} {_wanted?.Name} off the guild's own counter for {_paid}gp"));
        }

        if (_shop == null || _shop.Deleted || _shop.Map == null || _shop.Map == Map.Internal)
        {
            return BotDoing.Failed("the shopkeeper is gone");
        }

        if (!body.InRange(_shop.Location, BotShops.CounterReach))
        {
            // Runes (29.09.2026): a far counter is recalled to where a mage holds a rune near it; a failed cast walks. See BotRecall.
            if (BotRecall.Instead(bot, _shop.Map, _shop.Location, BotShops.CounterReach, _shop.Name, ref _recall) is { } cast)
            {
                return cast;
            }

            // The distance the work itself asks for, on the line above. See BotArrival.Beside.
            // Followed rather than aimed at: a shopkeeper wanders. See BotPeddle for the whole reason.
            return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(BotShops.CounterReach), $"to {_shop.Name}");
        }

        _bought = BotShops.Buy(bot, _shop, _wanted, _amount, out var refused);

        if (_bought <= 0)
        {
            // The shelf emptied by whoever got here first: on to the next counter that has it, at its own price. See
            // BotShops.Next; 15 restocks failed "the shelf holds no …" on 16.09.2026.
            var next = BotShops.Next(bot, _shop, _wanted, ref _repicks);

            if (next != null)
            {
                _shop = next;
                _price = Math.Max(1, BotShops.Price(next, _wanted));

                return BotDoing.Walk(next.Map, next, BotArrival.Within(BotShops.CounterReach), $"on to {next.Name} for {_wanted?.Name}");
            }

            // Standing at the counter with nothing to show for it: sold out, priced out, or refused. All
            // three are the shop's business rather than the bot's — but which of the three it was is this
            // log's business, and lumping them cost a night. See the note on the overload above.
            return BotDoing.Failed(refused ?? "the shop would not sell it");
        }

        _paid = _bought * _price;

        // What the town's bots buy over a counter is the trade a bot's shop could have had. See BotShopBook.
        BotShopBook.Counter(body, _wanted, _bought, _paid);

        // <b>A working tool bought is bound as the kit's was (30.09.2026, build 349).</b> Binding keeps a tool through death,
        // and only what birth handed out was bound: scissors joined every class's tools on 29.09, so the bots born before
        // bought theirs, lost them at the next death with the corpse, and walked out to buy them again — "died doing
        // restock: after 1 Scissors" six times each for Sylvaris, Mardred and Liftrasa over 344–346 and six of the nine
        // restock deaths of 348's first quarter hour. A tool on the buyer's own list (BotOutfit.ToolsFor), bought singly,
        // is bound on the spot: weightless, kept through death, not merchandise — what the kit's would have been.
        if (_bought == 1 && _wanted != null && body is BotMobile { Bond: { } bond, Class: { } klass } buyer
            && BotOutfit.ToolsFor(klass).Contains(_wanted) && buyer.Backpack?.FindItemByType(_wanted) is { } tool
            && !BotBinding.IsBound(tool, bond))
        {
            BotBinding.Bind(tool, bond);
            ToolsBound++;
        }

        // Straight on, if it is something to wear or wield.
        //
        // <b>The same hole death opened, dug by shopping.</b> A blade wears through and is bought again — and
        // the new one lands in the pack, where the old one never was. Without this a bot walks out of the shop
        // it just spent its takings in and goes back to fighting with its fists, carrying the sword.
        (bot as BotMobile)?.Rearm();

        return BotDoing.Done(Cut(bot, $"{_bought} {_wanted?.Name} for {_paid}gp"));
    }
}
