using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// A delivery is a meeting: once the carrier is near, the buyer comes to it — or both go to safe ground — instead of the
/// carrier walking after a buyer that is at work and moving.
///
/// <para>
/// <b>Patrick's order of 02.10.2026: "Today I saw a couple of times the customer of the goods running away from the
/// supplier. The result of the trade must be that they meet: either they walk towards each other, or they meet on safe
/// ground."</b> Measured on build 388's first nine hours: 654 deliveries ended, 555 handed over, median 26 seconds from
/// taking to handing — and 195 took over a minute, 108 over two, 39 over five; 28 were dropped and 13 put down on the
/// way, most of them walks after a buyer that took up a prowl, a hunt or a counter and kept going (Liriel after Dorin for
/// 304 s, dropped; Berengar after Norbert 184 s, dropped). The "could not catch up in 8 minutes" ending never fired once
/// in nine hours, because the auction took the carrier off the chase first. The carrier followed the buyer by its body
/// (<c>BotDoing.Walk(map, buyer, …)</c>) and the buyer knew nothing of it.
/// </para>
///
/// <para>
/// <b>Now the buyer is told.</b> A carrier within <see cref="CallTiles"/> of its buyer calls the meeting
/// (<see cref="Call"/>), and the buyer's own proposer (<see cref="BotMeeter"/>, on the free rung, pressing) offers it a
/// <see cref="BotMeet"/>: walk towards the carrier, stand when it is within <see cref="BotHandover.MeetTiles"/>, and let
/// the carrier's own beat hand the goods over. Where either stands on ground that is not safe — a hostile or deadly patrol
/// zone, or a square whose record asks more than the bot brings — the place is the nearest guarded ground to the point
/// between them within <see cref="SafeReach"/> (<see cref="BotHaven.Nearest"/>: a town's edge or a bank), and both walk
/// there. One meeting a buyer at a time; a second carrier for the same buyer follows as before until the first is done.
/// </para>
///
/// <para>
/// Counted: meetings arranged, met, missed — and why missed: the buyer waited and the carrier never came, the carrier
/// gave the delivery up with a meeting standing, the two stood together and nothing changed hands.
/// </para>
/// </summary>
public static class BotRendezvous
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRendezvous));

    /// <summary>A switch. Off, the carrier follows the buyer as on 388.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How near a carrier must be to its buyer before the meeting is called. Sixty tiles: a screen and a half.</summary>
    public static int CallTiles { get; set; } = 60;

    /// <summary>How long a called meeting stands without the carrier saying so again before it is gone.</summary>
    public static int StaleMs { get; set; } = 45000;

    /// <summary>How long the buyer gives the meeting before it goes back to its work. Three minutes.</summary>
    public static int MeetMs { get; set; } = 180000;

    /// <summary>How long the buyer stands with the carrier beside it and nothing handed over before it gives up.</summary>
    public static int WaitMs { get; set; } = 20000;

    /// <summary>How far from each of the two a guarded place may be to be the meeting place when the ground is unsafe.</summary>
    public static int SafeReach { get; set; } = 160;

    /// <summary>
    /// What the buyer's walk to the meeting is reckoned at per minute. The harrow's figure: a minute's walk that gets the
    /// buyer the thing it posted a want for, against a carrier walking after it for five. Not about money
    /// (<see cref="BotMeet.Unpaid"/>), so the ledger cannot refuse it for earning none.
    /// </summary>
    public static double Prior { get; set; } = 150.0;

    public sealed class Approach
    {
        public IBotWilful Carrier;

        public IBotWilful Buyer;

        public BotWant Want;

        public int Units;

        /// <summary>Where the two meet when the ground is unsafe; zero to walk towards each other.</summary>
        public Point3D Place;

        public bool Safe;

        public long CalledTick;

        public long Tick;

        /// <summary>Whether the buyer took the meeting up.</summary>
        public bool Taken;
    }

    private static readonly Dictionary<Serial, Approach> _approaches = [];

    // ---- Counted ---------------------------------------------------------------------------------------------------------

    public static long Arranged { get; private set; }

    /// <summary>Of the arranged, meetings moved to safe ground for the ground one of the two stood on.</summary>
    public static long OnSafeGround { get; private set; }

    public static long TakenUp { get; private set; }

    public static long Met { get; private set; }

    public static long Missed { get; private set; }

    /// <summary>The buyer waited the meeting out and the carrier never came, or came and nothing changed hands.</summary>
    public static long BuyerWaited { get; private set; }

    public static long StoodApart { get; private set; }

    /// <summary>The carrier gave the delivery up with a meeting standing.</summary>
    public static long CarrierGaveUp { get; private set; }

    /// <summary>Calls not taken because another carrier's meeting with the same buyer stood.</summary>
    public static long Doubled { get; private set; }

    // ---- The buyer stands for its carrier (12.10.2026, w7) -------------------------------------------------------------------

    /// <summary>
    /// <b>How near a carrier walking after its buyer must be before the buyer stands still for it (12.10.2026, w7).</b> Ten
    /// tiles. Of the 1 436 deliveries that ended "could not get nearer" on 11–12.10, 937 (65 %) ended with the carrier ten
    /// tiles or less from the buyer, 618 of them three to five. A meeting had been called for nearly all of them (L2: 568 of
    /// 585) and 544 of those buyers had not taken it up — they were mining, carrying their own deliveries, on supply runs —
    /// and walked on at their own pace, or a horse's. Patrick's order of 02.10 was that the two meet; the meeting is the buyer's choice and loses to its work, and
    /// this is the part that costs the buyer nothing: its walk is held where it stands (<see cref="BotJourney.Hold"/>, the
    /// delve leader's hold for its column) for <see cref="StandMs"/> at each of the carrier's beats, its work and its plan
    /// kept, until the goods are in its pack. Nought turns it off.
    /// </summary>
    public static int StandTiles { get; set; } = 10;

    /// <summary>How long each of the carrier's beats holds the buyer's walk; renewed by the next one, lapsing by itself (the delve's 1.5 s).</summary>
    public static int StandMs { get; set; } = 1500;

    /// <summary>
    /// The most a buyer stands for one carrier's walk, in all. Ten seconds: ten tiles is four seconds on foot, and a carrier
    /// that has not closed in ten is not closing; the buyer walks on and the carrier follows it as before. Not thirty (the first
    /// draft, review A of w7): the buyer's own work counts the beats it gets no nearer — a miner's walk to its seam, a supply
    /// run, a hire, a prowl give up at <c>TrekLimit</c> 200 beats, forty seconds — and although those counts now skip a held
    /// beat, a stand is somebody else's time spent and is kept short.
    /// </summary>
    public static int StandMostMs { get; set; } = 10000;

    /// <summary>Carriers' walks whose buyer stood for them, once a walk; of them handed over; and those whose buyer stood <see cref="StandMostMs"/> and walked on.</summary>
    public static long Stood { get; private set; }

    public static long StoodHanded { get; private set; }

    public static long StandSpent { get; private set; }

    /// <summary>Carriers' beats within <see cref="StandTiles"/> of a walking buyer that was not stood: in a company, a fight, a flight, or walking to a meeting.</summary>
    public static long StandPassed { get; private set; }

    /// <summary>One carrier's walk's account of how long its buyer has stood for it. Kept by the work that walks.</summary>
    public sealed class StandClock
    {
        internal bool Began;

        internal bool Spent;

        internal long Since;
    }

    /// <summary>
    /// The carrier is <paramref name="apart"/> tiles from its buyer and walking after it: a buyer walking about its own business
    /// is held where it stands for <see cref="StandMs"/>, within <see cref="StandMostMs"/> of the first time for this walk.
    /// Returns whether it was held. Not a buyer in a company (its company decides where it stands), in a fight, in flight, on a
    /// walk somebody pushed on top of its own, or walking to a meeting (it is coming already).
    /// </summary>
    public static bool Stand(Mobile carrier, Mobile buyer, int apart, StandClock clock)
    {
        if (!Running || StandTiles <= 0 || clock == null || clock.Spent || apart > StandTiles
            || buyer is not BotMobile { Deleted: false, Alive: true } bot || carrier == null || bot.Map != carrier.Map)
        {
            return false;
        }

        var journey = bot.Journey;

        // Not walking: it is at its work and standing already, and the carrier's next steps reach it. Walking is a step in the
        // last two seconds (BotJourney.Moving), or a stand already holding it.
        if (journey is not { Active: true } || journey.Current is not { } walk || !journey.Moving && !journey.Held)
        {
            return false;
        }

        if (walk.Interruption || bot.Squad != null || bot.Combatant != null || bot.Hidden
            || walk.Reason != null && walk.Reason.StartsWith("away from", StringComparison.Ordinal)
            || bot.Resolve?.Deed is BotMeet || bot.Resolve?.Deed is { } deed && (deed.Foe != null || deed.Braves))
        {
            StandPassed++;

            return false;
        }

        var now = Core.TickCount;

        if (!clock.Began)
        {
            clock.Began = true;
            clock.Since = now;
            Stood++;
        }
        else if (now - clock.Since >= StandMostMs)
        {
            clock.Spent = true;
            StandSpent++;

            return false;
        }

        journey.Hold(StandMs);

        return true;
    }

    /// <summary>The goods changed hands on a walk whose buyer had stood for it. See <see cref="Stand"/>.</summary>
    internal static void HandedAfterStand() => StoodHanded++;

    /// <summary>
    /// The carrier is near: the meeting is called, or said again. Returns the standing approach, or null when another
    /// carrier's meeting with this buyer stands.
    /// </summary>
    public static Approach Call(IBotWilful carrier, IBotWilful buyer, BotWant want, int units)
    {
        var body = carrier?.Self;
        var other = buyer?.Self;

        if (!Running || body is not { Deleted: false, Alive: true } || other is not { Deleted: false, Alive: true } || body.Map != other.Map)
        {
            return null;
        }

        var now = Core.TickCount;

        // <b>No meeting with a buyer whose pack will not take the goods (06.10.2026, work17).</b> On build 410 34 of 38 meet failures
        // were "<carrier> gave the delivery up before the meeting", 26 of them the carrier's refusal at this very buyer's full pack
        // within five seconds: the buyer walked to a hand-over its own pack refused. The carrier's own walk ends as it would; the
        // buyer is not sent on it. See BotRoom.Receives.
        if (BotRoom.Shut(other) || want != null && !BotRoom.Receives(other, want.Kind))
        {
            BotRoom.Note(other);
            BotRoom.MeetNotCalled();

            if (_approaches.TryGetValue(other.Serial, out var held) && ReferenceEquals(held.Carrier, carrier))
            {
                Gone(carrier, buyer);
            }

            return null;
        }

        if (_approaches.TryGetValue(other.Serial, out var standing) && now - standing.Tick < StaleMs)
        {
            if (!ReferenceEquals(standing.Carrier, carrier))
            {
                Doubled++;

                return null;
            }

            standing.Tick = now;
            standing.Units = units;

            return standing;
        }

        if (_approaches.Count >= 1024)
        {
            _approaches.Clear();
        }

        var approach = new Approach
        {
            Carrier = carrier,
            Buyer = buyer,
            Want = want,
            Units = units,
            CalledTick = now,
            Tick = now
        };

        approach.Place = SafePlace(body, other, out approach.Safe);

        _approaches[other.Serial] = approach;
        Arranged++;

        if (!approach.Safe)
        {
            OnSafeGround++;
        }

        logger.Information(
            "{Carrier} is {Tiles} tiles from {Buyer} with {Units} {Label} and calls the meeting{Place}",
            body.Name,
            Math.Max(Math.Abs(body.X - other.X), Math.Abs(body.Y - other.Y)),
            other.Name,
            units,
            want?.Label ?? "goods",
            approach.Place == Point3D.Zero
                ? ": they walk towards each other"
                : $" at ({approach.Place.X}, {approach.Place.Y}), guarded ground, since where they stand is not safe"
        );

        return approach;
    }

    /// <summary>
    /// Where the two meet: nowhere in particular (they walk towards each other) while both stand on safe ground; the nearest
    /// guarded ground to the point between them within <see cref="SafeReach"/> of both when one of them does not.
    /// </summary>
    private static Point3D SafePlace(Mobile carrier, Mobile buyer, out bool safe)
    {
        var map = carrier.Map;

        safe = !Unsafe(map, carrier) && !Unsafe(map, buyer);

        if (safe)
        {
            return Point3D.Zero;
        }

        var middle = new Point3D((carrier.X + buyer.X) / 2, (carrier.Y + buyer.Y) / 2, 0);
        var place = BotHaven.Nearest(map, middle, out _, out _);

        if (place == Point3D.Zero
            || Math.Max(Math.Abs(place.X - carrier.X), Math.Abs(place.Y - carrier.Y)) > SafeReach
            || Math.Max(Math.Abs(place.X - buyer.X), Math.Abs(place.Y - buyer.Y)) > SafeReach)
        {
            // No guarded ground near enough: they still walk towards each other, which is at least not a chase.
            return Point3D.Zero;
        }

        // A place two carriers lately found no way to is no meeting place (06.10.2026, deliver16): they walk towards each other.
        // BotHaven.Edge already passes such tiles by, so this holds only when the haven found nothing else. See BotWayless.
        if (BotWayless.Shut(map, place, true))
        {
            BotWayless.MovedMeeting();

            return Point3D.Zero;
        }

        return place;
    }

    /// <summary>Whether the ground a bot stands on is not safe for a meeting: a hostile or deadly patrol, or a bloodied square past its strength.</summary>
    private static bool Unsafe(Map map, Mobile body)
    {
        if (BotHaven.Guarded(map, body.Location))
        {
            return false;
        }

        if (BotZones.LevelAt(map, body.X, body.Y) is BotZoneLevel.Deadly or BotZoneLevel.Hostile)
        {
            return true;
        }

        return BotQuad.Muscle(map, body.Location) > BotThreat.Power(body);
    }

    /// <summary>The meeting called for this buyer, if one stands and is fresh; a stale one is forgotten and counted missed.</summary>
    public static Approach Standing(Mobile buyer)
    {
        if (buyer == null || !_approaches.TryGetValue(buyer.Serial, out var approach))
        {
            return null;
        }

        if (Core.TickCount - approach.Tick >= StaleMs)
        {
            _approaches.Remove(buyer.Serial);

            if (approach.Taken)
            {
                Missed++;
                FellOutOfCall++;
            }

            return null;
        }

        return approach;
    }

    /// <summary>The carrier stopped saying the meeting again for <see cref="StaleMs"/>: it fell out of call, or was taken off its delivery.</summary>
    public static long FellOutOfCall { get; private set; }

    /// <summary>The buyer took the meeting up. See <see cref="BotMeet"/>.</summary>
    public static void TookUp(Approach approach)
    {
        if (approach is { Taken: false })
        {
            approach.Taken = true;
            TakenUp++;
        }
    }

    /// <summary>The goods changed hands. Called by the carrier at the exchange.</summary>
    public static void Handed(IBotWilful buyer)
    {
        var body = buyer?.Self;

        if (body == null || !_approaches.Remove(body.Serial, out var approach))
        {
            return;
        }

        if (approach.Taken)
        {
            Met++;
        }
    }

    /// <summary>
    /// The carrier's delivery ended without the goods changing hands at this meeting: the meeting is forgotten, and counted
    /// missed when the buyer had taken it up.
    /// </summary>
    public static void Gone(IBotWilful carrier, IBotWilful buyer)
    {
        var body = buyer?.Self;

        if (body == null || !_approaches.TryGetValue(body.Serial, out var approach) || !ReferenceEquals(approach.Carrier, carrier))
        {
            return;
        }

        _approaches.Remove(body.Serial);

        if (approach.Taken)
        {
            Missed++;
            CarrierGaveUp++;
        }
    }

    /// <summary>The buyer gave the meeting up: waited it out, or stood beside the carrier and nothing changed hands.</summary>
    public static void Waited(Mobile buyer, bool apart)
    {
        if (buyer != null && _approaches.Remove(buyer.Serial, out var approach) && approach.Taken)
        {
            Missed++;

            if (apart)
            {
                BuyerWaited++;
            }
            else
            {
                StoodApart++;
            }
        }
    }

    public static string Describe() =>
        !Running
            ? "meetings are off; the carrier follows the buyer"
            : $"meetings: {Arranged} called by a carrier within {CallTiles} tiles ({OnSafeGround} of them moved to guarded ground for the ground one stood on), {TakenUp} taken up by the buyer, {Met} met with the goods handed over, {Missed} missed ({BuyerWaited} the buyer waited {MeetMs / 60000} minutes and the carrier never came, {StoodApart} stood together {WaitMs / 1000}s and nothing changed hands, {CarrierGaveUp} the carrier gave the delivery up first, {FellOutOfCall} the carrier fell out of call for {StaleMs / 1000}s), {Doubled} calls for a buyer already meeting another carrier; "
              + $"buyers stood for their carrier within {StandTiles} tiles: {Stood} walks ({StoodHanded} of them handed over, {StandSpent} stood {StandMostMs / 1000}s and walked on), {StandPassed} beats a walking buyer was not stood (a company, a fight, a flight, a meeting)";

    public static void Forget()
    {
        _approaches.Clear();
        Arranged = 0;
        OnSafeGround = 0;
        TakenUp = 0;
        Met = 0;
        Missed = 0;
        BuyerWaited = 0;
        StoodApart = 0;
        CarrierGaveUp = 0;
        FellOutOfCall = 0;
        Doubled = 0;
        Stood = 0;
        StoodHanded = 0;
        StandSpent = 0;
        StandPassed = 0;
    }
}

