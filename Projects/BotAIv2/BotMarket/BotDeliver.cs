using System;
using System.Collections.Generic;

namespace Server.BotAI.V2;

/// <summary>
/// Carrying what somebody said it wants to where it is, and handing it over: the maker's walk to the one who ordered, the
/// hunter's walk to the cook, the miner's walk to the smith.
///
/// <para>
/// <b>What a fill became (01.10.2026, Patrick's order of 30.09: "every order and every purchase must now be hand to hand;
/// our pathfinding works very well").</b> A fill used to be one line of arithmetic wherever the supplier stood, and the goods
/// then waited in the market for the buyer. Now the supplier walks: the buyer is followed rather than aimed at — a buyer is at
/// work and moves — and at <see cref="BotHandover.MeetTiles"/> the goods go pack to pack and the coin comes back
/// (<see cref="BotHandover.Exchange"/>). The units carried are spoken for on the want from the moment the delivery is taken
/// (<see cref="BotWant.Reserve"/>), so a second carrier does not set out for the same five ribs.
/// </para>
///
/// <para>
/// <b>Seen through, and given a clock.</b> Steadfast like any trip with goods in the pack, put down for a flight and taken up
/// again; but a buyer who keeps walking away is not caught by walking after it for ever, so after <see cref="GiveUpMs"/> of
/// walking the delivery fails and says so, and the buyer's want is left for somebody nearer.
/// </para>
/// </summary>
public sealed class BotDeliver : BotDeed, IBotWantCarrier
{
    /// <summary>The ledger's word, and the word its failures are filed under at the buyer's place.</summary>
    public const string Trade = "deliver";

    /// <summary>How long the handing over itself is reckoned at, once there.</summary>
    public static double WorkMinutes { get; set; } = 0.5;

    /// <summary>How long a carrier walks after a buyer before it gives the delivery up. Eight minutes of walking, pauses not counted.</summary>
    public static int GiveUpMs { get; set; } = 480000;

    /// <summary>
    /// <b>How long a delivery may be put down in all before its units are opened to other carriers (03.10.2026, build next).</b>
    /// Three minutes, summed over every time it was put down; nought keeps only the old clock (one pause of <see cref="GiveUpMs"/>).
    /// See <see cref="Stale"/>.
    /// </summary>
    public static int ReleaseDownMs { get; set; } = 180000;

    /// <summary>
    /// <b>How long a carrier walks after its buyer without once coming nearer to it than it has been before it lets the buyer
    /// go: "could not catch up" (12.10.2026, w7).</b> A minute, measured only while the carrier's own walk is the one in hand
    /// (a fight on top of it, or a hold, is not the buyer drawing away), and by the planner's road where it holds one.
    ///
    /// <para>
    /// The walker's own count used to end these walks inside twelve seconds — over 80 % of the 1 436 "could not get nearer"
    /// of 11–12.10 — whether the buyer was drawing away or the carrier was one step from the hand-over; that count no longer
    /// judges a walk after a buyer (<see cref="BotDoing.WalkAfter"/>), and <see cref="GiveUpMs"/> alone would walk after a
    /// buyer on a horse for eight minutes (Falk Ashdown rode away from Mirthal at eight tiles a second, 189 to 263 tiles off
    /// in half a minute, live 12.10 22:45). A minute of walking that never gains a tile is a buyer this carrier will not
    /// catch; a buyer that stops for its work is caught, and every tile gained starts the minute again. Ended from the work,
    /// not the walker, so nothing is written against the place the buyer stood (<see cref="BotWayless"/>) or against the
    /// buyer (<c>BotMend.Beyond</c>). Nought turns it off.
    /// </para>
    /// </summary>
    public static int LosingMs { get; set; } = 60000;

    /// <summary>Walks after a buyer ended for coming no nearer to it in <see cref="LosingMs"/> of walking. See <see cref="LosingMs"/>.</summary>
    public static long OutRun { get; private set; }

    // ---- Counted where they happen -------------------------------------------------------------------------------------------

    public static long Takes { get; private set; }

    public static long Delivered { get; private set; }

