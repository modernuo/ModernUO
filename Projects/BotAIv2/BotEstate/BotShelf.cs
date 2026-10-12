using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// The guild's own counter: finding the merchant in the hall, putting goods on it, buying off it, and
/// carrying its takings back to the guild.
///
/// <para>
/// <b>Why a shop in the hall is worth anything at all, when the bots already have a market.</b> The bots' own
/// shops by the bank (<see cref="BotShopkeep"/>) are kept by a bot standing there for a shift; what a keeper cannot
/// do is stand there for ever: it belongs to a bot, and this population is rebuilt at every restart. A merchant in a hall is saved with the house, so what is left on
/// its shelf outlives the bots that put it there — and Patrick can walk in and buy it.
/// </para>
///
/// <para>
/// <b>And the errand it removes is a real one.</b> Reagents, bandages, tools and bottles are bought from
/// shopkeepers in Britain, and that walk is where three percent of the population's time was going. One
/// member fetching a batch for the hall spares every other member the journey. The gold still leaves the
/// world at the town counter — it is the same purchase — but it leaves once instead of nine times, and
/// nobody crosses the island for a bandage.
/// </para>
///
/// <para>
/// <b>Everything here goes through the engine's own bookkeeping rather than round it.</b> Dropping into the
/// merchant's pack makes <c>PlayerVendor.OnSubItemAdded</c> open a lot at 999gp, and the price on that lot
/// is public, so a shelf can be stocked without a patch and without a gump. Buying repeats, line for line,
/// what the buy gump's OK button does: pocket first then bank, the goods into the buyer's pack, the price
/// into the shopkeeper's purse. Anything cheaper than that would be minting.
/// </para>
///
/// <para>
/// <b>The shopkeeper has to be paid or it dies.</b> In this era the engine charges a vendor
/// <c>20 + (shelf value - 500) / 500</c> every UO day, which is two real hours, and destroys it the first
/// time the bill is larger than its purse — a hired merchant is 1,252gp of the guild's money walking away
/// while nobody is looking. It is seeded with 1,000gp by the engine, so an empty one lives about four days;
/// a well-stocked one much less. See <see cref="Wage"/>, which is why the supply errand tops it up.
/// </para>
/// </summary>
public static class BotShelf
{
    /// <summary>How near the merchant a bot must stand to deal with it. The engine's own counter reach.</summary>
    public static int Reach => BotShops.CounterReach;

    /// <summary>
    /// What the guild adds to what it paid, as a multiplier.
    ///
    /// <para>
    /// One, and that is a decision rather than an oversight. The member who buys here has already been given
    /// the whole of the saving — it does not walk to Britain — so charging a premium on top would be the
    /// guild taxing its own people twice. The shopkeeper's wages come out of the guild's purse in
    /// <see cref="Wage"/> instead, which is the honest place for a cost the guild chose to take on.
    /// </para>
    ///
    /// <para>
    /// It is a dial because the argument for a margin is a real one and Patrick may want to see it: at 1.2
    /// the hall shop becomes a business rather than a store cupboard, and the shopper's own comparison —
    /// cheapest wins, ties to one of ours — will start sending members back to town when the guild gets
    /// greedy. That is the correct behaviour, and it is worth being able to watch it happen.
    /// </para>
    /// </summary>
    public static double Markup { get; set; } = 1.0;

    /// <summary>
    /// What the merchant is topped up to when a member is standing at it with the guild's money.
    ///
    /// Twelve pay ticks of an empty shelf, which is a day of real time — long enough that a guild whose
    /// members are all busy does not lose its merchant, short enough that the guild is not lending the
    /// shopkeeper a fortune it could be spending on armour.
    /// </summary>
    public static int Float { get; set; } = 240;

    /// <summary>Below this in the merchant's purse, a member standing there pays it. See <see cref="Wage"/>.</summary>
    public static int Wages { get; set; } = 120;

    /// <summary>How long a lot must have stood before anybody may buy it. The engine's own rule, kept.</summary>
    public static TimeSpan Settling { get; } = TimeSpan.FromMinutes(1.0);

    // ---- Every gate, counted apart, because a shelf that is empty and a shelf that is refusing look the
    // same from the outside. -----------------------------------------------------------------------------

