using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Items;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Whether the guild's counter is short of something its members keep running out of, and who is going to
/// fetch it.
///
/// <para>
/// The fourth of the estate's officers and the same shape as the other three — any member may go, one at a
/// time per guild, and what the guild can afford is asked here rather than inside the errand. The order that
/// falls out of the costs is a hall, then the benches its trade needs, then a shopkeeper, then something for
/// the shopkeeper to sell; nothing enforces it, and nothing needs to.
/// </para>
///
/// <para>
/// <b>It stocks what has actually been asked for.</b> <see cref="BotShopper"/> writes down every shortage it
/// meets, so this reads a measurement rather than a list: whatever the population has most often reached for
/// and not found is what the guild's shelf gets. That is the difference between a store cupboard and a
/// warehouse of things nobody wanted, which is what the first sketch of this would have built — the ignored
/// stalls, by construction the goods the whole island has already refused.
/// </para>
///
/// <para>
/// <b>And it must never become a second faucet.</b> Nothing here sells to a shopkeeper; it only buys, at the
/// shelf price, exactly as a member would have. So the errand is gold-neutral against the alternative and
/// the only thing it changes is how far somebody walks. See <c>BotPeddle</c>, which is the shard's one
/// faucet and stays that way.
/// </para>
/// </summary>
public sealed class BotSupplier : IBotProposer
{
    /// <summary>How long a claim lasts after the bot holding it was last heard from.</summary>
    public static int ClaimMs { get; set; } = 240000;

    /// <summary>
    /// How many of a kind a guild counter should be holding before it stops being restocked.
    ///
    /// Sixty, which is three of the batches below: enough that a guild of ten can each take a packet twice
    /// over before anybody has to go back to town, and not so much that the shelf's value drives the
    /// shopkeeper's wage bill up faster than the shelf earns.
    /// </summary>
    public static int Shelf { get; set; } = 60;

    /// <summary>The most units in one lot. A packet, in the sense a shop sells packets.</summary>
    public static int Batch { get; set; } = 20;

    /// <summary>
    /// About what one trip is allowed to spend of the guild's money.
    ///
    /// <para>
    /// Whichever of this and <see cref="Batch"/> gives the smaller number wins, so dear things come back a
    /// few at a time and cheap ones by the packet.
    /// </para>
    ///
    /// <para>
    /// <b>What this used to claim it prevented, it cannot prevent, and the example it named was the exact
    /// case it never touches.</b> The note said this was what stops "a guild spending four thousand gold on
    /// twenty sewing kits" - which would need a kit to cost two hundred gold. A kit costs three, so this
    /// cap yields eighty and <see cref="Batch"/> decides; the cap only ever bites on expensive goods, and
    /// the thing it was written about is precisely the thing it cannot reach. On 09.09.2026 the population
    /// bought 360 sewing kits at three gold apiece, every lot the maximum twenty, and not one of them was
    /// ever near this limit.
    /// </para>
    /// </summary>
    public static int Spend { get; set; } = 240;

    /// <summary>
    /// How many of a <em>tool</em> kind a hall should be holding before it stops being restocked.
    ///
    /// <para>
    /// <b>One number cannot mean the same thing for a reagent and for a tool, and <see cref="Shelf"/> was
    /// being asked to.</b> Sixty sulfurous ash is about an hour of this population's casting. Sixty sewing
    /// kits is sixty times twenty-five to seventy-five uses - three thousand acts of tailoring on one
    /// shelf, in a session that saw two and a half thousand across the whole island, and four halls each
    /// holding that. The kinds are not comparable and the target must not be either.
    /// </para>
    ///
    /// <para>
    /// Ten, which is about one apiece for a guild at full strength (see <c>BotGuilds</c>: five to fifteen)
    /// with a few spare. Anything the engine counts uses on is judged by this instead of by the shelf.
    /// </para>
    /// </summary>
    public static int Tools { get; set; } = 10;

    /// <summary>How many of this kind a hall should hold: <see cref="Tools"/> for a tool, <see cref="Shelf"/> for everything else.</summary>
    public static int Enough(Type kind) =>
        kind != null && typeof(IUsesRemaining).IsAssignableFrom(kind) ? Tools
        : kind != null && !Stacks(kind) ? Math.Min(Shelf, Loose)
        : Shelf;

