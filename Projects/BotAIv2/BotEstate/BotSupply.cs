using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Fetching a batch of something the population keeps running out of and leaving it on the guild's own
/// counter, so that the other nine members do not each walk to Britain for it.
///
/// <para>
/// <b>One journey instead of nine, and that is the whole of the argument.</b> The gold leaves the world at
/// the town counter either way — this buys at the same shelf, at the same price, that a shopper would have
/// bought at — so nothing here is a faucet and nothing here is a saving in coin. What it saves is the
/// island: three percent of the population's waking minutes were the walk to Britain and back, one bot at a
/// time, for a packet of bandages.
/// </para>
///
/// <para>
/// <b>What it fetches is measured rather than listed.</b> <see cref="BotShopper"/> has been keeping a tally
/// of what bots turn out to be short of since the day it was given counters; this reads that tally and
/// stocks the top of it. So a guild of archers fills its hall with arrows and a guild of casters fills it
/// with reagents, without anybody writing down which is which, and a class added tomorrow is provided for
/// the day after.
/// </para>
///
/// <para>
/// <b>The guild pays and the guild owns it.</b> Like the hall, the benches and the contract, the money comes
/// out of the members' purses through <c>BotGuilds.Stand</c> and the goods belong to all of them —
/// <see cref="Outlay"/> is nothing so that a buyer is not made to feel poor by an errand it is not paying
/// for, which is the mistake the Baron's rounds cost twelve minutes of standing still.
/// </para>
/// </summary>
public sealed class BotSupply : BotDeed
{
    /// <summary>Gold the guild's chest paid towards stocking its own counter. See <see cref="Advance"/>.</summary>
    public static long FromChest { get; private set; }

    /// <summary>The ledger key.</summary>
    public const string Trade = "supply";

    /// <summary>What stocking the hall is reckoned at per minute before experience corrects it.</summary>
    public static double Prior { get; set; } = 260.0;

    /// <summary>How long the errand itself takes once the bot is standing where it needs to be.</summary>
    public static double WorkMinutes { get; set; } = 3.0;

    /// <summary>How near the counter the bot must be. The engine's own reach, at both ends.</summary>
    public static int Reach => BotShops.CounterReach;

    private readonly Guild _guild;

    private readonly BaseHouse _hall;

    /// <summary>The shopkeeper this run is buying from; the next one that has the goods when this one's shelf is bare.</summary>
    private BaseVendor _shop;

    /// <summary>How many times this run has gone on to another shopkeeper. See <see cref="BotShops.RepickLimit"/>.</summary>
    private int _repicks;

    private readonly Type _kind;

    private readonly int _batch;

    private int _price;

    private int _before = -1;

    private int _bought;

    private int _shelved;

    private int _spent;

    public BotSupply(Guild guild, BaseHouse hall, BaseVendor shop, Type kind, int batch, int price)
    {
        _guild = guild;
        _hall = hall;
        _shop = shop;
        _kind = kind;
        _batch = Math.Max(1, batch);
        _price = Math.Max(1, price);
    }

    public override string Kind => Trade;

    public override Map Map => _bought > 0 ? _hall?.Map : _shop?.Map;

    public override Point3D Where => _bought > 0 ? _hall?.Location ?? Point3D.Zero : _shop?.Location ?? Point3D.Zero;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    /// <summary>Nothing out of this bot's own pocket: the guild is buying. See the note on the class.</summary>
    public override int Outlay => 0;

    /// <summary>The batch is bought over a counter; once bought, the walk to the hall needs no coin. See <see cref="BotDeed.AtCounter"/>.</summary>
    public override bool AtCounter => _shop != null && _bought <= 0;

    /// <summary>Not a coin comes back. The guild has bought its members a store cupboard.</summary>
    public override double Coin => 0.0;

    /// <summary>Not about money, and not to be refused for failing to earn any.</summary>
    public override bool Unpaid => true;

    /// <summary>
    /// Put down for what will not wait and taken up again, rather than dropped (12.10.2026, w2). In 10.2 h on 11.10, 191 of the
    /// 232 supply runs dropped were "pressed to enlist", 98 of them with the guild's batch already bought and in the pack — the
    /// guild's money left in a pocket and the shelf still bare. The batch and the shopkeeper are on the deed, so the run goes on
    /// from where it was. Not steadfast: a better offer still outbids the walk to the counter.
    /// </summary>
    public override bool Resumes => true;