/// <summary>Offers a buyer whose carrier is near the walk to meet it. See <see cref="BotRendezvous"/>.</summary>
public sealed class BotMeeter : IBotProposer
{
    public string Name => "Meeter";

    public BotStanding Rung => BotStanding.Free;

    public static long Asked { get; private set; }

    public static long Offered { get; private set; }

    public BotDeed Propose(IBotWilful bot)
    {
        var body = bot?.Self;

        if (!BotRendezvous.Running || body?.Map == null || body.Map == Map.Internal || !body.Alive)
        {
            return null;
        }

        var approach = BotRendezvous.Standing(body);

        if (approach == null)
        {
            return null;
        }

        Asked++;

        // A bot in a company goes where the company goes; the carrier follows it as before.
        if (bot.Squad != null || approach.Carrier?.Self is not { Deleted: false, Alive: true } carrier || carrier.Map != body.Map)
        {
            return null;
        }

        // Already there, or already meeting it.
        if (bot.Resolve?.Deed is BotMeet)
        {
            return null;
        }

        Offered++;

        return new BotMeet(approach);
    }

    public static void Forget()
    {
        Asked = 0;
        Offered = 0;
    }
}

/// <summary>
/// The buyer's half of a hand-to-hand delivery: going to meet the carrier, and standing while the goods are handed over.
///
/// <para>
/// Pressing, because the carrier is standing a screen away now and the buyer's next review may be half a minute off; a
/// summons, because a buyer at a seam is still a buyer, and the seam is put down and taken up again. The exchange itself
/// is the carrier's (<see cref="BotDeliver"/>, <see cref="BotHaul"/>): this only puts the buyer where it can happen and
/// keeps it there, and ends when the want has been filled, or the carrier is gone, or the clocks run out.
/// </para>
/// </summary>
public sealed class BotMeet : BotDeed
{
    public const string Trade = "meet";

