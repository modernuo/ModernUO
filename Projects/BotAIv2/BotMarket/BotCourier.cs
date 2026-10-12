using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// Offers a bot carrying what somebody has said it wants the walk to that somebody, and the sale at the other end.
///
/// <para>
/// <b>The supplier's half of hand to hand (01.10.2026).</b> Under the auction a want was filled from wherever the supplier
/// stood: the porter at a counter, the miner at its forge, the tailor at its bench — 421 fills for some 1,200 things in the
/// hour of 16:42–17:42 on 30.09.2026, leather 369, iron ingots 365, raw ribs 210, feathers 157, logs 40 and 93 pieces of
/// leather and plate armour. Every one of those is now a walk. Anything in the pack over what the bot keeps for itself
/// (<see cref="BotUnload.Keeps"/>) — and, for a bot standing at a bank, anything in its bank box — is weighed against every open
/// want for its kind, and the best by what it pays over the minutes of the walk is offered as a <see cref="BotDeliver"/>.
/// </para>
///
/// <para>
/// <b>Priced as a sale, and corrected by what sales paid.</b> The claim is the gold the delivery would bring over the walk and
/// the handing over. The ledger learns what deliveries really paid, as it learns everything else, so a carrier whose buyers
/// keep walking away stops being sent after them. A delivery worth less than <see cref="LeastGold"/>, or on another land, or
/// past <see cref="Reach"/>, or to a buyer who could not pay for one at its own offer, is not offered — each counted apart.
/// </para>
///
/// <para>
/// <b>Three walls made prices or questions (06.10.2026, needs15; Patrick: "95 on work and on Needs orders").</b> Build 400 failed 591
/// Needs lines in 11.3 hours, and three causes in this file held 472 of them. The tail of a part-handed want is offered under
/// <see cref="LeastGold"/> (<see cref="TailExempt"/>). A buyer past <see cref="Reach"/> nobody within it has answered is walked to
/// out to <see cref="FarReach"/>, the buyer paying carriage (<see cref="FarOpen"/>, <see cref="FarPrice"/>). And the straight road
/// the auction judges the walk by is asked here, of the want that would win, so the carrier offers the best walk its auction will
/// take rather than one it will refuse (<see cref="BotPass.Road"/>).
/// </para>
/// </summary>
public sealed class BotCourier : IBotProposer
{
    public string Name => "Courier";

    public BotStanding Rung => BotStanding.Free;

    /// <summary>
    /// How far a carrier walks to a buyer as a matter of course. The walk is priced as well, so this is a backstop. A town and its
    /// fields. Past it, a want nobody near answers is a far walk (<see cref="FarReach"/>, 06.10.2026).
    /// </summary>
    public static int Reach { get; set; } = 400;

    /// <summary>The least a delivery must bring to be worth the walk: five ribs at a cook's three gold, not one.</summary>
    public static int LeastGold { get; set; } = 8;

    /// <summary>The least and most a delivery is claimed at, per minute. The ledger corrects the claim by what deliveries paid.</summary>
    public static double LeastClaim { get; set; } = 4.0;

    public static double MostClaim { get; set; } = 300.0;

    // ---- Every gate, counted apart -----------------------------------------------------------------------------------------

    public static long Looks { get; private set; }

    public static long Carrying { get; private set; }

    public static long Offered { get; private set; }

    public static long Own { get; private set; }

    public static long Covered { get; private set; }

    public static long Far { get; private set; }

    public static long Unjoined { get; private set; }

    public static long Sealed { get; private set; }

    public static long Broke { get; private set; }

    public static long Petty { get; private set; }

    /// <summary>
    /// Wants passed over by a carrier standing in the dungeon block for a buyer outside its own land, and for a buyer down there
    /// that a walk or a haul from outside lately found no way to (03.10.2026, build next). See <see cref="BotUnreached"/>.
    /// </summary>
    public static long Stranded { get; private set; }

    public static long Unreached { get; private set; }

    private static readonly Dictionary<Type, int> _surplus = [];

    /// <summary>Of the surplus, how much lies in the bank box of a bot standing at a bank. See <see cref="Propose"/>.</summary>
    private static readonly Dictionary<Type, int> _boxed = [];