    /// <summary>
    /// The walk's best distance is forgotten when the run is taken up again (12.10.2026, w2, review B): the bot comes back from
    /// wherever the company broke up, and a <c>_nearest</c> measured from the first leg would read the new walk as "not closing"
    /// for its first two hundred beats. See <see cref="Closing"/>.
    /// </summary>
    public override void Resumed(IBotWilful bot) => Fresh();

    /// <summary>How many of the batch are bought and in the pack: the guild's money, until they are on the counter. See <c>BotSquads.Carrying</c>.</summary>
    public int Bought => _bought;

    /// <summary>
    /// What the bot's own purse put into this, handed straight back as takings.
    ///
    /// <para>
    /// <b>Every errand a guild pays for was teaching the shard that guild errands lose money.</b> Takings are
    /// <c>coin + made</c>, coin is measured as the change in the bot's own purse, and these errands are paid
    /// for by a levy that comes partly out of that purse — so the ledger recorded "raised The Lantern for
    /// 5000gp: -20 in 0.4 min (-50/min)", "set an oven up: -33", "hired a merchant: -148", "left 20 ash on
    /// the counter: -60". <c>BotCommons.Corrected</c> then drags the trade's whole estimate towards those
    /// numbers, which is why <c>BotHall.Prior</c> carries a note about having to be raised from ninety to
    /// four hundred before anybody would take one.
    /// </para>
    ///
    /// <para>
    /// The fix is the one <c>BotRestock</c> has always used and says in as many words: goods are worth what
    /// they cost. A hall, a bench, a shopkeeper and a shelf of reagents are all worth what was paid for them,
    /// and the bot's share of the price is a contribution to something the guild now owns rather than money
    /// that left the world. So the errand comes out at about nothing a minute — never punished, never
    /// preferred over work that actually produces something, which is the right place for it.
    /// </para>
    /// </summary>
    public override int Made => _mine;

    /// <summary>Coin out of this bot's own purse, measured across the payment rather than assumed.</summary>
    private int _mine;

    /// <summary>
    /// How many beats a leg of this errand may go without getting nearer before it is given up.
    ///
    /// <para>
    /// <b>An errand that only ever answers "walk" is immortal, and this one was.</b> Measured 09.09.2026:
    /// Quill held a supply run for eleven minutes, reported every time as "walking to somewhere 1 tile off",
    /// and never finished, failed or dropped it — because the bot had been taken onto the Bound rung by a
    /// company on the way, and a bound bot's auction is switched off, so nothing was ever going to replace
    /// the errand it was holding. The whole time it held its guild's supplier claim, so no other member could
    /// be sent either. See <c>bot-frozen-work-family</c>: work that answers Work, or the same walk, for ever
    /// is work that never ends.
    /// </para>
    ///
    /// <para>
    /// Measured as progress rather than as attempts, which is the distinction <c>BotDig.TrekLimit</c> pays
    /// for at length: a long walk across the island is legitimate, and what is not legitimate is a walk that
    /// stops getting closer.
    /// </para>
    ///
    /// <para>
    /// <b>Two hundred, and forty was wrong by five times over — the same units mistake this file's neighbour
    /// was opened to fix.</b> A bot is beaten once per step, not once per tick, so forty beats is eight to
    /// sixteen seconds of not gaining ground: an ordinary detour round a building. Measured within ten
    /// minutes of it going in — five supply runs failed with "the walk to Delano stopped closing" while the
    /// shops were perfectly reachable. Two hundred is <c>BotDig.TrekLimit</c>, which carries the reasoning
    /// and the measurement, and taking its number rather than choosing a second one is the point.
    /// </para>
    /// </summary>
    public static int TrekLimit { get; set; } = 200;

    /// <summary>The nearest this errand has got to what it is currently walking at.</summary>
    private int _nearest = int.MaxValue;

    private int _stalled;

    private int _routeStamp = -1;

    /// <summary>Whether the walk is still closing. Resets when the errand changes what it is walking at.</summary>
    private bool Closing(Mobile body, Point3D at)
    {
        // By the road the bot is walking, and started again with each route it draws. See BotJourney.RoadLeft.
        var gap = System.Math.Max(System.Math.Abs(body.X - at.X), System.Math.Abs(body.Y - at.Y));

        if (body is BotMobile { Journey: { } journey })
        {
            gap = journey.RoadLeft(body.Location, at);

            if (_routeStamp != journey.RouteStamp)
            {
                _routeStamp = journey.RouteStamp;
                _nearest = int.MaxValue;
                _stalled = 0;
            }
        }

        if (gap < _nearest)
        {
            _nearest = gap;
            _stalled = 0;

            return true;
        }

        // A held walk is not a walk that stopped closing (12.10.2026, w7): a buyer stood for its carrier (BotRendezvous.Stand)
        // or a trader for its guards stands by somebody else's word, and BotWill's own watch skips those beats too.
        if (body is BotMobile { Journey.Held: true })
        {
            return true;
        }

        return ++_stalled < TrekLimit;
    }