    /// <summary>
    /// How many of a kind that does not stack a hall should hold (06.10.2026, build next).
    ///
    /// <para>
    /// <b>A draught does not stack in this era, so every one of them is a lot and a slot.</b> <see cref="Shelf"/> is written for
    /// packets: sixty bandages are one lot on a merchant's hundred and twenty-five. Sixty heal potions are sixty lots, and two kinds
    /// of draught at the shelf's number would fill the counter on their own. Ten, the number the tools already keep.
    /// </para>
    /// </summary>
    public static int Loose { get; set; } = 10;

    /// <summary>Whether one of a kind stacks onto another, asked of a sample once and remembered. See <see cref="Fits"/>.</summary>
    public static bool Stacks(Type kind)
    {
        if (kind == null)
        {
            return true;
        }

        if (!_stacks.TryGetValue(kind, out var stacks))
        {
            var sample = kind.CreateInstance<Item>();

            stacks = sample?.Stackable ?? true;
            sample?.Delete();
            _stacks[kind] = stacks;
        }

        return stacks;
    }

    private static readonly Dictionary<Type, bool> _stacks = [];

    /// <summary>Lots cut down to the merchant's free places because the kind does not stack. See <see cref="Stacks"/>.</summary>
    public static long Slotted { get; private set; }

    /// <summary>What one of a kind weighs, worked out once and remembered. See <see cref="Fits"/>.</summary>
    private static readonly Dictionary<Type, double> _stones = [];

    /// <summary>
    /// How many of a kind this bot could take on and still be able to walk.
    ///
    /// <para>
    /// <b>The proposal asked whether the bot was already full and never whether the lot would fill it.</b>
    /// A courier at a hundred stones of a hundred and thirty passes that test and then buys twenty sewing
    /// kits at two stones each, and is over its ceiling the instant it pays. What happens next is the whole
    /// of the 09.09.2026 regression: the load now outranks price rather than discounting it, so the
    /// unloading errand takes the work off it on the spot and sells the goods the guild has just paid for.
    /// Eighty supply errands were dropped after the money was spent that evening against ten the hour
    /// before, thirty-eight of them to the unloading.
    /// </para>
    ///
    /// <para>
    /// A quarter of the headroom is left free, because the courier still has to draw the coins to pay with
    /// and may pick something up on the way. The weight comes from a sample of the item itself rather than
    /// a guess; <c>CreateInstance</c> is the idiom this assembly already uses for that, and the answer is
    /// cached because it cannot change.
    /// </para>
    /// </summary>
    public static int Fits(Mobile body, Type kind)
    {
        if (body == null || kind == null)
        {
            return 0;
        }

        var room = (BotLadder.Ceiling(body) - BotLadder.Load(body)) * 0.75;

        if (room <= 0.0)
        {
            return 0;
        }

        if (!_stones.TryGetValue(kind, out var each))
        {
            var sample = kind.CreateInstance<Item>();

            each = sample?.Weight ?? 1.0;
            sample?.Delete();

            // A weightless kind would divide the headroom by nothing. Anything the engine says weighs
            // nothing cannot be what is holding a bot down, so a floor here costs nothing and the division
            // below is then always safe.
            each = Math.Max(0.1, each);
            _stones[kind] = each;
        }

        return (int)(room / each);
    }

    /// <summary>How many of the population's shortages are considered before giving up on this beat.</summary>
    public static int Considered { get; set; } = 8;

    // ---- Every gate, counted apart, from the first line. This project has paid for the other habit often
    // enough that a new proposer arriving without counters would be the defect. --------------------------

    /// <summary>Bots looked at for a supply run.</summary>
    public static long Asked { get; private set; }

    /// <summary>Runs offered.</summary>
    public static long Offered { get; private set; }

    /// <summary>Times the guild had no counter to stock.</summary>
    public static long Shopless { get; private set; }

    /// <summary>Times another member was already on it.</summary>
    public static long Claimed { get; private set; }

    /// <summary>Times the shelf held enough of everything the population is short of.</summary>
    public static long Stocked { get; private set; }

    /// <summary>Times nobody within reach sells the thing the shelf is short of.</summary>
    public static long Unsold { get; private set; }