    public static long DeliveredUnits { get; private set; }

    public static long DeliveredGold { get; private set; }

    /// <summary>Tiles walked to deliveries that were handed over, from where the carrier took them: the price of hand to hand.</summary>
    public static long Tiles { get; private set; }

    public static long BuyerGone { get; private set; }

    public static long BuyerDead { get; private set; }

    public static long OtherLand { get; private set; }

    public static long Answered { get; private set; }

    public static long Unloaded { get; private set; }

    public static long Late { get; private set; }

    public static long Refused { get; private set; }

    public static long Blocked { get; private set; }

    private readonly BotWant _want;

    private readonly Type _kind;

    private readonly int _price;

    private readonly double _claim;

    private readonly Map _map;

    private readonly Point3D _where;

    private readonly string _buyerName;

    private int _units;

    private long _resumedTick;

    private long _walkedMs;

    private Point3D _from;

    private int _handed;

    private int _paid;

    /// <summary>Units to take out of the bank box at the first beat, standing at the bank the carrier was offered this at.</summary>
    private int _fromBox;

    /// <summary>Units carriers took out of their own bank boxes for a delivery.</summary>
    public static long Boxed { get; private set; }

    /// <summary>
    /// Walks taken up again whose units the market's sweep had opened to other carriers while they were put down
    /// (<see cref="Stale"/>), counted once a walk; of them those that found another carrier had taken the units on, and those
    /// whose buyer's money no longer carried them, which ended. See <see cref="Resumed"/>.
    /// </summary>
    public static long Reopened { get; private set; }

    public static long Overtaken { get; private set; }

    public static long Unafforded { get; private set; }

    /// <summary>Whether this walk has been counted in <see cref="Reopened"/> (review of build next, F4: once a walk, not once a resumption).</summary>
    private bool _everReopened;

    /// <summary>Milliseconds this delivery has been put down in all, before the pause in hand.</summary>
    private long _downMs;

    /// <summary>
    /// The nearest this walk has come to its buyer while walking after it in a straight line, the least road left on the route in
    /// hand and which route that was, and the walking since either last improved. See <see cref="LosingMs"/>.
    /// </summary>
    private int _nearest = int.MaxValue;

    private int _nearestRoad = int.MaxValue;

    private int _roadStamp = -1;

    private long _losingMs;

    private long _losingTick;

    private bool _losing;

    /// <summary>How long the buyer has stood for this walk. See <see cref="BotRendezvous.Stand"/>.</summary>
    private readonly BotRendezvous.StandClock _stand = new();

    /// <summary>Taken up again after the sweep let its units go: what is still open is held again at the next beat, or it ends.</summary>
    private bool _reopened;

    /// <summary>A walk to a buyer past <see cref="BotCourier.Reach"/> (<see cref="BotCourier.FarOpen"/>), and the last units of a want under the least.</summary>
    private readonly bool _far;

    private readonly bool _tail;

    /// <summary>
    /// This walk's own give-up clock: <see cref="GiveUpMs"/>, or twice the walk reckoned from where it was taken, whichever is
    /// longer (06.10.2026, needs15). A far walk of a thousand tiles is near seven minutes of walking alone, and the eight-minute
    /// clock would fail it on a road with one detour.
    /// </summary>
    private long _giveUpMs = GiveUpMs;

    /// <summary>Far walks and tails: taken, handed over, and ended without handing (counted once a walk, when it ends).</summary>
    public static long FarTakes { get; private set; }

    public static long FarDelivered { get; private set; }

    public static long FarFailed { get; private set; }

    public static long TailTakes { get; private set; }

    public static long TailDelivered { get; private set; }

    public BotDeliver(BotWant want, Type kind, int units, int price, double claim, Map map, Point3D where, int fromBox = 0, bool far = false, bool tail = false)
    {
        _far = far;
        _tail = tail;
        _fromBox = Math.Max(0, fromBox);
        _want = want;
        _kind = kind;
        _units = Math.Max(1, units);
        _price = Math.Max(1, price);
        _claim = Math.Max(0.01, claim);
        _map = map;
        _where = where;
        _buyerName = want?.Buyer?.Self?.Name ?? "somebody";

        // Seeded from a real tick, never left at nought (a tick field's default is not a time); Taken seeds it again.
        _resumedTick = Core.TickCount;
    }