    /// <summary>A new leg, so the old leg's best distance means nothing. Called when the target changes.</summary>
    private void Fresh()
    {
        _nearest = int.MaxValue;
        _stalled = 0;
    }


    /// <summary>
    /// The guild's money is in this pack as goods. See <see cref="BotDeed.Committed"/>.
    ///
    /// True only between the two legs: before the purchase there is nothing to lose, and once the lot is on
    /// the counter the errand is over anyway.
    /// </summary>
    public override bool Committed => _bought > _shelved;

    public override string Stage =>
        _shelved > 0 ? $"left {_shelved} {_kind?.Name} on the counter of {_guild?.Name}"
            : _bought > 0 ? $"carrying {_bought} {_kind?.Name} to the hall of {_guild?.Name}"
            : $"to {_shop?.Name} for {_batch} {_kind?.Name}";

    /// <summary>
    /// The way turned out not to exist.
    ///
    /// <para>
    /// Nothing to bend to — the batch was priced against this shopkeeper — but the failure is filed under the
    /// <em>place</em>, because that is the word the shop lookup asks in. Filed under the errand's name it
    /// would be written and never read, and the next beat would pick the same unreachable counter on
    /// distance alone. That is the peddler's lesson and it costs one line to keep.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        if (_bought <= 0 && _shop != null)
        {
            bot?.Resolve?.Ledger?.Beware(BotShops.ShopKind, _shop.Map, _shop.Location);
        }