    /// <summary>Times the guild could not raise the price of a batch.</summary>
    public static long Wanting { get; private set; }

    /// <summary>What the best-off guild that wanted a batch actually had. See <c>BotHirer.Nearest</c>.</summary>
    public static int Nearest { get; private set; }

    /// <summary>And how short it was.</summary>
    public static int Short { get; private set; }

    /// <summary>Times the population had not been short of anything yet, so there was nothing to stock.</summary>
    public static long Unmeasured { get; private set; }

    /// <summary>
    /// Times the bot could not have carried the errand and was not sent.
    ///
    /// <para>
    /// <b>The question the errand fails on, asked here instead — and leaving it unasked cost the shard its
    /// completion band twice over.</b> A guild's courier draws the price out of the bank and has to hold it
    /// on the way to the counter; a bot already at its carrying ceiling cannot hold the coins, so
    /// <c>BotShops.Buy</c> refuses with "the pack would not hold the Ngp it drew to pay with", the errand
    /// fails, its claim on the guild is released the same instant, and the next beat offers the same errand
    /// to the same bot. Measured 09.09.2026 at 18:02: <b>1,955 of the window's 2,012 failures were that one
    /// line</b>, about four hundred a minute, and the shard reported 9% of work finished.
    /// </para>
    ///
    /// <para>
    /// The appraisal already discounts work a laden bot cannot start — <c>BotAppraisal.StoppedShare</c> — but
    /// it is a factor under a fifth root, so a fiftieth of 260 a minute still beats most of what else is on
    /// offer. A factor is the right shape for "this is a poor idea" and the wrong shape for "this cannot
    /// happen", which is the distinction this project keeps having to relearn in both directions.
    /// </para>
    /// </summary>
    public static long Laden { get; private set; }

    /// <summary>Asked of a bot underground, which is offered nothing: the hall is on the island. See <see cref="Propose"/>.</summary>
    public static long Below { get; private set; }

    /// <summary>
    /// Times a lot was cut smaller than <see cref="Batch"/>, and the tally of how many units that saved.
    ///
    /// Its own bucket rather than being inferred from the spend: a lot cut because the shelf only wanted
    /// four more and a lot cut because the courier could only carry four are the same saving and completely
    /// different facts, and reading this beside <see cref="Laden"/> is what tells them apart.
    /// </summary>
    public static long Trimmed { get; private set; }

    /// <summary>Units not bought because the shelf or the courier did not have room for them.</summary>
    public static long Spared { get; private set; }

    /// <summary>
    /// Times the shelf had no slot left for the kind that was wanted.
    ///
    /// Its own bucket and not a share of <see cref="Stocked"/>: "the shelf holds enough of this" is the good
    /// answer and "the shelf cannot hold anything else at all" is a fault in the hall, and folding the second
    /// into the first is how eight hours passed with the estate line reporting healthy shelves while a
    /// thousand couriers came home loaded. See <c>BotShelf.Room</c>.
    /// </summary>
    public static long Cramped { get; private set; }

    /// <summary>
    /// Times the shelf still had room when the courier set out and none by the time it had walked to the
    /// shop, so the errand was given up before the guild's money was spent.
    ///
    /// Apart from <see cref="Cramped"/> deliberately. That one is the supplier reading a full shelf and not
    /// sending anybody, which costs nothing; this is a walk already made, and if it ever climbs to the same
    /// order as the other then the hall is filling faster than a courier can cross Britain, which is a fact
    /// about the hall rather than about the courier.
    /// </summary>
    public static long Closed { get; private set; }

    /// <summary>Supply runs sent to a counter in the hall's own town. See <see cref="Propose"/>.</summary>
    public static long HomeShops { get; private set; }

    /// <summary>Counts one of those.</summary>
    public static void Shut() => Closed++;

    /// <summary>This officer's name in the shared register of who is on what. See <see cref="BotOffice"/>.</summary>
    public const string Office = "supplier";

    public string Name => "supplier";