    private readonly BotRendezvous.Approach _approach;

    private readonly Map _map;

    private readonly int _filled;

    private long _begun;

    private long _besideTick;

    private bool _started;

    public BotMeet(BotRendezvous.Approach approach)
    {
        _approach = approach;
        _map = approach?.Carrier?.Self?.Map;
        _filled = approach?.Want?.Filled ?? 0;
    }

    public Mobile Carrier => _approach?.Carrier?.Self;

    public override string Kind => Trade;

    public override Map Map => _map;

    public override Point3D Where => _approach?.Place != Point3D.Zero ? _approach.Place : Carrier?.Location ?? Point3D.Zero;

    public override double Expects => BotRendezvous.Prior;

    public override double Minutes => 1.0;

    public override double Coin => 1.0;

    /// <summary>What the meeting brought in goods, so the ledger does not read it as a walk for nothing.</summary>
    public override int Made => _approach?.Want == null ? 0 : Math.Max(0, _approach.Want.Filled - _filled) * Math.Max(1, _approach.Want.Offer);

    /// <summary>Not about money: the buyer pays here. See <see cref="BotDeed.Unpaid"/>.</summary>
    public override bool Unpaid => true;

    /// <summary>The carrier is standing a screen away now. See <see cref="BotDeed.Pressing"/>.</summary>
    public override bool Pressing(IBotWilful bot) => true;

