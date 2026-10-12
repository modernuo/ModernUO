using System;
using System.Collections.Generic;
using Server.Items;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Goods from one bot's pack into another's, and coin the other way, with the two standing together. The one place a trade
/// between two bots away from a shop happens.
///
/// <para>
/// <b>Patrick's order of 30.09.2026: every order and every purchase hand to hand.</b> The auction settled a fill between a
/// supplier in a mine and a buyer in a dungeon, the goods waiting in the market for the buyer's beat to fetch them. Here the
/// exchange is refused unless both are alive, on one map and within <see cref="MeetTiles"/> of each other — the distance two
/// people stand to pass a sack across — and the goods go from pack to pack and the coin from pack to pack.
/// </para>
///
/// <para>
/// <b>The auction's order is kept, because it is the whole of the money safety:</b> the buyer is charged first, all or nothing
/// (<see cref="BotMarket.Charge(Mobile, int, out int)"/>: coin out of the pack, then the account — a bot at work keeps a hundred on
/// it and the rest in the bank, so the balance behind the purse pays as a cheque would); then as much as the buyer's pack will
/// take is handed over; what did not move is refunded; and only then is the seller paid, as coin into its pack. Charged equals
/// paid plus refunded, always. How much went over as coin and how much was drawn on the account is counted apart.
/// </para>
///
/// <para>
/// <b>The buyer's purchase is booked outside the work it was doing.</b> A carrier walks up to a hunter in the middle of a hunt
/// and the hunter pays; the hunt must not learn from that coin that hunting loses money (<see cref="BotYield.Aside"/>, the
/// ledger's own rule for money moved by a decision outside the errand in hand).
/// </para>
/// </summary>
public static class BotHandover
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotHandover));

    /// <summary>How near the two must stand for goods and coin to change hands: two tiles, within arm's reach of a sack.</summary>
    public static int MeetTiles { get; set; } = 2;

    /// <summary>Meetings that ended in goods moving, and what moved.</summary>
    public static long Exchanges { get; private set; }

    public static long Units { get; private set; }

    public static long Gold { get; private set; }

    /// <summary>Of that gold, what went over as coin out of the buyer's pack, and what was drawn on its account.</summary>
    public static long FromPack { get; private set; }

    public static long ByCheque { get; private set; }

    /// <summary>Asked while the two were further apart than <see cref="MeetTiles"/>. Should stay nought: the walk comes first.</summary>
    public static long Apart { get; private set; }

    /// <summary>The buyer could not pay for one at the price.</summary>
    public static long Broke { get; private set; }

    /// <summary>
    /// Meetings that handed fewer units than were brought because the price over the offer would have reached into the money held
    /// for the units still wanted (03.10.2026, build next), and the units left with the seller. See <see cref="BotMarket.Affords"/>.
    /// </summary>
    public static long CutToFree { get; private set; }

    public static long CutUnits { get; private set; }

    /// <summary>Gold of hand-overs settled out of a brewer's advance rather than paid at the meeting (03.10.2026, econ13). See <see cref="BotAdvance"/>.</summary>
    public static long OnAdvance { get; private set; }

    /// <summary>The seller had none of the thing left to hand over.</summary>
    public static long Empty { get; private set; }

    /// <summary>The buyer's pack would not take all of it; the rest stayed with the seller and was refunded.</summary>
    public static long PackFull { get; private set; }

    /// <summary>One of the two was dead, gone or elsewhere.</summary>
    public static long Absent { get; private set; }

    /// <summary>Pieces a bot would have put on and left in its pack instead, being promised to a buyer. See <see cref="Promised"/>.</summary>
    public static long KeptForBuyer { get; private set; }

    /// <summary>Buyers that put on what they had just been handed, there at the meeting. See <see cref="Exchange"/>.</summary>
    public static long WornAtOnce { get; private set; }

    /// <summary>
    /// Whether this bot is carrying <paramref name="kind"/> for a buyer: the work in its hands, or the work it has put down to
    /// be taken up again, is a carrier (<see cref="IBotWantCarrier"/>) bringing that kind against a spoken want.
    ///
    /// <para>
    /// <b>A trader wore what it had bought for an order (01.10.2026, build 386).</b> <c>BotMobile.Dress</c> puts on whatever in
    /// the pack stops more than what is worn, on a clock, and the pack held the leather skirt the trader was walking to Imogen
    /// with: "Norna took off LongPants for LeatherSkirt" at 12:20:26, "nothing left to hand over" at 12:21:59. Twenty-two of the
    /// forty-one failed hauls of build 386 and five of the seventeen failed deliveries ended so, and every death stripped the
    /// body again, so it began again after each. A promised piece stays in the pack: asked here by the dresser and the re-arm,
    /// a cast and a type compare, no walk.
    /// </para>
    /// </summary>
    public static bool Promised(IBotWilful bot, Type kind)
    {
        if (bot?.Resolve is not { } resolve || kind == null)
        {
            return false;
        }

        var carrier = resolve.Deed as IBotWantCarrier ?? resolve.Paused?.Deed as IBotWantCarrier;

        if (carrier?.Goods != kind)
        {
            return false;
        }

        KeptForBuyer++;

        return true;
    }

    private static readonly Dictionary<Type, (long Units, long Gold)> _kinds = [];

    private static readonly List<(Item Item, int Take)> _lots = [];

    /// <summary>
    /// Hands up to <paramref name="units"/> of <paramref name="kind"/> from the seller's pack to the buyer's at
    /// <paramref name="price"/> apiece. Returns how many moved; <paramref name="paid"/> is what the seller was paid.
    /// </summary>
    /// <param name="aside">
    /// Whether the buyer is paying in the middle of work of its own rather than on a purchase errand: its coin is then booked
    /// outside that work (<see cref="BotYield.Aside"/>). True for a carrier's delivery; false for a buyer at a shop, whose own
    /// errand already counts the bill.
    /// </param>
    public static int Exchange(
        IBotWilful seller, Mobile buyer, Type kind, int units, int price, bool aside, string why, out int paid, out string refused
    )
    {
        paid = 0;
        refused = null;

        var giver = seller?.Self;
        var from = giver?.Backpack;
        var into = buyer?.Backpack;

        if (giver is not { Deleted: false, Alive: true } || buyer is not { Deleted: false, Alive: true } || from == null
            || into == null || kind == null || units <= 0 || price <= 0)
        {
            Absent++;
            refused = "nobody to hand it to";

            return 0;
        }

        if (giver.Map != buyer.Map || !giver.InRange(buyer.Location, MeetTiles))
        {
            Apart++;
            refused = $"{buyer.Name} was not standing beside it";

            return 0;
        }

        Gather(seller, from, kind);

        var available = 0;

        for (var i = 0; i < _lots.Count; i++)
        {
            available += _lots[i].Take;
        }

        if (available <= 0)
        {
            Empty++;
            refused = $"it had no {kind.Name} left to hand over";

            return 0;
        }

        // What the buyer may pay with: what its want for this kind holds, and whatever it has free (02.10.2026, build 391). The
        // reservation was made for this hand-over; money its other wants hold is not this want's to spend. See BotMarket.Reserved.
        // Never more than the purse holds: a buyer short of its own reservation pays for what it can, not refused whole.
        // <b>And what the seller already holds of the buyer's money (03.10.2026, econ13):</b> a brewer whose glass and herbs the want
        // advanced takes that much off the bill, and the buyer pays the rest. See BotAdvance.
        var credit = BotAdvance.Credit(seller, buyer, kind);
        var purse = (int)Math.Min(BotYield.Wealth(buyer), (long)BotYield.Spendable(buyer) + BotMarket.ReservedFor(buyer, kind)) + credit;

        // <b>And only the reservation of the units handed over (03.10.2026, build next).</b> A price over the offer for some units
        // was paid out of the money held for the rest, and the want stood down on the next beat: every rib line that failed
        // "buyer could not pay" on live 395 was one. The units' own reservation pays the offer; the part over it comes out of free
        // money and the carriage this carrier agreed and had held. See BotMarket.Affords.
        var want = BotMarket.Of(buyer, kind);
        var payable = want != null ? BotMarket.Affords(want, price, seller) : purse / price;

        if (purse < price || payable <= 0)
        {
            Broke++;
            refused = purse < price || want == null || price <= want.Offer
                ? $"{buyer.Name} could not pay {price}gp for one {kind.Name} with {purse}gp"
                : $"{buyer.Name}'s free money would not carry {price - want.Offer}gp over its offer of {want.Offer}gp for one {kind.Name}";

            return 0;
        }

        // Never more than are still wanted: what another carrier brought meanwhile is not bought twice.
        var asked = Math.Min(units, available);

        if (want != null)
        {
            asked = Math.Min(asked, want.Amount);
        }

        units = Math.Min(Math.Min(asked, purse / price), payable);

        if (units < asked)
        {
            CutToFree++;
            CutUnits += asked - units;
        }

        var bill = units * price;
        var lent = Math.Min(credit, bill);

        if (!BotMarket.Charge(buyer, bill - lent, out var fromPack, kind))
        {
            Broke++;
            refused = $"{buyer.Name} could not put {bill - lent}gp down for {units} {kind.Name}";

            return 0;
        }

        var given = 0;

        for (var i = 0; i < _lots.Count && given < units; i++)
        {
            var (item, take) = _lots[i];

            if (item == null || item.Deleted)
            {
                continue;
            }

            var part = Math.Min(take, units - given);

            // The engine's own split: the stack keeps the part handed over and a new stack with the rest goes back beside it.
            if (part < Math.Max(1, item.Amount) && (!item.Stackable || Mobile.LiftItemDupe(item, part) == null))
            {
                continue;
            }

            var moved = Math.Max(1, item.Amount);

            if (!into.TryDropItem(buyer, item, false))
            {
                PackFull++;

                // The buyer is marked "no room" so no carrier walks to it again before it has made some (06.10.2026, deliver16).
                // See BotRoom.
                BotRoom.Refused(buyer, item.TotalWeight + item.PileWeight);

                break;
            }

            given += moved;
        }

        if (given < units)
        {
            // Out of the pack first, where the coin came from, so a bot that paid in coin gets coin back; the rest to the account.
            // An advance pays for the units handed first (econ13): what was charged over what those still owe goes back.
            var settled = Math.Min(lent, Math.Max(0, given) * price);
            var back = bill - lent - (Math.Max(0, given) * price - settled);

            lent = settled;

            var toPack = Math.Min(back, fromPack);

            if (toPack > 0)
            {
                into.DropItem(new Gold(toPack));
                fromPack -= toPack;
            }

            BotMarket.Refund(buyer, back - toPack);
        }

        if (given <= 0)
        {
            refused = $"{buyer.Name}'s pack would not take {kind.Name}";

            return 0;
        }

        var takings = given * price;

        // What the advance already paid the seller is not paid twice; the loan comes down by it (econ13). See BotAdvance.Settle.
        BotMarket.Pay(giver, takings - lent);

        if (lent > 0)
        {
            BotAdvance.Settle(seller, buyer, kind, lent, given);
            OnAdvance += lent;
        }

        BotAdvance.Handed(kind, given, lent > 0);

        paid = takings;

        Exchanges++;
        Units += given;
        Gold += takings;
        FromPack += Math.Min(fromPack, takings - lent);
        ByCheque += Math.Max(0, takings - lent - fromPack);

        var (u, g) = _kinds.GetValueOrDefault(kind);
        _kinds[kind] = (u + given, g + takings);

        if (aside)
        {
            // The coin that left the buyer now; the advance was booked aside when it was lent (econ13).
            BotYield.Aside(buyer, takings - lent);
        }

        BotMarket.Traded(kind, given, takings);

        // Trade mends a quarrel between guilds, both ways round: a sale is the one event both parties chose. See BotRegard.Traded.
        BotRegard.Traded((buyer.Guild as Guilds.Guild)?.Name, (giver.Guild as Guilds.Guild)?.Name);

        // And between the two bots themselves, and the seller's best sale for its story (07.10.2026, traits17). See BotBiography.Sold.
        BotBiography.Sold(giver, buyer, given, kind, takings);

        // A town's supply task counts goods that went out to a buyer. See BotBurgh.Supplied.
        BotBurgh.Supplied(giver, kind, given);

        // <b>What a bot asked for and was handed goes on now, not on the dresser's next clock (01.10.2026, build 386).</b> Brokk
        // handed Norna the skirt she had asked for at 12:20:13; the dresser would have worn it at 12:20:26, but the trader's look
        // at 12:20:20 found it in the pack first and sold it on. Worn at the meeting it is on the body, where neither a haul's
        // stock nor the porter reads it. Armour, clothing and weapons only; a re-arm is a walk of the pack, once per such meeting.
        if (buyer is BotMobile wearer && (typeof(BaseArmor).IsAssignableFrom(kind) || typeof(BaseClothing).IsAssignableFrom(kind)
            || typeof(BaseWeapon).IsAssignableFrom(kind)) && wearer.Rearm() > 0)
        {
            WornAtOnce++;
        }

        logger.Information(
            "{Seller} handed {Units} {Item} to {Buyer} for {Paid}gp, {Price} each — {Why}",
            giver.Name,
            given,
            kind.Name,
            buyer.Name,
            takings,
            price,
            why ?? "hand to hand"
        );

        BotEvents.Post("market", giver, $"handed {given} {kind.Name} to {buyer.Name} for {takings}gp");

        var fill = new Dictionary<string, string>
        {
            ["buyer"] = buyer.Name,
            ["seller"] = giver.Name,
            ["amount"] = given.ToString(),
            ["item"] = BotShopkeep.Words(kind),
            ["price"] = takings.ToString()
        };

        // Said to the one bot standing beside the speaker, so local unless bot-voice.json sends hand-overs to the trade room.
        BotVoice.Say(giver, BotVoice.HandOver, BotVoice.Phrase("market:hand", null, giver, fill) ?? $"Here you are, {buyer.Name}: {given} {fill["item"]}.", true);
        BotVoice.Say(buyer, BotVoice.HandOver, BotVoice.Phrase("market:paid", null, buyer, fill) ?? $"{takings} gold, as agreed. Thank you, {giver.Name}.", true);

        return given;
    }

    /// <summary>The seller's stacks of the kind it may hand over: merchandise in its pack, never what is bound to it.</summary>
    private static void Gather(IBotWilful seller, Container from, Type kind)
    {
        _lots.Clear();

        var bond = seller.Bond;
        var carried = from.Items;

        for (var i = 0; i < carried.Count; i++)
        {
            var item = carried[i];

            if (item?.GetType() == kind && BotShopkeep.Merchandise(item, bond))
            {
                _lots.Add((item, Math.Max(1, item.Amount)));
            }
        }
    }

    /// <summary>How many of this kind the bot carries that it could hand over: merchandise in its pack.</summary>
    public static int Carried(IBotWilful bot, Type kind)
    {
        var pack = bot?.Self?.Backpack;

        if (pack == null || kind == null)
        {
            return 0;
        }

        var bond = bot.Bond;
        var total = 0;
        var carried = pack.Items;

        for (var i = 0; i < carried.Count; i++)
        {
            var item = carried[i];

            if (item?.GetType() == kind && BotShopkeep.Merchandise(item, bond))
            {
                total += Math.Max(1, item.Amount);
            }
        }

        return total;
    }

    private static string Commonest(int most)
    {
        if (_kinds.Count == 0)
        {
            return "nothing yet";
        }

        List<(Type Kind, long Units, long Gold)> ranked = [];

        foreach (var (kind, (units, gold)) in _kinds)
        {
            ranked.Add((kind, units, gold));
        }

        ranked.Sort(static (a, b) => b.Gold.CompareTo(a.Gold));

        List<string> said = [];

        for (var i = 0; i < ranked.Count && i < most; i++)
        {
            said.Add($"{ranked[i].Kind.Name} {ranked[i].Units} for {ranked[i].Gold}gp");
        }

        return string.Join(", ", said);
    }

    public static string Describe() =>
        $"{Exchanges} meetings moved {Units} things for {Gold}gp ({FromPack}gp paid over in coin, {ByCheque}gp drawn on the buyer's account); "
        + $"refused: {Apart} not standing together, {Broke} the buyer could not pay, {Empty} the seller had none left, {PackFull} the buyer's pack took less, {Absent} one of the two gone; "
        + $"{CutToFree} handed fewer than brought for what the buyer could pay — a price over the offer its free money would not carry, or a purse short of its own reservation ({CutUnits} units kept back, the rest of the want still held); "
        + $"{OnAdvance}gp of it settled out of a brewer's advance for its glass and herbs rather than paid at the meeting; "
        + $"{KeptForBuyer} times a piece a carrier would have worn was left in its pack for the buyer (counted per look at the pack, not per piece) and {WornAtOnce} buyers put on what they were handed; "
        + $"most by value: {Commonest(6)}";

    public static void Forget()
    {
        Exchanges = 0;
        Units = 0;
        Gold = 0;
        FromPack = 0;
        ByCheque = 0;
        Apart = 0;
        Broke = 0;
        CutToFree = 0;
        CutUnits = 0;
        OnAdvance = 0;
        Empty = 0;
        PackFull = 0;
        Absent = 0;
        KeptForBuyer = 0;
        WornAtOnce = 0;
        _kinds.Clear();
    }
}