    /// <summary>Lots put on a guild counter.</summary>
    public static long Stocked { get; private set; }

    /// <summary>What those lots cost the guilds that bought them.</summary>
    public static long StockedWorth { get; private set; }

    /// <summary>Times the merchant's pack would not take any more.</summary>
    public static long Refused { get; private set; }

    /// <summary>Refusals where a lot the engine says these goods stack onto was standing on the shelf.</summary>
    public static long Unstacked { get; private set; }

    /// <summary>Refusals where nothing on the shelf would take them and there was no slot either.</summary>
    public static long Slotless { get; private set; }

    /// <summary>Times the goods went in and no lot of the kind could be found afterwards.</summary>
    public static long Lost { get; private set; }

    /// <summary>Times the lot was on the shelf and the engine had opened no vendor item against it.</summary>
    public static long Unpriced { get; private set; }

    /// <summary>Lots bought off a guild counter by a member.</summary>
    public static long Taken { get; private set; }

    /// <summary>What members have paid their own guild's shop.</summary>
    public static long Paid { get; private set; }

    /// <summary>Takings carried back off a merchant into the guild.</summary>
    public static long Gathered { get; private set; }

    /// <summary>Coin paid to shopkeepers so that they keep standing.</summary>
    public static long Waged { get; private set; }

    /// <summary>
    /// The guild's merchant, if it has one standing.
    ///
    /// <para>
    /// The hall is the register: <c>BaseHouse.PlayerVendors</c> is kept by the engine, saved with the house
    /// and emptied when one is destroyed, so there is no second list here to fall out of step with it. The
    /// first one is taken rather than the best one — a guild is allowed at most
    /// <c>BotEstate.MostMerchants</c>, and while that is one the question does not arise.
    /// </para>
    /// </summary>
    public static PlayerVendor Of(Guild guild) => Of(BotEstate.Hall(guild));

    /// <summary>The merchant in this bot's own guild's hall.</summary>
    public static PlayerVendor Of(Mobile bot) => Of(BotEstate.Hall(bot?.Guild as Guild));

    /// <summary>The merchant standing in this hall.</summary>
    public static PlayerVendor Of(BaseHouse hall)
    {
        var standing = hall?.PlayerVendors;

        if (standing == null)
        {
            return null;
        }

        for (var i = 0; i < standing.Count; i++)
        {
            if (standing[i] is { Deleted: false, Map: not null } merchant && merchant.Map != Map.Internal)
            {
                return merchant;
            }
        }

        return null;
    }

    /// <summary>How many of a kind are standing on the shelf, over every lot.</summary>
    public static int Held(PlayerVendor merchant, Type kind)
    {
        var pack = merchant?.Backpack;

        if (pack == null || kind == null)
        {
            return 0;
        }

        var many = 0;
        var lots = pack.Items;

        for (var i = 0; i < lots.Count; i++)
        {
            var lot = lots[i];

            if (!lot.Deleted && kind.IsInstanceOfType(lot))
            {
                many += Math.Max(1, lot.Amount);
            }
        }

        return many;
    }

    /// <summary>
    /// The cheapest lot of a kind on the shelf that may be bought right now, and what it costs.
    ///
    /// <para>
    /// Cheapest by the lot rather than by the unit, because a lot is what changes hands: a shop sells a
    /// packet of twenty bandages, not a bandage. A buyer wanting five and finding a packet of twenty buys
    /// the packet, which is what happens at any counter and what the engine's own vendor does.
    /// </para>
    /// </summary>
    public static Item Offer(PlayerVendor merchant, Type kind, out int price)
    {
        price = 0;

        var pack = merchant?.Backpack;

        if (pack == null || kind == null)
        {
            return null;
        }

        Item best = null;
        var lots = pack.Items;

        for (var i = 0; i < lots.Count; i++)
        {
            var lot = lots[i];

            if (lot.Deleted || !kind.IsInstanceOfType(lot))
            {
                continue;
            }

            var vi = merchant.GetVendorItem(lot);

            // Not for sale, not valid, or not yet settled — the engine's three refusals, asked here so a
            // buyer is never sent across a room for a lot the shopkeeper would decline at the counter.
            if (vi is not { Valid: true, IsForSale: true } || vi.Created + Settling > Core.Now)
            {
                continue;
            }

            if (best == null || vi.Price < price)
            {
                best = lot;
                price = vi.Price;
            }
        }

        return best;
    }