        return false;
    }

    /// <summary>However it went, the guild is no longer waiting on this bot.</summary>
    public override void Drop(IBotWilful bot) => BotSupplier.Release(_guild);

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _guild == null || _hall is not { Deleted: false })
        {
            return BotDoing.Failed("the hall is gone");
        }

        var pack = body.Backpack;

        if (pack == null)
        {
            return BotDoing.Failed("no pack to carry supplies in");
        }

        BotSupplier.Hold(_guild);

        var merchant = BotShelf.Of(_hall);

        if (merchant == null)
        {
            return BotDoing.Failed($"{_guild.Name} has no merchant to stock");
        }

        if (_bought <= 0)
        {
            if (_shop is not { Deleted: false } || _shop.Map == null)
            {
                return BotDoing.Failed("the shopkeeper is gone");
            }

            // <b>Asked again here, and not only at the proposal, because the money is spent on the far side
            // of a walk.</b> A shelf with a slot free when the courier set out can be full by the time it
            // reaches the shop — nine other members are stocking the same hall — and the whole cost of
            // finding out at the counter instead is that the guild's money is already gone and the goods are
            // in a pack that will keep them. Failing here costs a walk; failing there cost the population
            // 1,239 packets in eight hours. See BotShelf.Room.
            if (!BotShelf.Room(merchant, _kind))
            {
                BotSupplier.Shut();

                return BotDoing.Failed(
                    $"the counter of {_guild.Name} had no room left for {_kind.Name} before anything was bought"
                );
            }

            if (!body.InRange(_shop.Location, Reach))
            {
                if (!Closing(body, _shop.Location))
                {
                    // Filed under the place, because that is the word the shop lookup asks in. See Bend.
                    bot?.Resolve?.Ledger?.Beware(BotShops.ShopKind, _shop.Map, _shop.Location);

                    return BotDoing.Failed($"the walk to {_shop.Name} stopped closing {_nearest} tiles short");
                }

                return BotDoing.Walk(_shop.Map, _shop, BotArrival.Within(Reach), $"to {_shop.Name} for {_kind.Name}");
            }

            // <b>Counted before the purchase, not after it.</b> A bot on this errand is very likely carrying
            // some of the same thing for its own use — a healer fetching bandages has bandages — and putting
            // the pack's whole holding on the shelf would be the guild robbing its own courier. What goes out
            // is what came off this shelf and nothing else.
            if (_before < 0)
            {
                _before = pack.GetAmount(_kind);
            }

            var bill = _batch * _price;
            var wealth = BotYield.Wealth(body);
            var mine = wealth;

            // <b>The guild's chest pays its own counter first (30.09.2026, build 361).</b> The offer weighed the guild's fund —
            // its chest and what its members can spare (BotEstate.Fund) — and the payment asked only the members (BotGuilds.Stand):
            // The Crown, the first guild with a hall standing, its chest holding what the members had not, failed "could not
            // raise 234gp to stock its counter" 32 times in ten minutes from 14:55 on 360, three couriers in turn, the breaker
            // stopping each and the next taking it up. The shortfall is drawn from the chest into the courier's pack first; the
            // members make up what is still short, as before.
            var fromChest = 0;

            if (wealth < bill && BotChest.Holds(_guild.Name) > 0 && body.Backpack is { } purse)
            {
                fromChest = BotChest.Draw(_guild.Name, bill - wealth);

                if (fromChest > 0)
                {
                    purse.DropItem(new Gold(fromChest));
                    wealth += fromChest;
                    FromChest += fromChest;
                }
            }

            if (wealth < bill && !BotGuilds.Stand(body as BotMobile, bill - wealth))
            {
                return BotDoing.Failed($"{_guild.Name} could not raise {bill}gp to stock its counter");
            }

            var got = BotShops.Buy(bot, _shop, _kind, _batch, out var refused);

            // What this bot itself put in, across the whole payment: the levy topped its purse up first, so
            // the difference is its own share and nothing else. See Made.
            _mine += Math.Max(0, mine - BotYield.Wealth(body));

            // What the chest put in and the counter did not take goes back to the chest, not into the courier's own purse.
            if (got <= 0 && fromChest > 0 && body.Backpack is { } returning && returning.ConsumeTotal(typeof(Gold), fromChest))
            {
                BotChest.Put(_guild.Name, fromChest);
                FromChest -= fromChest;
            }

            if (got <= 0)
            {
                // <b>A shelf emptied by whoever got there first: on to the next counter that has it, as a restock goes.</b>
                // The guild's supply run was the one buyer on the shard that still stopped at the first bare shelf: on
                // build 206, 09:58–10:12 on 24.09.2026, three of its eight failures were "the shelf holds no Skillet at
                // any price" (Elspeth at Bjorn's, Harlan at Everett's) and "no Scissors" (Dain at Orane's). See
                // BotShops.Next, which BotRestock has used since 16.09.
                var next = BotShops.Next(bot, _shop, _kind, ref _repicks);

                if (next != null)
                {
                    _shop = next;
                    _price = Math.Max(1, BotShops.Price(next, _kind));
                    Fresh();

                    return BotDoing.Walk(next.Map, next, BotArrival.Within(Reach), $"on to {next.Name} for {_kind.Name}");
                }

                return BotDoing.Failed(refused ?? "the shopkeeper would not sell it");
            }

            _bought = got;
            _spent = got * _price;

            // A different place to be walking at, so the first leg's best distance says nothing about this
            // one. Without this the second leg starts already believing it has been within a tile.
            Fresh();

            return BotDoing.Work($"{got} {_kind.Name} bought for the hall");
        }

        if (!body.InRange(merchant.Location, Reach))
        {
            if (!Closing(body, merchant.Location))
            {
                // Nothing to file: the hall is the guild's own and its merchant is not going anywhere, so a
                // walk that stopped closing is about this bot's afternoon rather than about the place.
                return BotDoing.Failed(
                    $"the walk to the counter of {_guild.Name} stopped closing {_nearest} tiles short"
                );
            }

            return BotDoing.Walk(merchant.Map, merchant, BotArrival.Within(Reach), $"to the counter of {_guild.Name}");
        }

        // Standing at its own guild's counter with the guild's money in reach: the two things a member is
        // uniquely able to do here, done while it is here rather than made into errands of their own.
        BotShelf.Wage(body as BotMobile, merchant);
        BotShelf.Collect(body, merchant);

        var carrying = pack.GetAmount(_kind);
        var spare = Math.Min(_bought, Math.Max(0, carrying - Math.Max(0, _before)));

        if (spare <= 0)
        {
            return BotDoing.Failed($"the {_kind.Name} was gone out of the pack before it got to the counter");
        }

        // <b>A kind that does not stack goes on the shelf one by one (06.10.2026, build next).</b> Lift looks for one item holding
        // the whole lot and, finding none, takes "any one lot of the kind" — and a draught is always a lot of one in this era. So
        // sixteen heal potions bought with the guild's money became one potion on the counter priced at sixteen ("1 LesserCurePotion
        // off the guild's own counter for 90gp", 166 such sales on 04.10.2026) and fifteen in the courier's pack, for ever: 1,504
        // draughts bought for the halls on 04.10.2026 and 219 shelved. Malachi held 73 heal potions at 00:29 on 06.10.2026, Aldric
        // Ashdown 70 cure potions, Sarendil 62 — every one of the gatherers Patrick saw with fifty and more was a guild's courier.
        if (!BotSupplier.Stacks(_kind))
        {
            return Singly(body, pack, merchant, spare);
        }

        var lot = Lift(body, pack, _kind, spare);

        if (lot == null)
        {
            return BotDoing.Failed($"could not get the {_kind.Name} out of the pack");
        }

        // At what the guild paid, times the markup, which is one. A member buying here is meant to be
        // indifferent about the price and better off about the walk. See BotShelf.Markup.
        var asking = Math.Max(1, (int)Math.Round(spare * _price * BotShelf.Markup));

        if (!BotShelf.Put(merchant, lot, asking))
        {
            // Back in the pack rather than on the floor: the goods are the guild's, and a failed errand is
            // not a reason to leave them in a room.
            pack.DropItem(lot);

            return BotDoing.Failed("the merchant would not take any more on its shelf");
        }

        _shelved = spare;

        return BotDoing.Done(
            $"{spare} {_kind.Name} on the counter of {_guild.Name} at {asking}gp the lot, {_spent}gp of guild money"
        );
    }

    /// <summary>Things of a kind that does not stack put on a guild counter one by one. See <see cref="Singly"/>.</summary>
    public static long Loose { get; private set; }

    /// <summary>Of those, the ones the merchant would not take, left in the courier's pack (for the porter to put in its box).</summary>
    public static long Unshelved { get; private set; }

    /// <summary>
    /// The batch on the shelf one thing at a time, each at its own price, for a kind that does not stack. What the merchant will not
    /// take stays in the pack and is counted (<see cref="Unshelved"/>); the supplier sizes the batch to the free places, so that is a
    /// race with another courier and not the rule.
    /// </summary>
    private BotDoing Singly(Mobile body, Container pack, PlayerVendor merchant, int spare)
    {
        var each = Math.Max(1, (int)Math.Round(_price * BotShelf.Markup));
        var put = 0;

        while (put < spare)
        {
            var one = Lift(body, pack, _kind, 1);

            if (one == null)
            {
                break;
            }

            if (!BotShelf.Put(merchant, one, each))
            {
                pack.DropItem(one);

                break;
            }

            put++;
        }

        Loose += put;
        Unshelved += spare - put;

        if (put <= 0)
        {
            return BotDoing.Failed("the merchant would not take any more on its shelf");
        }

        _shelved = put;

        return BotDoing.Done(
            $"{put} {_kind.Name} on the counter of {_guild.Name} at {each}gp each, {_spent}gp of guild money"
            + (put < spare ? $"; {spare - put} more the merchant would not take, left in the pack" : "")
        );
    }

    /// <summary>
    /// Takes exactly this many of a kind out of a pack as one lot, splitting a stack if it has to.
    ///
    /// <para>
    /// <c>LiftItemDupe</c> is the engine's own way of dividing a stack, and its shape is easy to read
    /// backwards: it leaves the <em>original</em> item holding the amount asked for and puts the remainder in
    /// a new one beside it. So the item handed back here is the one to carry to the shelf, and the leftovers
    /// stay in the pack without anything else being done about them.
    /// </para>
    /// </summary>
    private static Item Lift(Mobile body, Container pack, Type kind, int many)
    {
        // A courier's own bound weapon is not the guild's stock, even when the guild sells that kind.
        var bond = (body as BotMobile)?.Bond;
        List<Item> carried = [.. pack.Items];

        for (var i = 0; i < carried.Count; i++)
        {
            var item = carried[i];

            if (item.Deleted || !item.Movable || !kind.IsInstanceOfType(item) || BotBinding.IsBound(item, bond))
            {
                continue;
            }

            var held = Math.Max(1, item.Amount);

            if (held < many)
            {
                continue;
            }

            if (held > many)
            {
                Mobile.LiftItemDupe(item, many);
            }

            return item;
        }

        // Nothing single held enough. Any one lot of the kind is better than failing the errand, and the
        // caller has already worked out that the pack owes the shelf this much.
        for (var i = 0; i < carried.Count; i++)
        {
            var item = carried[i];

            if (!item.Deleted && item.Movable && kind.IsInstanceOfType(item) && !BotBinding.IsBound(item, bond))
            {
                return item;
            }
        }

        return null;
    }
}