    /// <summary>A call from outside the bot's own business, through a steadfast hold; the seam is taken up again after.</summary>
    public override bool Summons => true;

    public override bool Resumes => true;

    /// <summary>A second call to the same meeting adds nothing.</summary>
    public override bool Repeats(BotDeed other) => other is BotMeet meet && ReferenceEquals(meet._approach, _approach);

    public override string Stage =>
        _approach?.Place != Point3D.Zero
            ? $"meeting {Carrier?.Name ?? "a carrier"} at ({_approach.Place.X}, {_approach.Place.Y}) for {_approach.Units} {_approach.Want?.Label}"
            : $"going to meet {Carrier?.Name ?? "a carrier"} for {_approach?.Units} {_approach?.Want?.Label}";

    public override void Taken(IBotWilful bot) => BotRendezvous.TookUp(_approach);

    public override void Resumed(IBotWilful bot) => _besideTick = 0;

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _approach == null || _map == null)
        {
            return BotDoing.Failed("no body");
        }

        var now = Core.TickCount;

        if (!_started)
        {
            _started = true;
            _begun = now;
        }

        var want = _approach.Want;

        // The goods came: the carrier's beat handed them over and forgot the meeting.
        if (want != null && want.Filled > _filled)
        {
            return BotDoing.Done($"met {Carrier?.Name ?? "the carrier"} and took {want.Filled - _filled} {want.Label}");
        }

        var standing = BotRendezvous.Standing(body);

        if (!ReferenceEquals(standing, _approach))
        {
            return BotDoing.Failed($"{Carrier?.Name ?? "the carrier"} gave the delivery up before the meeting");
        }

        var carrier = Carrier;

        if (carrier is not { Deleted: false, Alive: true } || carrier.Map != body.Map)
        {
            BotRendezvous.Waited(body, apart: true);

            return BotDoing.Failed($"{carrier?.Name ?? "the carrier"} is gone");
        }

        if (body.InRange(carrier.Location, BotHandover.MeetTiles))
        {
            if (_besideTick == 0)
            {
                _besideTick = now;
            }
            else if (now - _besideTick >= BotRendezvous.WaitMs)
            {
                BotRendezvous.Waited(body, apart: false);

                return BotDoing.Failed($"stood {BotRendezvous.WaitMs / 1000}s beside {carrier.Name} and nothing changed hands");
            }

            return BotDoing.Work($"waiting for {carrier.Name} to hand over {_approach.Units} {want?.Label}");
        }

        _besideTick = 0;

        if (now - _begun >= BotRendezvous.MeetMs)
        {
            BotRendezvous.Waited(body, apart: true);

            return BotDoing.Failed($"{carrier.Name} did not come to the meeting in {BotRendezvous.MeetMs / 60000} minutes");
        }

        var place = _approach.Place;

        if (place != Point3D.Zero && !body.InRange(place, 2))
        {
            return BotDoing.Walk(_map, place, BotArrival.Within(2), $"to the meeting place for {carrier.Name}");
        }

        if (place != Point3D.Zero)
        {
            return BotDoing.Work($"at the meeting place, waiting for {carrier.Name}");
        }

        // After a carrier that is walking too: a reached plan is a way (BotDoing.WalkAfter, 12.10.2026, w7); MeetMs ends it.
        return BotDoing.WalkAfter(_map, carrier, BotArrival.Within(BotHandover.MeetTiles), $"towards {carrier.Name} to meet it");
    }
}