    /// <summary>Deliveries offered out of the carrier's own bank box, standing at a bank.</summary>
    public static long FromBox { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        if (!BotMarket.Running || !BotMarket.AnyBespoke)
        {
            return null;
        }

        var body = bot?.Self;
        var map = body?.Map;
        var pack = body?.Backpack;

        if (map == null || map == Map.Internal || pack == null || !body.Alive)
        {
            return null;
        }

        // A bot that cannot take a step cannot walk to anybody.
        if (BotLadder.Load(body) > BotLadder.Ceiling(body))
        {
            return null;
        }

        Looks++;

        // The cheap question first: does the pack hold anything anybody has asked for at all? A hash lookup an item. And the bank
        // box, for a bot standing at a bank: the porter put its surplus there, and a fighter keeps no shop to sell it from, so a
        // want is its road out (01.10.2026).
        var any = false;
        var items = pack.Items;

        for (var i = 0; i < items.Count && !any; i++)
        {
            any = items[i] is { Deleted: false } item && BotMarket.Bespoke(item.GetType());
        }

        _boxed.Clear();

        var box = BotMarket.AtBank(body) ? body.FindBankNoCreate() : null;

        if (box != null)
        {
            var held = box.Items;

            for (var i = 0; i < held.Count; i++)
            {
                if (held[i] is { Deleted: false } item && BotMarket.Bespoke(item.GetType()) && BotShopkeep.Merchandise(item, bot.Bond))
                {
                    _boxed[item.GetType()] = _boxed.GetValueOrDefault(item.GetType()) + Math.Max(1, item.Amount);
                    any = true;
                }
            }
        }

        if (!any)
        {
            return null;
        }

        // What of it is over what the bot keeps for itself: only that is for sale.
        _surplus.Clear();

        var keep = BotUnload.Keeps(bot);
        var bond = bot.Bond;

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (item == null || item.Deleted || !BotMarket.Bespoke(item.GetType()) || !BotShopkeep.Merchandise(item, bond))
            {
                continue;
            }

            var kind = item.GetType();

            _surplus[kind] = _surplus.GetValueOrDefault(kind) + Math.Max(1, item.Amount);
        }

        List<Type> kinds = [.. _surplus.Keys];

        for (var i = 0; i < kinds.Count; i++)
        {
            var kind = kinds[i];

            if (keep.TryGetValue(kind, out var allowed))
            {
                var over = allowed >= int.MaxValue ? 0 : Math.Max(0, _surplus[kind] - allowed);

                if (over <= 0)
                {
                    _surplus.Remove(kind);
                }
                else
                {
                    _surplus[kind] = over;
                }
            }
        }

        // What is in the box is surplus by construction: nothing but the porter's surplus is ever put there.
        foreach (var (kind, units) in _boxed)
        {
            _surplus[kind] = _surplus.GetValueOrDefault(kind) + units;
        }

        if (_surplus.Count == 0)
        {
            return null;
        }

        Carrying++;

        var ledger = bot.Resolve?.Ledger;
        var wants = BotMarket.Wants;
        var now = Core.TickCount;
        var strength = BotQuad.Strength(body);

        BotWant best = null;
        var bestUnits = 0;
        var bestBoxed = 0;
        var bestRate = 0.0;
        var bestTo = Point3D.Zero;
        var bestPrice = 0;
        var bestFar = false;
        var bestTail = false;

        // The best rate whose road was refused: an offer that goes to a lower one is the road's doing. See RoadSecond.
        var refusedRate = 0.0;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (want?.Kind == null || !_surplus.TryGetValue(want.Kind, out var have) || !want.IsOpen)
            {
                continue;
            }

            if (ReferenceEquals(want.Buyer, bot) || BotMarket.Wanted(bot, want.Kind) != null)
            {
                Own++;

                continue;
            }

            // Every pass-over below is also written on the want, so a line that fails names who held the goods and why they did
            // not come (02.10.2026, build 391). See BotWant.Passed and BotLines.
            if (want.Buyer?.Self is not { Deleted: false } buyer)
            {
                continue;
            }

            if (!buyer.Alive)
            {
                want.Passed(BotPass.Dead);

                continue;
            }

            if (buyer.Map != map)
            {
                want.Passed(BotPass.Land);

                continue;
            }