    /// <summary>
    /// Puts a lot on the shelf at a price.
    ///
    /// <para>
    /// The engine opens the lot itself the moment the goods land in the merchant's pack — see
    /// <c>PlayerVendor.OnSubItemAdded</c>, which sets one at 999gp — so this drops first and prices second.
    /// Dropped with <c>TryDropItem</c> and its stacking left on, because a second packet of bandages should
    /// join the first rather than take up another slot on a pack that has a hundred and twenty-five.
    /// </para>
    ///
    /// <para>
    /// <b>And the price is set after the drop for a reason that is easy to get wrong.</b> If the goods stack
    /// onto a lot that is already there, the item the caller was holding is gone and the lot to price is the
    /// one on the shelf. Asking the merchant which lot the goods ended up in — rather than assuming it is the
    /// one handed over — is the difference between a shelf priced correctly and a stack of forty bandages
    /// still marked at the price of twenty.
    /// </para>
    /// </summary>
    public static bool Put(PlayerVendor merchant, Item goods, int price)
    {
        var pack = merchant?.Backpack;

        if (pack == null || goods is not { Deleted: false } || price < 0)
        {
            return false;
        }

        var kind = goods.GetType();
        // <b>The shopkeeper itself when its owner is gone, and its owner is very often gone.</b> A merchant
        // is saved with its house and this population is rebuilt at every restart, so the bot that carried
        // the contract last night is a deleted mobile this morning. Handing a deleted mobile to TryDropItem
        // as the one doing the dropping is asking for trouble for no gain: nothing about the drop depends on
        // who it was.
        var owner = merchant.Owner is { Deleted: false } held ? held : merchant;

        // <b>What the shelf was already asking for this kind, taken before the drop.</b> The note below
        // predicted the stacking case and the first cut still got it wrong in the other direction: twenty
        // sulphurous ash went out at 60gp, a second twenty stacked onto them, and the merged lot of forty
        // was re-priced at 60 — the guild's second purchase given away. A price is for a lot, so a lot that
        // grows has to be priced for what it now holds.
        var standing = Lot(merchant, kind);
        var already = standing == null ? 0 : merchant.GetVendorItem(standing)?.Price ?? 0;

        if (!pack.TryDropItem(owner, goods, false))
        {
            Refused++;

            // <b>Which half of Room was wrong, because the fix depends entirely on that.</b> Room used to answer
            // yes for two different reasons — a stackable lot of the kind already standing, or a slot free — and
            // this is what said which one lied: on build 43 all twenty refusals had the lot, because the pack asks
            // its item cap before it tries the stacks. Room now asks for the slot alone; a refusal counted here
            // again means somebody filled the last slot between the question and the drop.
            if (Standing(merchant, goods))
            {
                Unstacked++;
            }
            else
            {
                Slotless++;
            }

            return false;
        }

        // Whatever the goods became — the lot itself, or the stack it merged into.
        var merged = goods.Deleted || goods.Parent != pack;
        var lot = merged ? Lot(merchant, kind) : goods;

        // <b>Three ways out of here and one bucket between them, which is how "the merchant would not take
        // any more" came to mean four different things.</b> The courier's failure line says the same words
        // whichever of these fired, so 1,239 refusals in eight hours could not be told apart — and the room
        // check written against the first of them on 10.09.2026 removed none of them at all, because the
        // refusals were not that one. See BotShelf.Room.
        if (lot == null)
        {
            Lost++;

            return false;
        }

        var vi = merchant.GetVendorItem(lot);

        if (vi == null)
        {
            Unpriced++;

            return false;
        }

        // Only when it actually joined the lot that was standing there: a second, separate lot of the same
        // kind is priced on its own.
        vi.Price = merged && ReferenceEquals(lot, standing) ? already + price : price;
        vi.Description = "";
        lot.InvalidateProperties();

        Stocked++;
        StockedWorth += price;

        return true;
    }