    /// <summary>The want this is answering. Read by the market's sweep: a reservation whose carrier is on another errand is dropped.</summary>
    public BotWant Want => _want;

    /// <summary>The sweep's question (<see cref="IBotWantCarrier"/>, 01.10.2026 build 385).</summary>
    public bool Carries(BotWant want) => ReferenceEquals(_want, want);

    /// <summary>The kind on its way to the buyer: not for the carrier's own back (<see cref="BotHandover.Promised"/>, build 386).</summary>
    public Type Goods => _kind;

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _where;

    public override double Expects => _claim;

    public override double Minutes => WorkMinutes;

    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>Paid in coin, at the buyer's side.</summary>
    public override double Coin => 1.0;

    /// <summary>Nothing made: the goods were counted as made by the trade that made them. The coin is this errand's takings.</summary>
    public override int Made => 0;

    /// <summary>A trip with somebody's goods in the pack is seen through. The clock inside is put right on resuming.</summary>
    public override bool Steadfast => true;

    public override string Stage =>
        _handed > 0
            ? $"handed {_handed} {_want?.Label} to {_buyerName} for {_paid}gp"
            : $"bringing {_units} {_want?.Label} to {_buyerName} at {_price}gp each";

    public override void Taken(IBotWilful bot)
    {
        _resumedTick = Core.TickCount;
        _from = bot?.Self?.Location ?? Point3D.Zero;
        _want?.Reserve(bot, _units, _price);
        Takes++;

        if (_from != Point3D.Zero)
        {
            _giveUpMs = Math.Max(GiveUpMs, (long)(2.0 * BotAppraisal.Travel(_from, _where) * 60000.0));
        }

        if (_far)
        {
            FarTakes++;
        }

        if (_tail)
        {
            TailTakes++;
        }

        var body = bot?.Self;
        var buyer = _want?.Buyer?.Self;

        if (body == null || buyer == null)
        {
            return;
        }

        var fill = new Dictionary<string, string>
        {
            ["buyer"] = buyer.Name,
            ["amount"] = _units.ToString(),
            ["item"] = BotShopkeep.Words(_kind),
            ["price"] = _price.ToString()
        };

        BotVoice.Say(
            body,
            "trade",
            BotVoice.Phrase("market:bring", null, body, fill) ?? $"{buyer.Name}, I have your {_units} {fill["item"]} at {_price} each. Coming to you."
        );
    }

    public override void Paused(IBotWilful bot)
    {
        _walkedMs += Math.Max(0L, Core.TickCount - _resumedTick);
        _paused = true;
        _pausedTick = Core.TickCount;
    }

    public override void Resumed(IBotWilful bot)
    {
        var now = Core.TickCount;

        if (_paused)
        {
            _downMs += Math.Max(0L, now - _pausedTick);
        }

        _resumedTick = now;
        _paused = false;

        // The buyer has walked on meanwhile: the nearest so far is about a buyer that is no longer there.
        LoseTrack();

        // <b>Held again only if it was still held (03.10.2026, build next).</b> A delivery put down past its clock (Stale) had
        // its units opened to other carriers by the market's sweep; taking it up again re-reserved all of them on top of whatever
        // another carrier had taken since, and counted the same carrier as a second take. What is still open is held again at
        // the next beat (Advance), and nothing open ends the walk.
        if (_want != null && !_want.Carried(bot))
        {
            // A want that has left the board meanwhile is the next beat's "met or withdrawn", not a reopening.
            if (BotMarket.Stands(_want))
            {
                _reopened = true;

                if (!_everReopened)
                {
                    _everReopened = true;
                    Reopened++;
                }
            }

            return;
        }

        _want?.Reserve(bot, _units, _price);
    }

    private bool _paused;

    private bool _farEnded;

    private long _pausedTick;