            // <b>A pack that refused a hand-over lately and has made no room since (06.10.2026, deliver16).</b> On build 407 267 of
            // 444 failed deliveries were "<buyer>'s pack would not take <kind>", from 14 buyers, the next carrier 22 seconds after
            // the last: the refusal was counted and never read. See BotRoom.
            if (BotRoom.Shut(buyer))
            {
                BotRoom.Held();
                want.Passed(BotPass.Room);

                continue;
            }

            // <b>And a pack at its count before any refusal (06.10.2026, work17).</b> 17 of the 26 deliver pack refusals on build 410
            // were the first of their episode: the pack was full before the carrier set out, and only the refusal said so. Read
            // here, the buyer is marked as a refusal would mark it, and its peddler and porter are pressed. See BotRoom.Receives.
            if (!BotRoom.Receives(buyer, want.Kind))
            {
                BotRoom.Note(buyer);
                BotRoom.FirstPassed();
                want.Passed(BotPass.Room);

                continue;
            }

            // <b>A buyer standing where two walks lately found no way (06.10.2026, deliver16).</b> The carrier's ledger kept the
            // refusal for itself; the next carrier walked to the same cell. See BotWayless.
            if (BotWayless.Shut(map, buyer.Location, false))
            {
                BotWayless.PassedByCarrier();
                want.Passed(BotPass.Sealed);

                continue;
            }

            // <b>Distance is a price, not a wall (06.10.2026, needs15).</b> Past Reach a want is still walked when nobody within reach
            // has answered it for FarQuietMs, no counter sells its kind, and it is worth the walk: paid for by carriage out of the
            // buyer's own money, within its ceiling. See FarOpen and FarPrice.
            var tiles = Math.Max(Math.Abs(buyer.X - body.X), Math.Abs(buyer.Y - body.Y));
            var far = false;

            if (tiles > Reach)
            {
                if (!FarOpen(want, tiles, now))
                {
                    Far++;
                    want.Passed(BotPass.Reach);

                    continue;
                }

                far = true;
                FarOpened++;
            }

            if (!BotGates.Joined(map, body.Location, buyer.Location))
            {
                Unjoined++;
                want.Passed(BotPass.Land);

                continue;
            }

            // <b>Down the dungeon block (03.10.2026, build next).</b> A carrier standing in it walks only inside its own land of the
            // walkers' graph, and a buyer down there that a walk or a haul from outside lately found no way to is left for a bot
            // that reaches it, or for when it comes up. See BotUnreached.
            if (BotUnreached.Shut(map, body.Location, buyer.Location))
            {
                Stranded++;
                want.Passed(BotPass.Sealed);

                continue;
            }

            if (BotUnreached.Refuses(map, buyer, body.Location))
            {
                Unreached++;
                want.Passed(BotPass.Sealed);

                continue;
            }

            if (ledger?.Cautious(BotDeliver.Trade, map, buyer.Location) == true
                || BotReach.Ask(map, body.Location, buyer.Location, BotArrival.Within(BotHandover.MeetTiles), false) == BotReachVerdict.Sealed)
            {
                Sealed++;
                want.Passed(BotPass.Sealed);

                continue;
            }

            var units = Math.Min(have, want.Payable);

            // A far walk asks its carriage of the buyer, within the want's ceiling; a buyer that cannot pay the carriage is still
            // walked to at its own offer.
            var price = far ? FarPrice(want, tiles, units) : want.Offer;

            // The money behind this want: its own reservation, and whatever its buyer has free (build 391). See BotMarket.Affords.
            // And what this carrier already holds of it as a brewer's advance (econ13), which comes off the bill. See BotAdvance.
            var afford = BotMarket.Affords(want, price, bot);

            if (afford <= 0 && price > want.Offer)
            {
                price = want.Offer;
                afford = BotMarket.Affords(want, price, bot);
            }

            units = Math.Min(units, afford);

            if (units <= 0)
            {
                Broke++;
                want.Passed(BotPass.Poor);

                continue;
            }

            var gold = units * price;
            var tail = false;