    /// <summary>
    /// Whether the shelf could take a lot of this kind at all.
    ///
    /// <para>
    /// <b>The question nobody asked, and it cost the population its packs.</b> A merchant's backpack is a
    /// <c>Backpack</c> and holds a hundred and twenty-five items — the engine's own cap, checked in
    /// <c>Container.CheckHold</c> — and the four guild counters were sitting at exactly five hundred lots
    /// between them by the eighth hour of 10.09.2026. <see cref="Held"/> answers "how many of this kind are
    /// on the shelf", which is a different question: a shelf holding none of a kind and no free slot passes
    /// that test every time. So the supplier kept sending couriers, each of whom drew the guild's money,
    /// bought a packet of twenty in town, walked to the hall and was refused — 1,239 times in eight hours,
    /// with the goods staying in the courier's pack for ever afterwards. Ulla was carrying six hundred and
    /// fifty-nine bandages at 244 of 243 stones when Patrick looked.
    /// </para>
    ///
    /// <para>
    /// <b>A slot, whatever is standing on the shelf, and the second version of this got that backwards.</b> It
    /// answered yes whenever a stackable lot of the kind stood there, because the server's
    /// <c>Container.TryDropItem</c> tries the stacks before it asks about room. A merchant's pack is not that
    /// class: <c>VendorBackpack</c> is a <c>Backpack</c>, a <c>BaseContainer</c>, and <c>BaseContainer.TryDropItem</c>
    /// overrides it to ask <c>CheckHold</c> with the item count <em>first</em> — so a shelf at the cap refuses a
    /// packet of ash that would have joined the ash already on it. On build 43 every one of the twenty refusals in
    /// the first thirty-four minutes had such a lot standing, "the merchant would not take any more on its shelf" was
    /// the commonest failure on the shard, and each of them was a courier that had already spent the guild's money
    /// in town. The kind is kept in the question for the day a lot is itself a container.
    /// </para>
    /// </summary>
    /// <summary>
    /// Whether any lot on the shelf would take these exact goods, by the engine's own stacking test.
    ///
    /// A diagnosis for <see cref="Put"/>'s refusals and not a question of room: the merchant's pack asks its item
    /// cap before it tries a single stack, so a lot that would take the goods says nothing about whether the pack
    /// will let them in. See <see cref="Room"/>.
    /// </summary>
    public static bool Standing(PlayerVendor merchant, Item goods)
    {
        var pack = merchant?.Backpack;

        if (pack == null || goods == null)
        {
            return false;
        }

        var lots = pack.Items;

        for (var i = 0; i < lots.Count; i++)
        {
            if (lots[i] is not Container && !lots[i].Deleted && lots[i].CanStackWith(goods))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Room(PlayerVendor merchant, Type kind)
    {
        var pack = merchant?.Backpack;

        if (pack == null || kind == null)
        {
            return false;
        }

        var most = pack.MaxItems;

        return most <= 0 || pack.TotalItems < most;
    }

    /// <summary>The lot of this kind on the shelf, for the stacking case in <see cref="Put"/>.</summary>
    private static Item Lot(PlayerVendor merchant, Type kind)
    {
        var lots = merchant.Backpack.Items;

        for (var i = 0; i < lots.Count; i++)
        {
            if (!lots[i].Deleted && kind.IsInstanceOfType(lots[i]))
            {
                return lots[i];
            }
        }

        return null;
    }

    /// <summary>
    /// A bot buys a lot off the shelf, by exactly the accounting the buy gump does.
    ///
    /// <para>
    /// <b>The owner pays like anybody else, and the engine's own rule is the one being departed from here.</b>
    /// <c>PlayerVendor.TryToBuy</c> tells an owner to help itself, and that is right for a player who put its
    /// own goods out. It is wrong here: what stands on this shelf was bought with the guild's money, and the
    /// one member who happened to carry the contract has no more claim on it than the other nine. Charging
    /// everybody is what keeps the shelf a guild's store rather than one bot's larder.
    /// </para>
    /// </summary>
    public static int Take(Mobile buyer, PlayerVendor merchant, Item lot, out string refused)
    {
        refused = null;

        var pack = buyer?.Backpack;

        if (pack == null || merchant is not { Deleted: false } || lot is not { Deleted: false })
        {
            refused = "there is no shopkeeper to buy from";

            return 0;
        }

        if (!buyer.InRange(merchant.Location, Reach))
        {
            refused = "it is not close enough to the counter";

            return 0;
        }

        var vi = merchant.GetVendorItem(lot);

        if (vi is not { Valid: true, IsForSale: true } || !lot.IsChildOf(merchant.Backpack))
        {
            refused = "the shopkeeper would not sell that";

            return 0;
        }

        if (vi.Created + Settling > Core.Now)
        {
            refused = "the lot has only just gone out";

            return 0;
        }

        var price = vi.Price;
        var purse = BotYield.Spendable(buyer);

        if (purse < price)
        {
            refused = $"{purse}gp will not buy a lot at {price}gp";

            return 0;
        }

        var many = Math.Max(1, lot.Amount);

        // <b>Paid for before it is handed over, which is not the order the gump uses.</b> The gump moves the
        // goods first and then takes the money, and gets away with it because a client cannot be halfway
        // through the exchange — but here a withdrawal can fail on its own, and it is not checked there at
        // all. Moving first would mean a bot whose bank refused it walks away holding the guild's goods for
        // nothing. Pocket first and then the account, as everywhere else on this shard; anything that fails
        // is put straight back.
        var fromPack = pack.ConsumeUpTo(typeof(Gold), price);
        var owing = price - fromPack;

        if (owing > 0 && !Banker.Withdraw(buyer, owing))
        {
            Refund(buyer, pack, fromPack);

            refused = $"{fromPack}gp in the pack and the bank would not find the other {owing}gp";

            return 0;
        }

        if (!buyer.PlaceInBackpack(lot))
        {
            Refund(buyer, pack, price);

            refused = "there is no room in the pack for it";

            return 0;
        }

        merchant.HoldGold += price;

        Taken++;
        Paid += price;

        return many;
    }

    /// <summary>
    /// Money back into a bot that has been charged for something it did not get.
    ///
    /// Into the bank rather than the pack, because the pack is exactly the thing that may have just refused
    /// to hold something — and a refund that can itself fail is how a bot ends up paying for nothing.
    /// </summary>
    private static void Refund(Mobile buyer, Container pack, int coins)
    {
        if (coins <= 0 || buyer == null)
        {
            return;
        }

        if (Banker.Deposit(buyer, coins))
        {
            return;
        }

        var back = new Gold(coins);

        if (pack == null || !pack.TryDropItem(buyer, back, false))
        {
            back.MoveToWorld(buyer.Location, buyer.Map);
        }
    }

    /// <summary>
    /// Carries the shopkeeper's takings back into the guild, leaving it its wages.
    ///
    /// <para>
    /// Into the collecting member's own bank, which is where <see cref="BotEstate.Fund"/> looks: a guild on
    /// this shard has no treasury of its own, only the sum of what its members are holding. Any member may
    /// do it, and that is deliberate — a till only the hiring bot could empty is a till that fills up while
    /// that bot is off hunting, which is the shape of half the faults this project has had.
    /// </para>
    /// </summary>
    public static int Collect(Mobile member, PlayerVendor merchant)
    {
        if (member == null || merchant is not { Deleted: false })
        {
            return 0;
        }

        var spare = merchant.HoldGold - Float;

        if (spare <= 0)
        {
            return 0;
        }

        var given = merchant.GiveGold(member, spare);

        Gathered += given;

        // The guild's takings in whoever happens to stand here, not pay for the errand that brought it. See
        // BotYield.Aside.
        BotYield.Aside(member, -given);

        return given;
    }

    /// <summary>
    /// Pays the shopkeeper enough to keep it standing, out of the guild's money.
    ///
    /// <para>
    /// The engine destroys a vendor the first time its bill is bigger than its purse, and the bill grows with
    /// the value of what it is holding — so the better the guild's shop is doing, the sooner an unpaid
    /// shopkeeper walks off with the stock. This is the one place a hall's merchant is fed, and it is asked
    /// every time a member is standing at it anyway.
    /// </para>
    /// </summary>
    public static int Wage(BotMobile member, PlayerVendor merchant)
    {
        if (member == null || merchant is not { Deleted: false })
        {
            return 0;
        }

        var purse = merchant.BankAccount + merchant.HoldGold;

        if (purse >= Wages)
        {
            return 0;
        }

        var owing = Float - purse;
        var pack = member.Backpack;

        if (pack == null || owing <= 0)
        {
            return 0;
        }

        // The guild's money, fetched the way every guild purchase on this shard fetches it. If the guild
        // cannot raise it the shopkeeper simply goes unpaid this time round, which is not fatal until the
        // next tick of a two-hour timer.
        var wealth = BotYield.Wealth(member);

        if (wealth < owing && !BotGuilds.Stand(member, owing - wealth))
        {
            return 0;
        }

        var carried = pack.GetAmount(typeof(Gold));

        if (carried < owing && !Banker.Withdraw(member, owing - carried))
        {
            SetAside(member, wealth);

            return 0;
        }

        if (!pack.ConsumeTotal(typeof(Gold), owing))
        {
            SetAside(member, wealth);

            return 0;
        }

        merchant.BankAccount += owing;

        Waged += owing;

        SetAside(member, wealth);

        return owing;
    }

    /// <summary>
    /// Books what the wage did to the member as aside: its own share of a guild's bill when it was paid, or the guild's
    /// money it was handed and kept when the payment fell over after the draw. <see cref="BotGuilds.Stand"/> books the
    /// mates' side, and the member's when the draw itself falls short. See <see cref="BotYield.Aside"/>.
    /// </summary>
    private static void SetAside(Mobile member, int before) => BotYield.Aside(member, before - BotYield.Wealth(member));

    /// <summary>What stands on every guild counter on the island, for the estate's line.</summary>
    public static (int Shops, int Lots, int Worth, int Purse) Standing()
    {
        var shops = 0;
        var lots = 0;
        var worth = 0;
        var purse = 0;

        foreach (var hall in BotEstate.Halls)
        {
            var merchant = Of(hall);

            if (merchant == null)
            {
                continue;
            }

            shops++;
            purse += merchant.BankAccount + merchant.HoldGold;

            var pack = merchant.Backpack;

            if (pack == null)
            {
                continue;
            }

            var held = pack.Items;

            for (var i = 0; i < held.Count; i++)
            {
                var vi = merchant.GetVendorItem(held[i]);

                if (vi is not { Valid: true, IsForSale: true })
                {
                    continue;
                }

                lots++;
                worth += vi.Price;
            }
        }

        return (shops, lots, worth, purse);
    }

    public static string Describe()
    {
        var (shops, lots, worth, purse) = Standing();

        return $"{shops} guild counters holding {lots} lots worth {worth}gp with {purse}gp in their tills; "
            + $"{Stocked} lots put out for {StockedWorth}gp, {Refused} turned away for want of room, "
            + $"{Lost} that went in and could not be found again, {Unpriced} the engine opened no lot for "
            + $"({Unstacked} of the refusals had a lot of the kind standing, which a pack at its item cap will not let them join, {Slotless} had neither lot nor slot), "
            + $"{Taken} bought back off them for {Paid}gp, {Gathered}gp carried into the guilds and {Waged}gp paid in wages";
    }

    public static void Forget()
    {
        Stocked = 0;
        StockedWorth = 0;
        Refused = 0;
        Lost = 0;
        Unpriced = 0;
        Unstacked = 0;
        Slotless = 0;
        Taken = 0;
        Paid = 0;
        Gathered = 0;
        Waged = 0;
    }

    /// <summary>Every kind a guild counter is holding, so the shopper can be asked one question per bot.</summary>
    public static void Kinds(PlayerVendor merchant, HashSet<Type> into)
    {
        var pack = merchant?.Backpack;

        if (pack == null || into == null)
        {
            return;
        }

        var lots = pack.Items;

        for (var i = 0; i < lots.Count; i++)
        {
            if (!lots[i].Deleted)
            {
                into.Add(lots[i].GetType());
            }
        }
    }
}