    /// <summary>
    /// Put down for longer than its own give-up clock: a carrier pressed into a company (build 381, <c>BotSquads.Pressable</c>
    /// takes a steadfast delivery) or on a flight for the length of a delve. The market's sweep drops its reservation then, so
    /// the want asks again and another carrier — or the buyer's own walk to a shop — may answer it meanwhile (01.10.2026,
    /// build 384 review); the delivery itself goes on when it resumes and reserves again, and finds the want met at worst.
    ///
    /// <para>
    /// <b>And put down for <see cref="ReleaseDownMs"/> in all (03.10.2026, build next).</b> The clock was one pause, and a
    /// company's call is a minute or two: on live 396 (04:19–05:40) Quillathe held five of Brom's ribs for fifteen minutes through
    /// six companies before another delivery outbid the walk, and 28 walks were put down for three minutes or more in all — 11
    /// handed over, 17 dropped, failed or died holding their units. An early, honest release beats a late failure: past the
    /// summed clock the units are open to another carrier while this one is away.
    /// </para>
    /// </summary>
    public bool Stale
    {
        get
        {
            if (!_paused)
            {
                return false;
            }

            var down = Core.TickCount - _pausedTick;

            return down >= GiveUpMs || ReleaseDownMs > 0 && _downMs + down >= ReleaseDownMs;
        }
    }

    /// <summary>
    /// Released by the summed clock and not by the old single pause: the units are open to other carriers, but this carrier is
    /// still on its way, put down, and the want still reads it as coming (<see cref="BotWant.Coming"/>) — so nothing that waits on
    /// a carrier (a shopper's far want, ask10's ask, the raise) gives up on it sooner than before the release (review of build
    /// next, F3). Past <see cref="GiveUpMs"/> at once it is not coming, as before.
    /// </summary>
    internal bool Away
    {
        get
        {
            if (!_paused || ReleaseDownMs <= 0)
            {
                return false;
            }

            var down = Core.TickCount - _pausedTick;

            return down < GiveUpMs && _downMs + down >= ReleaseDownMs;
        }
    }

    public override void Drop(IBotWilful bot)
    {
        // A far walk that ended with nothing handed, however it ended: failed, dropped, the carrier dead. Once a walk.
        if (_far && _handed <= 0 && !_farEnded)
        {
            _farEnded = true;
            FarFailed++;
        }

        _want?.Release(bot);

        // A meeting still standing when the delivery ends is a meeting missed. See BotRendezvous.Gone.
        BotRendezvous.Gone(bot, _want?.Buyer);
    }

    /// <summary>Deliveries that called a meeting, and beats spent walking to or waiting at a meeting place on guarded ground.</summary>
    public static long Meetings { get; private set; }

    public static long AtPlace { get; private set; }

    private bool _called;

    /// <summary>
    /// The way to the buyer turned out not to exist: filed under this errand's word at the buyer's place, which is the word the
    /// carrier's chooser asks in (<see cref="BotCourier"/>), and given up.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        var buyer = _want?.Buyer?.Self;

        // <b>A meeting place the walk found no way to is not the end of the delivery (06.10.2026, deliver16).</b> On build 407, 45 of
        // the 86 "no way through" deliveries were to meeting places in one row of Britain (x 1280-1440, y 1540-1555, z 20) that
        // BotHaven.Nearest calls guarded ground and no planner reaches: "planned 31 roads to one place in 18s without getting
        // anywhere", five carriers in twelve minutes for Orsik's one rib. The place is filed for every carrier (BotWayless), the
        // meeting forgets it, and the carrier walks to the buyer itself, as a meeting on safe ground does. Once a walk.
        if (_place != Point3D.Zero)
        {
            BotWayless.NoWay(_map, _place, true);
        }

        if (_place != Point3D.Zero && !_placeBent)
        {
            _placeBent = true;
            BotWayless.Rejoin();

            if (BotRendezvous.Standing(buyer) is { } meeting && ReferenceEquals(meeting.Carrier, bot))
            {
                meeting.Place = Point3D.Zero;
            }

            _place = Point3D.Zero;

            return true;
        }

        Blocked++;