            if (gold < LeastGold)
            {
                // <b>The tail of a want already part handed (06.10.2026, needs15).</b> Build 400, 04.10: 103 lines failed "partial",
                // 90 of them raw ribs and 88 of those one or two short, because one rib at three or four gold is under the least a
                // delivery must bring, so no carrier was ever offered the last of a five; not one of 6375 handovers was under 8gp.
                // The floor keeps a carrier from walking one rib of five; it was never meant to strand the last one. A delivery that
                // finishes what is still open of a want somebody has already handed against is offered whatever it comes to.
                if (TailExempt && want.Filled > 0 && units >= want.Payable)
                {
                    tail = true;
                }
                else
                {
                    Petty++;
                    want.Passed(BotPass.Petty);

                    continue;
                }
            }

            // A partner guild's want under standing orders is worth a quarter more to its carrier. See BotPact.Serves.
            var served = BotPact.Serves(body, buyer) ? 1.25 : 1.0;
            var minutes = BotAppraisal.Travel(body.Location, buyer.Location) + BotDeliver.WorkMinutes;
            var rate = gold * served / Math.Max(0.25, minutes);

            if (rate > bestRate)
            {
                // <b>The road, asked here and not only by the auction (06.10.2026, needs15).</b> The courier offered one walk a
                // look, the best by rate, and the auction then refused it for the straight road to the buyer (BotAppraisal:
                // BotDanger.Road) — on build 400 2468 of the 2487 deliveries printed as refused were refused for that road — so the
                // carrier offered nothing at all while it held goods somebody else nearer or round the danger could be handed. The
                // same question, with the same strength the deed brings (BotDeed.Brings), asked only of a want that would win.
                if (BotDanger.Road(body, map, body.Location, buyer.Location, strength, out _, out _, out _))
                {
                    RoadRefused++;
                    refusedRate = Math.Max(refusedRate, rate);
                    want.Passed(BotPass.Road);

                    continue;
                }

                best = want;
                bestUnits = units;
                bestRate = rate;
                bestTo = buyer.Location;
                bestPrice = price;
                bestFar = far;
                bestTail = tail;

                // Whatever the pack does not hold comes out of the box before the walk.
                var inBox = _boxed.GetValueOrDefault(want.Kind);
                bestBoxed = Math.Min(inBox, Math.Max(0, units - (have - inBox)));
            }
        }

        if (best == null)
        {
            Covered++;

            return null;
        }

        Offered++;

        if (refusedRate > bestRate)
        {
            RoadSecond++;
        }

        if (bestFar)
        {
            FarOffered++;
            best.OfferedFar();
        }
        else
        {
            best.Offered();
        }

        if (bestTail)
        {
            TailOffered++;
        }

        if (bestBoxed > 0)
        {
            FromBox++;
        }

        return new BotDeliver(best, best.Kind, bestUnits, bestPrice, Math.Clamp(bestRate, LeastClaim, MostClaim), map, bestTo, bestBoxed, bestFar, bestTail);
    }

    // ---- Far walks and tails (06.10.2026, needs15) -------------------------------------------------------------------------

    /// <summary>
    /// How far a carrier walks to a buyer nobody within <see cref="Reach"/> has answered. Twelve hundred tiles: Yew to Britain and
    /// back to the Sages north of it; the walk's own clock grows with it (<see cref="BotDeliver"/>). Nought or below Reach: the wall
    /// stands as before.
    /// </summary>
    public static int FarReach { get; set; } = 1200;

    /// <summary>How long a want stands with no hand within reach offering for it before a far carrier may. The raise's ten minutes.</summary>
    public static int FarQuietMs { get; set; } = 600000;

    /// <summary>The least the open part of a want must be worth for a far walk: a scroll, a piece of armour, not three ribs.</summary>
    public static int FarLeastGold { get; set; } = 30;

    /// <summary>Carriage asked of the buyer for each tile past <see cref="Reach"/>, split over the units carried, never past the want's ceiling.</summary>
    public static double FarGoldPerTile { get; set; } = 0.1;

    /// <summary>Whether the last units of a want already part handed are offered under <see cref="LeastGold"/>. See <see cref="Propose"/>.</summary>
    public static bool TailExempt { get; set; } = true;

    /// <summary>
    /// Whether a buyer past <see cref="Reach"/> may be walked to: within <see cref="FarReach"/>; the kind sold at no counter (a
    /// shelf-sold kind is the traders' long haul and the buyer's own walk to a counter); the open part worth
    /// <see cref="FarLeastGold"/>; and nobody within reach answering it for <see cref="FarQuietMs"/> (<see cref="BotWant.Quiet"/>).
    /// Bounded after that by the carrier's own rules, as any walk: the gates, the dungeon block, the ledger's caution, the road
    /// (<see cref="BotDanger.Road"/>), and in its auction the ground and the hostile strength at the buyer's place.
    /// </summary>
    internal static bool FarOpen(BotWant want, int tiles, long now) =>
        FarReach > Reach && tiles <= FarReach && want.Shelf <= 0 && !want.ShelfPending && want.Worth >= FarLeastGold
        && want.Quiet(now, FarQuietMs);

    /// <summary>The price a far walk asks for one: the offer, and the carriage past <see cref="Reach"/> over the units, within the want's ceiling.</summary>
    private static int FarPrice(BotWant want, int tiles, int units)
    {
        if (FarGoldPerTile <= 0.0 || tiles <= Reach)
        {
            return want.Offer;
        }

        var carriage = (int)Math.Ceiling(FarGoldPerTile * (tiles - Reach) / Math.Max(1, units));
        var top = want.Highest(BotMarket.MostMultiple);

        return Math.Max(want.Offer, Math.Min(top, want.Offer + carriage));
    }

    /// <summary>Looks past <see cref="Reach"/> that the far rule opened, far walks offered, wants passed over for the road, offers that went to a want below one whose road was refused, and tails offered under the least.</summary>
    public static long FarOpened { get; private set; }

    public static long FarOffered { get; private set; }

    public static long RoadRefused { get; private set; }

    public static long RoadSecond { get; private set; }

    public static long TailOffered { get; private set; }

    /// <summary>Makers' looks that found a far want answerable by the far rule. See <see cref="Answerable"/>.</summary>
    public static long FarAnswerable { get; private set; }

    /// <summary>
    /// Whether somebody within a carrier's reach of this bot, on its map and land, has said it wants this kind — the porter's
    /// question before it puts a thing in the bank box: goods somebody near is asking for stay in the pack for the walk. Never the
    /// bot's own want, nor a kind it wants itself.
    /// </summary>
    public static bool Deliverable(IBotWilful bot, Type kind)
    {
        if (!BotMarket.Running || kind == null || !BotMarket.Bespoke(kind))
        {
            return false;
        }

        var body = bot?.Self;
        var map = body?.Map;

        if (map == null || map == Map.Internal || BotMarket.Wanted(bot, kind) != null)
        {
            return false;
        }

        var wants = BotMarket.Wants;

        for (var i = 0; i < wants.Count; i++)
        {
            var want = wants[i];

            if (want.Kind != kind || !want.IsOpen || ReferenceEquals(want.Buyer, bot))
            {
                continue;
            }

            if (want.Buyer?.Self is { Deleted: false, Alive: true } buyer && buyer.Map == map
                && Utility.InRange(body.Location, buyer.Location, Reach) && BotGates.Joined(map, body.Location, buyer.Location))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Whether a want is one this supplier's carrier could ever answer: its buyer alive, on the supplier's map, within
    /// <see cref="Reach"/> and on the same land (<see cref="BotGates.Joined"/>) — the very gates <see cref="Propose"/> reads before
    /// it offers the walk.
    ///
    /// <para>
    /// <b>Asked by every maker before it takes an order (01.10.2026, build 384 review).</b> The auction filled a want from wherever
    /// the maker stood, so a smith at Britain's forge could take a Yew archer's order and the archer got the piece. Now the piece
    /// reaches the buyer only by a carrier's walk, and the carrier refuses a buyer past 400 tiles or on another land — so a maker
    /// reading the same dearest want the carrier will never take would beat out pieces nobody can be handed, stint after stint, for
    /// the hundred minutes the want takes to give up and again when it is said anew. The bots settle in every mainland town
    /// (BotSettle: Yew, Minoc, Vesper, Trinsic, Cove and Britain are 600–1400 tiles apart), so it is the ordinary case, not the odd one.
    /// </para>
    /// </summary>
    public static bool Answerable(IBotWilful supplier, BotWant want)
    {
        var body = supplier?.Self;
        var map = body?.Map;

        if (want is not { IsOpen: true } || map == null || map == Map.Internal || ReferenceEquals(want.Buyer, supplier))
        {
            return false;
        }

        if (want.Buyer?.Self is not { Deleted: false, Alive: true } buyer || buyer.Map != map)
        {
            return false;
        }

        // Past Reach, the far rule the carrier reads (06.10.2026, needs15): a scribe far from a buyer nobody near answers writes for it.
        var tiles = Math.Max(Math.Abs(buyer.X - body.X), Math.Abs(buyer.Y - body.Y));
        var near = tiles <= Reach;

        if (!near && !FarOpen(want, tiles, Core.TickCount) || !BotGates.Joined(map, body.Location, buyer.Location))
        {
            OutOfReach++;
            want.Passed(BotPass.Reach);

            return false;
        }

        if (!near)
        {
            FarAnswerable++;
            want.FarAsked();
        }

        return true;
    }

    /// <summary>
    /// Whether this maker already carries enough surplus of the want's kind to answer it — made on its last stint, waiting in the
    /// pack for the carrier's walk (<see cref="BotDeliver"/>).
    ///
    /// <para>
    /// <b>Asked by every maker before it takes an order (01.10.2026, build 384 review).</b> A want's units used to come down at
    /// the bench (the auction's fill); they come down at the meeting now, so between the stint that made the piece and the walk
    /// that hands it over the order reads exactly as it did before, and a maker whose forge outbid the delivery on its next review
    /// beat out a second piece for the same buyer, and a third. Surplus over what the bot keeps for itself (<c>BotUnload.Keeps</c>),
    /// the same reading the carrier makes, so a fletcher's own quiver is not mistaken for an order already answered.
    /// </para>
    /// </summary>
    public static bool Holding(IBotWilful maker, BotWant want)
    {
        if (want?.Kind == null || maker == null)
        {
            return false;
        }

        var have = BotHandover.Carried(maker, want.Kind);

        if (have <= 0)
        {
            return false;
        }

        if (BotUnload.Keeps(maker).TryGetValue(want.Kind, out var allowed))
        {
            have = allowed >= int.MaxValue ? 0 : Math.Max(0, have - allowed);
        }

        if (have < want.Payable)
        {
            return false;
        }

        AlreadyHeld++;

        return true;
    }

    /// <summary>Orders makers passed over: the buyer past a carrier's reach or on another land, and pieces already made and waiting for the walk.</summary>
    public static long OutOfReach { get; private set; }

    public static long AlreadyHeld { get; private set; }

    public static string Describe() =>
        Looks == 0
            ? "nobody has been looked at for a delivery"
            : $"{Looks} looks: {Carrying} carried something somebody had asked for, {Offered} deliveries offered ({FromBox} of them out of the carrier's own bank box at a bank; {FarOffered} to a buyer past {Reach} tiles that nobody within them had answered for {FarQuietMs / 60000} min, of {FarOpened} such looks; {TailOffered} the last units of a want already part handed, under {LeastGold}gp; {RoadSecond} went to a lesser want because the best one's road was refused); passed over: {Own} wanted by the carrier itself, {Far} buyers past {Reach} tiles (and not open to a far walk: past {FarReach}, sold at a counter, worth under {FarLeastGold}gp or answered within reach lately), {Unjoined} on another land, {Sealed} beyond a way through, {Stranded} from a carrier standing in the dungeon block to a buyer outside its land, {Unreached} down the dungeon block where a walk or a haul from outside lately found no way, {Broke} buyers who could not pay for one, {Petty} worth under {LeastGold}gp, {RoadRefused} whose straight road asked more strength than the carrier brings ×{BotDanger.RoadDare:F1}; {Covered} looks found nothing left to carry that was not on its way already; makers' looks passed over {OutOfReach} wants whose buyer no carrier from there could reach and {AlreadyHeld} whose piece was already made and waiting for the walk, and took {FarAnswerable} past reach by the far rule";

    public static void Forget()
    {
        Looks = 0;
        Carrying = 0;
        Offered = 0;
        FromBox = 0;
        Own = 0;
        Covered = 0;
        Far = 0;
        Unjoined = 0;
        Sealed = 0;
        Stranded = 0;
        Unreached = 0;
        Broke = 0;
        Petty = 0;
        OutOfReach = 0;
        AlreadyHeld = 0;
        FarOpened = 0;
        FarOffered = 0;
        RoadRefused = 0;
        RoadSecond = 0;
        TailOffered = 0;
        FarAnswerable = 0;
    }
}
