using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a trip to a counter with goods the population is not asking for: out of the bot's own bank box while it stands at a
/// bank, or out of a crowded pack.
///
/// <para>
/// <b>The island decides who comes here, and it decides with facts it was already keeping.</b> Under the auction the test was a
/// stall that had never sold one in ten minutes. The stalls went on 01.10.2026 (Patrick's order of 30.09: the auction removed,
/// every purchase hand to hand), and the same sentence — "nobody here wants this" — is now read where buyers say it: no bot has a
/// want out for the kind (<see cref="BotMarket.Bespoke"/>) and, for a bot that keeps shop, none has been short of it lately
/// (<see cref="BotShopkeep.Demanded"/>). What the island does ask for is left for a shop by the bank or a carrier; what it does
/// not goes to a shopkeeper, the world's own shops, which stay. A fighter keeps no shop, so its goods the island is merely short
/// of go over a counter too, where the bots short of them buy.
/// </para>
///
/// <para>
/// <b>And it can only ever offer what the bot itself put aside.</b> The bank box holds nothing but the porter's surplus
/// (<c>BotUnload.Store</c>), and the pack is read against the bot's own keep list, so a bot's tools, herbs, paper and bandages
/// are never candidates — the first version sold the same shopkeeper the same reagents four thousand times because nothing
/// distinguished "goods" from "the things I need to do my job". The box is opened only at a bank, where a player opens it.
/// </para>
/// </summary>
public sealed class BotPeddler : IBotProposer
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPeddler));

    private static bool _saidNoBuyer;

    // ---- Every gate, counted apart. -------------------------------------------------------------------------------------------

    /// <summary>Bots looked at for something to peddle.</summary>
    public static long Asked { get; private set; }

    /// <summary>Bots standing at a bank whose box was looked through.</summary>
    public static long AtBank { get; private set; }

    /// <summary>Kinds left alone because somebody within a carrier's reach has said a want for them, or (a keeper's) the island has been short of them lately.</summary>
    public static long Spoken { get; private set; }

    /// <summary>Kinds with no shopkeeper in reach who buys the thing.</summary>
    public static long NoBuyer { get; private set; }

    /// <summary>Trips to a counter actually offered, and of those the ones out of a bank box and out of a crowded pack.</summary>
    public static long Offered { get; private set; }

    public static long FromBox { get; private set; }

    public static long FromPack { get; private set; }

    public static string Describe() =>
        Asked == 0
            ? "nobody has been offered a trip to a counter with goods"
            : $"{Asked} looks for something to peddle: {Offered} trips to a counter offered, {FromBox} of them out of a bank box at a bank ({AtBank} boxes looked through, {FullLoads} loads out of a box at least {FullShare:P0} full) and {FromPack} out of a pack ({BoxedOutLoads} of them capped surplus its full bank box had no place for); "
              + $"{Spoken} kinds left alone because somebody asks for them, {NoBuyer} found no shopkeeper in reach who buys the thing, {Petty} were worth less than the walk (under {PettyGold}gp or {PettyPerTile:F1}gp a tile); "
              + $"box loads: {WeightCut} cut to what the pack carries under {BotUnload.Heavy:P0} of its weight, {BarCut} cut to {BarLoad} bars, {TooHeavy} kinds left in the box for a pack already at that line; "
              + $"hunters' loot{(HunterSells ? "" : " (off)")}: {LootInTown} loads offered standing in a town and {LootLaden} outside one with {LootLoad}gp or more of it; {BotPeddle.AlsoSold} visits sold other loot the same shopkeeper buys ({BotPeddle.AlsoUnits} things for {BotPeddle.AlsoGold}gp)";

    public string Name => "Peddler";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>
    /// The least a load must be worth to be walked to a shopkeeper at all. Sixteen deaths doing peddle on build 349, twelve of them
    /// carrying a handful of reagents across the island: a load worth less than this, or than <see cref="PettyPerTile"/> gold for
    /// every tile to the counter, is not walked.
    /// </summary>
    public static int PettyGold { get; set; } = 15;

    /// <summary>And the least for every tile of the walk to the counter.</summary>
    public static double PettyPerTile { get; set; } = 0.1;

    /// <summary>Loads not peddled for being worth less than the walk. See <see cref="PettyGold"/>.</summary>
    public static long Petty { get; private set; }

    /// <summary>How full a pack must be, of the engine's count of things, before its own surplus is peddled from it.</summary>
    public static double CrowdedShare { get; set; } = 0.5;

    /// <summary>How often one bot's pack or box is read for this. A walk of a container is not free.</summary>
    public static int PackLookMs { get; set; } = 60000;

    private static readonly Dictionary<Serial, long> _looked = [];

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || !body.Alive)
        {
            return null;
        }

        Asked++;

        var now = Core.TickCount;

        if (_looked.TryGetValue(body.Serial, out var at) && now - at < PackLookMs)
        {
            return null;
        }

        if (_looked.Count > 1024)
        {
            _looked.Clear();
        }

        _looked[body.Serial] = now;

        // <b>The box first, standing at a bank (01.10.2026).</b> What the porter put there is the bot's whole surplus; the part the
        // island is asking for waits there for a shift at the shop by the bank, and the rest goes to a shopkeeper from here.
        if (BotMarket.AtBank(body) && body.FindBankNoCreate() is { } box && box.Items.Count > 0)
        {
            AtBank++;

            // The bot's own kinds are left in the box: the counter is shown every one of a kind in the pack, the kit's included.
            // Unless the box is full (06.10.2026, build next): see FullShare.
            var full = box.MaxItems > 0 && box.TotalItems >= box.MaxItems * FullShare;
            var drawn = Weigh(bot, body, map, box, BotUnload.Keeps(bot), true, full);

            if (drawn != null && full)
            {
                FullLoads++;
            }

            if (drawn != null)
            {
                return drawn;
            }
        }

        var pack = body.Backpack;

        if (pack == null || pack.MaxItems <= 0)
        {
            return null;
        }

        // <b>A hunter sells its loot on the way (02.10.2026, build next; Patrick's option 4).</b> A pack was read only when crowded —
        // half the engine's count of things — and a hunter's loot is a few stacks, so it rode in the pack until the porter put it in
        // the bank box or a death left it on the corpse: on build 392 (15:26–16:12) the fighting classes sold 130gp over counters,
        // everybody else 14,182gp, and they made 7 peddles against 42 trips to put loot in a box. A bot that keeps no shop is read when it stands in a town, or wherever it is once
        // what it carries is worth LootLoad at the market's price; a want within a carrier's reach still keeps its goods back.
        var crowded = pack.TotalItems >= pack.MaxItems * CrowdedShare;
        var keep = BotUnload.Keeps(bot);

        // <b>A pack that refused a hand-over is pressed to make room (06.10.2026, deliver16).</b> What it held for a shift at its shop
        // goes to a shopkeeper now, the kinds holding the most places first: the shop sells out of the box, and the pack is what
        // refuses the goods the bot itself asked for. See BotRoom.
        var room = BotRoom.Pressing(body);

        // <b>And a pack at four fifths of its places, before any refusal (06.10.2026, work17).</b> A maker's pieces were held for a
        // shift at its shop (Demanded) while the shift failed to open beside Britain's bank: Hrolf 100 leather caps, Varek 70, at
        // 125 of 125 things. Such a pack sells as a marked one does. See BotRoom.Packed.
        var packed = !room && BotRoom.Packed(body);

        room = room || packed;
        crowded = crowded || room;
        var hunter = false;
        var town = false;

        if (!crowded && HunterSells && Hunter(body))
        {
            town = BotTowns.Of(body.Location) != null;
            hunter = town || Loot(bot, body, pack, keep) >= LootLoad;
        }

        // <b>A capped surplus a full box has no place for (06.10.2026, box16).</b> The porter does not count what the box cannot take,
        // and this read a pack's keep-list kinds as never a load: Malachi's 66 draughts over, his box at 388 things of 125, had no
        // door for three and a half hours. Such a surplus goes to a shopkeeper (an alchemist buys a draught at 7, a healer a
        // bandage at 1, a mage a reagent at 2–3), the allowance stays (BotPeddle.Beyond), a want near keeps its units for HoldMs.
        _refused.Clear();

        var refused = BotUnload.Refused(bot, body, _refused);
        var boxedOut = refused >= BotUnload.KitSlack;

        if (!crowded && !hunter && !boxedOut)
        {
            return null;
        }

        var deed = Weigh(bot, body, map, pack, keep, false, false, refused > 0 ? _refused : null, room);

        if (deed != null && packed)
        {
            BotRoom.PackedLoad();
        }
        else if (deed != null && room)
        {
            BotRoom.Peddled();
        }

        if (deed is BotPeddle { BoxedOut: true })
        {
            BoxedOutLoads++;
        }

        if (deed != null && hunter)
        {
            if (town)
            {
                LootInTown++;
            }
            else
            {
                LootLaden++;
            }
        }

        return deed;
    }

    /// <summary>
    /// How full a bank box must be, of the engine's count of things, before what it holds of the bot's own capped kinds over one
    /// refill, and what the island asks for, may go to a shopkeeper (06.10.2026, build next).
    ///
    /// <para>
    /// <b>A full box refuses everything, and nothing here would empty one.</b> The box is where the porter puts every unit over
    /// the allowance, and a draught does not stack, so each is a place of the box's hundred and twenty-five. This proposer left
    /// a bot's own kinds in the box whatever their number, and held back whatever the island was short of for a shift at the shop —
    /// so a box full of spare draughts stayed full: 62,204 refusals against 10,255 lots stored in the eleven hours of 04.10.2026,
    /// and the surplus they refused rode in the packs ("1,619,781 things the porter left out of its count for having no room").
    /// One refill of each capped kind stays in the box for the bot's own restock (BotShopper takes it out at a bank); a want near
    /// still keeps its goods for a carrier.
    /// </para>
    /// </summary>
    public static double FullShare { get; set; } = 0.9;

    /// <summary>Loads drawn out of a box at <see cref="FullShare"/> or more. Of <see cref="FromBox"/>.</summary>
    public static long FullLoads { get; private set; }

    /// <summary>Loads of capped surplus out of a pack whose full box had no place for it (box16). Of <see cref="FromPack"/>.</summary>
    public static long BoxedOutLoads { get; private set; }

    /// <summary>The capped surplus a full box refuses, by kind, for the one look in hand. See <see cref="BotUnload.Refused"/>.</summary>
    private static readonly Dictionary<Type, int> _refused = [];

    /// <summary>Whether a bot not crowded, but keeping no shop, has its loot weighed for a counter. See <see cref="Hunter"/>.</summary>
    public static bool HunterSells { get; set; } = true;

    /// <summary>What a hunter's loot must be worth, at the market's price, before it walks to a counter from outside a town. Sixty.</summary>
    public static int LootLoad { get; set; } = 60;

    /// <summary>Loads of loot sold to a counter by a bot that keeps no shop: standing in a town, and laden past <see cref="LootLoad"/> outside one.</summary>
    public static long LootInTown { get; private set; }

    public static long LootLaden { get; private set; }

    /// <summary>A bot that keeps no shop and is no trader: a fighter, whose goods have no shift at the bank to wait for. See <see cref="BotShopkeeper.Merchant"/>.</summary>
    public static bool Hunter(Mobile body) =>
        body is BotMobile { Class: not null } && !BotShopkeeper.Merchant(body) && !BotTrading.Trader(body);

    /// <summary>
    /// What a pack's loot is worth at the market's price (<see cref="BotMarket.Worth"/>): everything movable, unbound and not on the
    /// bot's own keep list, that no want within a carrier's reach is waiting for. One walk of the pack, on the peddler's minute.
    /// </summary>
    private static int Loot(IBotWilful bot, Mobile body, Container pack, Dictionary<Type, int> keep)
    {
        var worth = 0;
        var items = pack.Items;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item == null || item.Deleted || !item.Movable || item is Gold || !item.IsStandardLoot() || !BotShopkeep.Merchandise(item, bot.Bond))
            {
                continue;
            }

            var kind = item.GetType();

            if (keep != null && keep.ContainsKey(kind) || BotCourier.Deliverable(bot, kind))
            {
                continue;
            }

            worth += BotMarket.Worth(kind, 1) * Math.Max(1, item.Amount);
        }

        return worth;
    }

    /// <summary>
    /// The best load in a container the island is not asking for and a shopkeeper in reach buys for more than the walk, as a
    /// peddle, or null. Kinds on the bot's own keep list (<paramref name="keep"/>) are never offered, from the box either: the
    /// counter is shown every one of a kind the pack holds, and a caster's own reagents are not a peddler's load.
    /// </summary>
    private static BotDeed Weigh(
        IBotWilful bot, Mobile body, Map map, Container from, Dictionary<Type, int> keep, bool box, bool full = false,
        Dictionary<Type, int> refused = null, bool room = false
    )
    {
        Dictionary<Type, (Item Sample, int Amount, int Lots)> kinds = [];
        var items = from.Items;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item == null || item.Deleted || !item.Movable || item is Gold || !item.IsStandardLoot()
                || !BotShopkeep.Merchandise(item, bot.Bond))
            {
                continue;
            }

            var kind = item.GetType();

            // A full box lets go of its own capped kinds over one refill (see FullShare); what is kept whole stays. A pack lets go of
            // the capped surplus its own full box has no place for (box16), over the allowance.
            if (keep != null && keep.TryGetValue(kind, out var cap) && (!full || cap >= int.MaxValue)
                && (refused == null || !refused.ContainsKey(kind)))
            {
                continue;
            }

            kinds[kind] = kinds.TryGetValue(kind, out var had)
                ? (had.Sample, had.Amount + Math.Max(1, item.Amount), had.Lots + 1)
                : (item, Math.Max(1, item.Amount), 1);
        }

        if (kinds.Count == 0)
        {
            return null;
        }

        BotShops.Survey(map, body.Location);

        // A maker or gatherer keeps shop by the bank, so what the island is short of waits for its shift; a fighter keeps none, so
        // only a spoken want (a carrier's road) holds its goods back from a shopkeeper. See BotShopkeeper.Merchant.
        // A full box has no shift to wait for: its shop sells from a box that takes nothing more in (06.10.2026, build next).
        var keeps = !full && !room && (BotShopkeeper.FightersKeepShop || BotShopkeeper.Merchant(body));

        // The kinds that hold the most places first when the box is full: they are what frees it.
        List<(Type Kind, Item Sample, int Amount, int Lots)> order = [];

        foreach (var (kind, (sample, amount, lots)) in kinds)
        {
            var units = amount;

            if (full && keep != null && keep.TryGetValue(kind, out var reserve))
            {
                units -= reserve;
            }

            // The capped surplus a full box refuses: what is over the allowance (Refused counted it), less what a want near holds in
            // the pack for HoldMs, the porter's own reading (BotUnload.Holds).
            if (refused != null && refused.TryGetValue(kind, out var over))
            {
                units = Math.Min(units, over) - BotUnload.Holds(bot, kind, over);
            }

            if (units > 0)
            {
                order.Add((kind, sample, units, lots));
            }
        }

        if (full || room)
        {
            order.Sort((a, b) => b.Lots.CompareTo(a.Lots));
        }

        for (var o = 0; o < order.Count; o++)
        {
            var (kind, sample, amount, _) = order[o];

            var letGo = refused != null && refused.ContainsKey(kind);
            var trimmed = false;

            // <b>A want a carrier from here could answer, not a want anywhere (01.10.2026, build 384 review).</b> The gate read
            // BotMarket.Bespoke — any open want on the shard — while the carrier's road out of this pack or box reaches only a buyer
            // within BotCourier.Reach on the same land. A tailor's want for leather stands nearly always (BotStores asks whenever
            // it holds under twenty), so a fighter's leather in its box at Yew's bank, wanted only in Britain 1000 tiles off, was
            // neither carried nor sold: frozen for the life of the shard, with every other kind somebody far away asked for. The
            // porter keeps goods by the same local reading (BotUnload.Store, BotCourier.Deliverable); the peddler now lets go by it.
            // The capped surplus a full box refuses has had its want's share taken off above, and no shift at the shop to wait for:
            // the shop sells out of a box that takes nothing in.
            // A pack pressed for room (marked, or at BotRoom.PackedShare of its places) keeps for a want near only what that want
            // holds (BotUnload.Holds, the porter's reading), not the whole kind: one want for a cap held a hundred caps (work17).
            if (!letGo && room && !box && BotCourier.Deliverable(bot, kind))
            {
                amount -= BotUnload.Holds(bot, kind, amount);
                trimmed = true;

                if (amount <= 0)
                {
                    Spoken++;

                    continue;
                }
            }
            else if (!letGo && (BotCourier.Deliverable(bot, kind) || keeps && BotShopkeep.Demanded(kind)))
            {
                Spoken++;

                continue;
            }

            // A load drawn out of the box at a bank may be drawn to the capital's counter by its price; one carried in a crowded
            // pack goes to the best buyer at its own price. See BotCapital.Pull.
            var shop = BotShops.Buyer(bot, sample, amount, box, out var price, out var factor, out var nearer);

            if (shop == null)
            {
                NoBuyer++;
                Missing(BotMarket.Label(sample), map);

                // Neither the box nor any shopkeeper in reach takes it: counted by kind, the residue a guildmate's hand would be for.
                if (letGo)
                {
                    BotSurplus.CountOnce(BotSurplus.Stranded, body, kind, amount);
                }

                continue;
            }

            // <b>A load the bot can walk with, and coloured bars no faster than the counters' purse (06.10.2026, ingots16).</b> A load
            // out of a box was the whole stack: 2,479 iron bars (248 stones) drawn at 09:42 on 06.10 made the bot too heavy, the
            // porter put them back, and the peddle ended "no Iron Ingot left in the pack". A box load now holds what the pack carries
            // under the porter's own line (BotUnload.Heavy), bars at most BarLoad; a coloured load no more than the hour's purse
            // has left (BotOre.CounterRoom), and none when it is spent.
            var cut = trimmed;

            if (box)
            {
                var carry = Carryable(body, sample);

                if (carry <= 0)
                {
                    TooHeavy++;

                    continue;
                }

                if (amount > carry)
                {
                    amount = carry;
                    WeightCut++;
                    cut = true;
                }

                if (sample is BaseIngot && BarLoad > 0 && amount > BarLoad)
                {
                    amount = BarLoad;
                    BarCut++;
                    cut = true;
                }
            }

            var purse = BotOre.CounterRoom(body, kind, price);

            if (purse <= 0)
            {
                BotOre.Shut();

                continue;
            }

            var purseCut = amount > purse;

            if (purseCut)
            {
                amount = purse;
                cut = true;
            }

            var tiles = Math.Max(Math.Abs(shop.X - body.X), Math.Abs(shop.Y - body.Y));

            if (amount * price * factor < Math.Max(PettyGold, tiles * PettyPerTile))
            {
                Petty++;

                continue;
            }

            Offered++;

            if (box)
            {
                FromBox++;
            }
            else
            {
                FromPack++;
            }

            // Walked past a nearer buyer to the capital's counter: counted as offered here, and as carried when it sells there.
            var passed = factor > 1.0 && nearer >= 0 && tiles > nearer ? tiles - nearer : 0;

            if (passed > 0)
            {
                BotCapital.Offer();
            }

            // A hunter's other loot this shopkeeper buys goes over on the same visit, and the walk is claimed with it (build next).
            var extra = Hunter(body) ? BotPeddle.RestWorth(bot, body, shop, kind) : 0;

            // The coloured load's gold is held back from other bots' offers until it sells. See BotOre.Hold.
            BotOre.Hold(body, kind, amount * price, purseCut);

            // A load of capped surplus sells no more than the units counted here: the rest of the kind over the allowance is a want's.
            // A load cut to the pack's weight, to BarLoad or to the purse sells no more than its cut either.
            return new BotPeddle(shop, kind, BotMarket.Label(sample), amount, price, box, factor, passed)
            {
                Extra = extra, BoxedOut = letGo, Most = letGo || cut ? amount : 0
            };
        }

        return null;
    }

    /// <summary>
    /// The most bars one load out of a bank box holds, whatever the pack could carry: three hundred (thirty stones; 900gp of a
    /// three-gold coloured metal, 1,200gp of iron). Nought: the pack's weight alone.
    /// </summary>
    public static int BarLoad { get; set; } = 300;

    /// <summary>Box loads cut to what the pack carries under the porter's line, cut to <see cref="BarLoad"/>, and kinds left in the box for a pack already at the line.</summary>
    public static long WeightCut { get; private set; }

    public static long BarCut { get; private set; }

    public static long TooHeavy { get; private set; }

    /// <summary>
    /// How many of these the bot can take out of its box before what it carries reaches the porter's own line
    /// (<see cref="BotUnload.Heavy"/> of <see cref="BotLadder.Ceiling"/>, the body counted as the engine counts it), past which the
    /// porter would put them straight back. Anything that weighs nothing is not limited.
    /// </summary>
    private static int Carryable(Mobile body, Item sample)
    {
        var weight = sample?.Weight ?? 0.0;

        if (body == null || weight <= 0.0)
        {
            return int.MaxValue;
        }

        var spare = BotLadder.Ceiling(body) * BotUnload.Heavy - BotLadder.Load(body);

        if (spare <= 0.0)
        {
            return 0;
        }

        return (int)Math.Min(int.MaxValue, Math.Floor(spare / weight));
    }

    private static void Missing(string label, Map map)
    {
        if (_saidNoBuyer)
        {
            return;
        }

        _saidNoBuyer = true;

        // Once, by name. Goods that no bot wants and no shopkeeper buys stay in a bank box for the life of the shard.
        logger.Error(
            "No shopkeeper within reach of the bots on {Map} buys {Item}, and no bot wants it either; it will stay where it is",
            map,
            label
        );
    }

    public static void Forget()
    {
        _saidNoBuyer = false;
        Asked = 0;
        AtBank = 0;
        Spoken = 0;
        NoBuyer = 0;
        Offered = 0;
        FromBox = 0;
        FullLoads = 0;
        BoxedOutLoads = 0;
        FromPack = 0;
        Petty = 0;
        LootInTown = 0;
        LootLaden = 0;
        WeightCut = 0;
        BarCut = 0;
        TooHeavy = 0;
        BotPeddle.ForgetCounts();
        _looked.Clear();
    }
}