        // Where the buyer stood, for every carrier: the second "no way" there passes it over for the next. See BotWayless.
        if (buyer != null && _place == Point3D.Zero)
        {
            BotWayless.NoWay(buyer.Map, buyer.Location, false);
        }

        bot?.Resolve?.Ledger?.Beware(Trade, buyer?.Map ?? _map, buyer?.Location ?? _where);

        // A buyer down the dungeon block is marked for every carrier and trader, not only this one's ledger. See BotUnreached.
        BotUnreached.NoWay(buyer?.Map ?? _map, buyer, _from, bot?.Self);

        return false;
    }

    /// <summary>
    /// The one bend this work survives is a meeting place it found no way to, and the walk goes on to the buyer with the goods in
    /// the pack: routine, not trouble, so it keeps its hold (06.10.2026, deliver16). See <see cref="BotDeed.BendIsTrouble"/>.
    /// </summary>
    public override bool BendIsTrouble => false;

    /// <summary>The meeting place the last beat walked to; nought while the buyer itself is followed. See <see cref="Bend"/>.</summary>
    private Point3D _place;

    /// <summary>Whether this walk has already gone to its buyer for a meeting place it found no way to. Once a walk.</summary>
    private bool _placeBent;

    // ---- The hold, by the planner's road (06.10.2026, deliver16) -------------------------------------------------------------

    /// <summary>
    /// How far past the road still to walk a delivery is held against a better offer: the Will's own stretch
    /// (<c>BotWill.CommitStretch</c>). Nought turns the planner's hold off and the straight-line reckoning holds alone.
    /// </summary>
    public static double HoldStretch { get; set; } = 1.5;

    /// <summary>The minutes this walk is held for, by the planner's road; nought until the first walking beat. See <see cref="Reckon"/>.</summary>
    private double _holdFor;

    private bool _roadCounted;

    /// <summary>Walks whose road left by the planner was a fifth or more longer than the straight line to the buyer, counted once a walk.</summary>
    public static long RoadLonger { get; private set; }

    /// <summary>
    /// <b>Held for the road the planner drew, not the straight line (06.10.2026, deliver16).</b> The Will holds work in hand
    /// against a better offer for 1.5 times what it reckoned when it was taken (<c>BotWill.HoldMs</c>), and a delivery reckoned its
    /// walk by the straight line to where the buyer stood (<c>BotAppraisal.Travel</c> in <c>BotWill.Commit</c>). On build 407, 43
    /// of the 44 deliveries outbid had walked past that reckoning by more than half again (median 1.64 times, "after 3.5 of 2.3
    /// minutes reckoned"), with the goods in the pack. Here the hold is what has been walked and the road still ahead by the
    /// planner's route (<c>BotJourney.RoadLeft</c>; the straight line when no route is drawn or it is shorter) times
    /// <see cref="HoldStretch"/>, never past this walk's own give-up clock, which still ends a walk after a buyer that keeps going.
    /// What a delivery is reckoned at for the take line and the claim is unchanged.
    /// </summary>
    public override double HoldsFor => _holdFor;

    private void Reckon(Mobile body, Mobile buyer, long walked)
    {
        if (HoldStretch <= 0.0)
        {
            _holdFor = 0.0;

            return;
        }

        var straight = Math.Max(Math.Abs(buyer.X - body.X), Math.Abs(buyer.Y - body.Y));
        var road = (body as BotMobile)?.Journey.RoadLeft(body.Location) ?? 0;

        if (!_roadCounted && straight > 0 && road * 5 >= straight * 6)
        {
            _roadCounted = true;
            RoadLonger++;
        }

        var left = Math.Max(road, straight) * (double)BotWalk.StepDelayMs(false) / 60000.0;

        _holdFor = Math.Min(walked / 60000.0 + left * HoldStretch + WorkMinutes, _giveUpMs / 60000.0);
    }

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        var buyer = _want?.Buyer?.Self;

        if (buyer == null || buyer.Deleted)
        {
            BuyerGone++;

            return BotDoing.Failed($"{_buyerName} is gone");
        }

        if (!buyer.Alive)
        {
            BuyerDead++;

            return BotDoing.Failed($"{_buyerName} is dead");
        }

        if (!BotMarket.Stands(_want) || _want.Amount <= 0)
        {
            Answered++;

            return BotDoing.Failed($"{_buyerName}'s want for {_want.Label} was met or withdrawn before it arrived");
        }

        // Taken up again after its units were opened to other carriers (see Resumed): what is still open is held again; nothing
        // open, another carrier has the walk, and this one ends rather than walking for goods nobody will pay it for.
        if (_reopened)
        {
            _reopened = false;

            var open = Math.Min(_units, _want.Payable);

            if (open <= 0)
            {
                Overtaken++;

                return BotDoing.Failed($"another carrier took {_buyerName}'s {_want.Label} on while this walk was put down");
            }

            // What the buyer's money carries now at this walk's price: it may have spent what the released units no longer held
            // (review of build next, F5; the same cap a haul's resumption reads). See BotMarket.Affords.
            open = Math.Min(open, BotMarket.Affords(_want, _price, bot));

            if (open <= 0)
            {
                Unafforded++;

                return BotDoing.Failed($"{_buyerName}'s money no longer carried its {_want.Label} at {_price}gp when this walk was taken up again");
            }

            _units = open;
            _want.Reserve(bot, open, _price, true);
        }

        // Out of the box first, at the bank the delivery was offered at: the box is reached only there.
        if (_fromBox > 0)
        {
            Boxed += BotMarket.FromBank(bot, _kind, _fromBox);
            _fromBox = 0;
        }

        var held = BotHandover.Carried(bot, _kind);

        if (held <= 0)
        {
            Unloaded++;

            return BotDoing.Failed($"no longer carrying any {_want.Label}");
        }

        _units = Math.Min(Math.Min(_units, held), _want.Amount);

        if (buyer.Map != body.Map || buyer.Map == null || buyer.Map == Map.Internal)
        {
            OtherLand++;

            return BotDoing.Failed($"{_buyerName} is on another land");
        }

        if (!body.InRange(buyer.Location, BotHandover.MeetTiles))
        {
            var walked = _walkedMs + Math.Max(0L, Core.TickCount - _resumedTick);

            if (walked >= _giveUpMs)
            {
                Late++;

                return BotDoing.Failed($"could not catch up with {_buyerName} in {walked / 60000.0:F1} minutes");
            }

            Reckon(body, buyer, walked);

            // <b>A meeting, once near (02.10.2026, Patrick: "the result of the trade must be that they meet").</b> Within
            // BotRendezvous.CallTiles the buyer is told, and comes; on unsafe ground both go to the guarded place the meeting names,
            // and the carrier waits there. See BotRendezvous.
            var apart = Math.Max(Math.Abs(buyer.X - body.X), Math.Abs(buyer.Y - body.Y));

            if (BotRendezvous.Running && apart <= BotRendezvous.CallTiles && BotRendezvous.Call(bot, _want.Buyer, _want, _units) is { } meeting)
            {
                if (!_called)
                {
                    _called = true;
                    Meetings++;
                }

                if (meeting.Place != Point3D.Zero)
                {
                    AtPlace++;
                    _place = meeting.Place;
                    LoseTrack();

                    return body.InRange(meeting.Place, 2)
                        ? BotDoing.Work($"waiting at the meeting place for {_buyerName} with {_units} {_want.Label}")
                        : BotDoing.Walk(buyer.Map, meeting.Place, BotArrival.Within(2), $"to the meeting place for {_buyerName} with {_units} {_want.Label}");
                }
            }

            _place = Point3D.Zero;

            // <b>The buyer stands for the last few tiles (12.10.2026, w7).</b> See BotRendezvous.Stand.
            BotRendezvous.Stand(body, buyer, apart, _stand);

            // <b>And a buyer that draws away is let go by this walk's own clock (12.10.2026, w7).</b> See LosingMs.
            if (Losing(body, buyer, apart))
            {
                OutRun++;

                return BotDoing.Failed(
                    $"could not catch up with {_buyerName}: no nearer than {_nearest} tiles in {LosingMs / 1000}s of walking after it, {apart} off now"
                );
            }

            // Followed rather than aimed at: the buyer is at work and moves, and a walk order matched by a point would be replaced
            // every beat it did. See BotPeddle for the whole reason. Kept up with (12.10.2026, w7): a plan that reaches the buyer is
            // a way however far it has walked on, and the clocks above end the walk (BotDoing.WalkAfter).
            return BotDoing.WalkAfter(buyer.Map, buyer, BotArrival.Within(BotHandover.MeetTiles), $"to {_buyerName} with {_units} {_want.Label}");
        }

        var given = BotHandover.Exchange(bot, buyer, _kind, _units, _price, true, "against its want", out var paid, out var refused);

        if (given <= 0)
        {
            Refused++;

            // A buyer that cannot pay at the meeting cannot honour the want either: it stands down, so no other carrier walks for it.
            // What it can pay is its want's reservation and whatever it has free (build 391): stood down when it cannot pay for one
            // at its own offer (build next). See BotMarket.Affords.
            if (buyer.Alive && BotMarket.Affords(_want, _want.Offer, bot) <= 0)
            {
                BotMarket.Withdrawn(_want.Buyer, _kind, BotWithdraw.Unpaid);
            }

            return BotDoing.Failed(refused ?? $"{_buyerName} would not take it");
        }

        _handed = given;
        _paid = paid;

        BotMarket.Received(_want, given, paid, body, BotRoute.Carrier);
        BotPact.Filled(body, buyer);

        // Handed down the dungeon block by a walk from outside it: the way exists. See BotUnreached.
        BotUnreached.Reached(body.Map, buyer, _from);

        // And here: whatever was filed against this cell is wrong. See BotWayless.
        BotWayless.Reached(body.Map, buyer.Location);

        // The meeting, if one was called, is met. See BotRendezvous.Handed.
        BotRendezvous.Handed(_want.Buyer);

        if (_stand.Began)
        {
            BotRendezvous.HandedAfterStand();
        }

        Delivered++;
        DeliveredUnits += given;
        DeliveredGold += paid;

        // A capped kind's surplus handed to another bot, by kind, with the other routes it may take (06.10.2026, box16). See BotSurplus.
        BotSurplus.CountIfCapped(bot, BotSurplus.ToBot, _kind, given);

        if (_far)
        {
            FarDelivered++;
        }

        if (_tail)
        {
            TailDelivered++;
        }

        if (_from != Point3D.Zero)
        {
            Tiles += Math.Max(Math.Abs(_from.X - body.X), Math.Abs(_from.Y - body.Y));
        }

        return BotDoing.Done($"handed {given} {_want.Label} to {_buyerName} for {paid}gp");
    }

    /// <summary>
    /// Whether this walk has come no nearer to its buyer in <see cref="LosingMs"/> of walking after it. Nearer is either of two
    /// things, and the minute starts again at both: a new least straight-line distance to the buyer, or — while the carrier
    /// holds a route to about where the buyer is — less road left on that route than at any beat since the route was drawn.
    /// Time counts only while the carrier's own walk is the one in hand and not held, and no more than a few seconds for a
    /// gap between two beats.
    ///
    /// <para>
    /// <b>Two measures, because one of them lies each way (reviews A and B of w7).</b> By the road alone, the first beat
    /// measured the straight line (no route drawn yet) and every later one the longer road, so a carrier on foot on a road
    /// much longer than the line read as losing for the whole of its first minute while it gained; and a road measured across
    /// routes restarts at every redraw, so a buyer riding away — every few seconds a new route — might never run the
    /// minute out (plausible, not traced; OutRun against Late after the deploy says). The straight line never restarts and so catches the rider; the road, compared only within one route
    /// (its <see cref="BotJourney.RouteStamp"/>, as BotWill's own watch and BotDig do), keeps the long way round honest.
    /// </para>
    /// </summary>
    private bool Losing(Mobile body, Mobile buyer, int apart)
    {
        if (LosingMs <= 0)
        {
            return false;
        }

        var now = Core.TickCount;
        var journey = (body as BotMobile)?.Journey;
        var gained = false;

        if (apart < _nearest)
        {
            _nearest = apart;
            gained = true;
        }

        if (journey != null && journey.HoldsRoadTo(buyer.Location))
        {
            var road = journey.RoadLeft(body.Location, buyer.Location);

            if (_roadStamp != journey.RouteStamp)
            {
                // A new route: its first reading is where its own count starts, not a gain.
                _roadStamp = journey.RouteStamp;
                _nearestRoad = road;
            }
            else if (road < _nearestRoad)
            {
                _nearestRoad = road;
                gained = true;
            }
        }

        if (gained || !_losing)
        {
            _losingMs = 0;
            _losing = true;
            _losingTick = now;

            return false;
        }

        if (journey is { Held: false } && journey.Current is { Interruption: false })
        {
            _losingMs += Math.Min(Math.Max(0L, now - _losingTick), 5000L);
        }

        _losingTick = now;

        return _losingMs >= LosingMs;
    }

    /// <summary>The nearest so far forgotten: the walk was put down, or went to a meeting place instead of after the buyer.</summary>
    private void LoseTrack()
    {
        _nearest = int.MaxValue;
        _nearestRoad = int.MaxValue;
        _roadStamp = -1;
        _losingMs = 0;
        _losing = false;
    }

    public static string Describe() =>
        Takes == 0
            ? "no delivery has been taken"
            : $"{Takes} taken, {Delivered} handed over ({DeliveredUnits} things for {DeliveredGold}gp, {(Delivered > 0 ? Tiles / Delivered : 0)} tiles on average from where the carrier set out; {Boxed} things taken out of a carrier's bank box for them); "
              + $"failed: {Answered} found the want met or withdrawn, {BuyerGone} the buyer gone, {BuyerDead} the buyer dead, {OtherLand} the buyer on another land, {Unloaded} the carrier no longer holding it, {Late} could not catch up in {GiveUpMs / 60000} minutes, {OutRun} came no nearer to a buyer walking on in {LosingMs / 1000}s, {Refused} refused at the meeting, {Blocked} no way to the buyer, {Overtaken} another carrier had taken the units on while it was put down, {Unafforded} the buyer's money no longer carried them when it was taken up again; "
              + $"{Meetings} deliveries called a meeting once within {BotRendezvous.CallTiles} tiles of the buyer, {AtPlace} beats spent going to or waiting at a guarded meeting place; "
              + $"{Reopened} walks taken up again after being put down {ReleaseDownMs / 60000} minutes in all or {GiveUpMs / 60000} at once, their units opened to other carriers meanwhile (the {Overtaken} and {Unafforded} above among them; the rest held again what was still open); "
              + $"far walks past {BotCourier.Reach} tiles: {FarTakes} taken, {FarDelivered} handed over, {FarFailed} ended with nothing handed; tails under {BotCourier.LeastGold}gp: {TailTakes} taken, {TailDelivered} handed over; {RoadLonger} walks held by the planner's road, a fifth or more longer than the straight line (x{HoldStretch:F1}, within the give-up clock); {BotSquads.CarriersSpared} company calls passed a carrier holding another bot's goods by, {BotCry.CarriersSpared} bots carrying for a buyer left out of a dire cry's calling past {BotCry.CarrierReach} tiles";

    public static void Forget()
    {
        Takes = 0;
        Boxed = 0;
        Delivered = 0;
        DeliveredUnits = 0;
        DeliveredGold = 0;
        Tiles = 0;
        BuyerGone = 0;
        BuyerDead = 0;
        OtherLand = 0;
        Answered = 0;
        Unloaded = 0;
        Late = 0;
        OutRun = 0;
        Refused = 0;
        Blocked = 0;
        Meetings = 0;
        AtPlace = 0;
        Reopened = 0;
        Overtaken = 0;
        Unafforded = 0;
        FarTakes = 0;
        FarDelivered = 0;
        FarFailed = 0;
        TailTakes = 0;
        TailDelivered = 0;
        RoadLonger = 0;
    }
}