    public BotStanding Rung => BotStanding.Free;

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotEstate.Running || !BotEstate.Merchanting || bot?.Self is not BotMobile { Deleted: false } body)
        {
            return null;
        }

        if (body.Map == null || body.Map == Map.Internal || body.Guild is not Guild guild)
        {
            return null;
        }

        var hall = BotEstate.Hall(guild);

        if (hall is not { Deleted: false } || hall.Map != body.Map)
        {
            return null;
        }

        Asked++;

        // <b>Not from underground, where the hall is not.</b> The dungeons are a block of their own with no road to the
        // island (see BotDungeon), and a run bought there cannot be carried home: Isolde of The Crown, standing at
        // (5313, 1307) on 24.09.2026, bought 17 skinning knives off the nearest shopkeeper at 10:13:55 and failed "no way
        // through to Alvita … inside a house" a minute and a half later, as she had at 09:59 — 238gp of guild money in a
        // pack that could not reach the counter.
        if (BotDungeon.Under(body.Location))
        {
            Below++;

            return null;
        }

        // Nothing about this errand works for a bot that cannot carry any more: not the coins it draws to
        // pay with, not the goods it buys. See Laden.
        if (BotLadder.Load(body) >= BotLadder.Ceiling(body))
        {
            Laden++;

            return null;
        }

        var merchant = BotShelf.Of(hall);

        if (merchant == null)
        {
            Shopless++;

            return null;
        }

        var now = Core.TickCount;

        if (BotOffice.Busy(Office, guild))
        {
            Claimed++;

            return null;
        }

        // <b>Both tallies, because neither one sees the whole of what this population cannot get.</b> The
        // shopper's list is what bots were short of in their own kit — bandages, reagents, tools. The town's
        // is what a counter actually turned somebody away for, which is where a crafter's raw material shows
        // up: brewing was failing eight times in ten minutes for want of a bottle, and no bottle appeared in
        // the shopper's list at all because a bottle is a brewer's material rather than its kit.
        //
        // What the guild counter can do about a town that has run out is not conjure any: it is to buy the
        // batch the moment the shelf refills, so five brewers draw from the hall over the hour instead of
        // racing each other to the counter and four of them losing. Same argument as the walk to Britain,
        // applied to scarcity rather than to distance.
        var wants = BotShopper.Shortages();
        var dry = BotShops.Dry();

        for (var i = 0; i < dry.Count; i++)
        {
            wants.Add(dry[i]);
        }

        wants.Sort((a, b) => b.Times.CompareTo(a.Times));

        if (wants.Count == 0)
        {
            Unmeasured++;

            return null;
        }

        // Whatever this bot is standing in the middle of, swept once for everybody — the same sweep the
        // shopper makes, and it is what makes Nearest able to answer at all. And the hall's own town: see HomeShops.
        BotShops.Survey(body.Map, body.Location);
        BotShops.Survey(hall.Map, hall.Location);

        var looked = Math.Min(Considered, wants.Count);
        var full = 0;
        var unsold = 0;
        var poor = 0;
        var cramped = 0;

        for (var i = 0; i < looked; i++)
        {
            var kind = wants[i].Kind;

            var held = kind == null ? 0 : BotShelf.Held(merchant, kind);

            if (kind == null || held >= Enough(kind))
            {
                full++;

                continue;
            }

            // <b>And whether the shelf has anywhere to put it, which is not the same question.</b> See
            // BotShelf.Room: a counter at the engine's hundred-and-twenty-five-item cap holds none of this
            // kind and has no slot for it either, and every courier sent to one spends the guild's money in
            // town and then keeps the goods for the rest of the session.
            if (!BotShelf.Room(merchant, kind))
            {
                cramped++;

                continue;
            }

            // <b>The guild's trader buys in the guild's own town (01.10.2026; Patrick, 30.09: "a guild's traders prefer to
            // trade in their home town").</b> The counter nearest the hall rather than the one nearest wherever the courier
            // happens to stand: the hall's shelf is stocked from its own town's shops, and the walk home is the short one.
            var shop = BotShops.Nearest(bot, kind, hall.Location);

            if (shop == null)
            {
                unsold++;

                continue;
            }

            var home = BotTowns.Of(shop.Location) is { } town && town == BotTowns.Of(hall.Location);

            var price = BotShops.Price(shop, kind);

            if (price <= 0)
            {
                unsold++;

                continue;
            }

            // <b>Three things now say how big a lot is, and only one of them used to.</b> The money cap
            // and the packet size were both about the shop; neither asked what the shelf still wanted nor
            // what the courier could carry, so a shelf four short of full was restocked with twenty, and a
            // courier with room for six was sent for twenty and could not walk afterwards. The smallest of
            // the four wins, which is the only combination of them that is never wrong.
            var wanted = Math.Max(1, Enough(kind) - held);
            var carriable = Math.Max(1, Fits(body, kind));
            var asked = Math.Min(Math.Min(Spend / price, Batch), Math.Min(wanted, carriable));

            // <b>And, for a kind that does not stack, the merchant's free places (06.10.2026, build next).</b> Each draught is a lot
            // of its own on the shelf, so a batch of sixteen needs sixteen places; bought past them, the rest rode in the courier's
            // pack for the rest of the session. See BotSupply.Advance.
            if (!Stacks(kind) && merchant.Backpack is { MaxItems: > 0 } shelf)
            {
                var free = Math.Max(1, shelf.MaxItems - shelf.TotalItems);

                if (asked > free)
                {
                    asked = free;
                    Slotted++;
                }
            }

            var batch = Math.Max(1, asked);

            if (batch < Batch)
            {
                Trimmed++;
                Spared += Batch - batch;
            }

            var fund = BotEstate.Fund(guild);

            if (fund < batch * price)
            {
                poor++;

                if (fund > Nearest)
                {
                    Nearest = fund;
                    Short = batch * price - fund;
                }

                continue;
            }

            Offered++;
            BotOffice.Offering(Office, guild);

            if (home)
            {
                HomeShops++;
            }

            return new BotSupply(guild, hall, shop, kind, batch, price);
        }

        // <b>One refusal counted, and it is the one that actually decided the beat.</b> Counting all three
        // would make the denominators lie: a look that considered eight kinds and found seven stocked and
        // one unaffordable is one refusal for want of money, not seven-and-one. The commonest reason wins,
        // and ties fall to the least alarming, because a shelf that is simply full is the good answer.
        if (cramped > 0 && cramped >= full && cramped >= unsold && cramped >= poor)
        {
            Cramped++;
        }
        else if (full >= unsold && full >= poor)
        {
            Stocked++;
        }
        else if (unsold >= poor)
        {
            Unsold++;
        }
        else
        {
            Wanting++;
        }

        return null;
    }

    /// <summary>The errand is alive and still on it. Called every beat by <c>BotSupply.Advance</c>.</summary>
    public static void Hold(Guild guild) => BotOffice.Hold(Office, guild, ClaimMs);

    /// <summary>The errand is over, however it went.</summary>
    public static void Release(Guild guild) => BotOffice.Release(Office, guild);

    /// <summary>Part of the estate's line.</summary>
    public static string Describe() =>
        Asked == 0
            ? "nobody has been looked at for a supply run"
            : $"the supplier looked {Asked} times and sent {Offered} ({BotSupply.FromChest}gp of it paid from the guilds' chests; {HomeShops} runs to a counter in the hall's own town): {Shopless} guilds had no counter, {Claimed} already had somebody on it, "
              + $"{Stocked} shelves held enough, {Unsold} wanted what nobody within reach sells, {Wanting} could not raise a batch"
              + (Wanting > 0 ? $" (the best-off had {Nearest}gp free and was {Short} short)" : "")
              + $", {Unmeasured} asked before the population had run short of anything, {Laden} could not have carried it, {Below} were underground, "
              + $"{Cramped} found the shelf with no room left for it ({Closed} more found it full on arrival at the shop)"
              + $"; {Trimmed} lots came back smaller than a packet, {Spared} units the shelf or the courier had no room for"
              + $"; {Slotted} lots of a kind that does not stack cut to the merchant's free places, {BotSupply.Loose} such things shelved one by one "
              + $"and {BotSupply.Unshelved} left in a courier's pack when the merchant would take no more";

    public static void Forget()
    {
        Trimmed = 0;
        HomeShops = 0;
        Spared = 0;
        Cramped = 0;
        Closed = 0;
        Asked = 0;
        Offered = 0;
        Shopless = 0;
        Claimed = 0;
        Stocked = 0;
        Unsold = 0;
        Wanting = 0;
        Unmeasured = 0;
        Nearest = 0;
        Short = 0;
        Laden = 0;
        Below = 0;
        Slotted = 0;
    }
}
