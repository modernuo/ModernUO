using System.Collections.Generic;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;
using Server.Multis;

namespace Server.BotAI.V2;

/// <summary>
/// Dig, melt, put away. One undertaking with three stages, and the second and third are the point of it.
///
/// <para>
/// <b>Ore is worth nothing to anybody.</b> No counter buys it, no bot wants it, and a miner that comes home
/// with a pack full of rock has produced exactly nothing — which is what the first version's miners did all
/// night, because their goal ended at the vein. So the work is not "mine": it is <em>mine, carry it to a
/// fire, and put the metal where it is safe</em>, and it is not finished until the last of those has
/// happened.
/// </para>
///
/// <para>
/// <b>Its stages are its own business.</b> The decision layer asks what to do now and is told a place, or
/// "work here", or that it is over; it never learns what ore is. That is the whole reason adding a trade to
/// this shard does not touch <c>BotWill/</c>.
/// </para>
/// </summary>
public sealed class BotDig : BotDeed
{
    /// <summary>
    /// The ledger's key for this kind of work. A kind, not an instance: "mine" learns from thirty trips,
    /// while "mine-the-vein-at-2144-891" would learn from one and forget it when the row was evicted.
    /// </summary>
    public const string Trade = "mine";

    /// <summary>
    /// What a mining trip is expected to come to, in gold-equivalent per minute, before this bot's own
    /// experience of a given seam is taken into account.
    ///
    /// <para>
    /// Forty-five, from arithmetic rather than taste: about twenty ingots a trip at
    /// <see cref="GoldPerIngot"/>, plus roughly half a point of mining across the swings, which at the
    /// project's exchange rate is worth several times the metal — and eight minutes of walking, digging and
    /// banking to earn it. That the skill dominates is not an accident of the numbers; it is what this
    /// population is for.
    /// </para>
    /// </summary>
    public static double Prior { get; set; } = 45.0;

    /// <summary>
    /// How long a trip is expected to take. Used to weigh the walk against the work: half an hour of
    /// digging is worth a five-minute walk and five minutes of digging is not.
    /// </summary>
    public static double WorkMinutes { get; set; } = 8.0;

    /// <summary>
    /// What an ingot is taken to be worth when the shard has nothing to say about it.
    ///
    /// <para>
    /// <b>It is now the last resort rather than the answer.</b> <see cref="BotMarket.Worth"/> is asked
    /// first: what somebody is actually offering for metal, with the money down, and failing that what metal
    /// has really changed hands for on a stall. This six is what is left when neither has ever happened —
    /// which on a fresh shard is the first trip and nothing after it.
    /// </para>
    ///
    /// <para>
    /// That ordering is the whole of how a shortage reaches a miner. Nobody tells it to dig; a want for metal
    /// raises what its metal counts for, the takings go into the ledger, and the ledger raises its estimate
    /// of digging next time. Which is the same trick as everything else here: arithmetic from a shared fact,
    /// not a message.
    /// </para>
    /// </summary>
    public static int GoldPerIngot { get; set; } = 6;

    /// <summary>How much of what the bot can carry may be filled with rock before it goes to the fire.</summary>
    public static double FillFraction { get; set; } = 0.8;

    /// <summary>Ore enough to head for a fire whatever the scales say.</summary>
    public static int TargetOre { get; set; } = 20;

    /// <summary>
    /// How many swings without anything new in the pack before the tile is given up on, at a miner who never
    /// misses.
    ///
    /// <para>
    /// <b>A backstop that was doing the deciding, and it was sized for a master.</b> The reasoning was that
    /// emptiness is read from the engine, so this only had to catch a run of bad rolls — six being "enough to
    /// ride out ordinary bad luck". Ordinary luck for this population is not what that assumed:
    /// <c>HarvestSystem</c> rolls against iron's nought-to-a-hundred, so a bot with thirty mining misses
    /// seven swings in ten, and a run of six misses is not bad luck, it is Tuesday.
    /// </para>
    ///
    /// <para>
    /// Measured on the morning of 09.09.2026, the first time the two cases were ever counted apart:
    /// <b>16 rocks given up with the engine's bank under them empty against 35 still holding ore.</b> Two in
    /// three rocks written off as worked out were full, the seam was then rested for everybody, and the log
    /// said the island was running out of ore. It was not. The miners were missing.
    /// </para>
    ///
    /// <para>
    /// So the real limit is this divided by what the engine says the bot's chance actually is — see
    /// <see cref="BotOre.Chance"/> — floored here and capped at <see cref="MostDry"/>. A master gives up
    /// after six quiet swings and a novice after twenty, which is the same statement about both of them.
    /// </para>
    /// </summary>
    public static int DryLimit { get; set; } = 6;

    /// <summary>And the most, however hopeless the miner. A minute of swinging at one rock is enough.</summary>
    public static int MostDry { get; set; } = 24;

    /// <summary>
    /// How near a counter a bot has to stand to bank and to put goods out.
    ///
    /// <para>
    /// <b>Nothing in the engine gates this, and that is the fact the number was missing.</b>
    /// <c>Banker.Deposit</c> credits the account or the bank box from anywhere on the map — it does not so
    /// much as look at where the depositor is standing — and this market is placeless, so a stall can be set
    /// up from a field. The walk to a counter is verisimilitude: a bot puts its takings away at a bank,
    /// because that is what a bank is for. The distance was two here and three in <c>BotUnload</c>, both
    /// copied across from <c>BotShops.CounterReach</c> — which is three because a <em>vendor</em> refuses to
    /// trade further off than that, a rule about a conversation that never happens here.
    /// </para>
    ///
    /// <para>
    /// So the errand was demanding that a bot lean over the counter to do something it could have done from
    /// the doorway, and a banker stands <em>behind</em> a counter: the tiles against it are furniture. With
    /// twenty bots and exactly two bankers on the shard, the ones who could reach four tiles and not three
    /// spent twelve plans proving it and then failed — 25 of the 58 refused walks in the hour to 16:25 on
    /// 26.08.2026, on those two bankers alone, while 44 other trips to the very same counters succeeded.
    /// </para>
    ///
    /// <para>
    /// <b>Six is a judgement and not a derivation</b>, and it is worth saying so plainly: it is about the
    /// size of a bank, so the bot stands inside the building or in its doorway rather than in the street or
    /// on the clerk's toes. What is <em>not</em> a judgement is that there should be one of these numbers
    /// rather than two — see <see cref="BotUnload.Reach"/>, which now asks this one.
    /// </para>
    /// </summary>
    public static int CounterReach { get; set; } = 6;

    /// <summary>How many times this undertaking may pick somewhere else before giving up.</summary>
    public static int MaxBends { get; set; } = 3;

    /// <summary>
    /// How many emptied rocks one trip may work through before it takes what it has and goes.
    ///
    /// A trip, not a bot: this is not knowledge worth keeping, because the engine refills a bank of ore in
    /// time and a rock written off for ever would be a mine that shrinks every session.
    /// </summary>
    public static int MaxSpent { get; set; } = 8;

    /// <summary>
    /// How long a miner rests between swings.
    ///
    /// <para>
    /// A second, by order of 24.08.2026. There is no engine rule wanting one — the harvest system takes a
    /// swing whenever it is offered — so without a number here the rate was simply however often the
    /// population's clock came round to this bot, which is several times a second. Every other trade on the
    /// shard already had this and mining was the one that did not.
    /// </para>
    /// </summary>
    public static int SwingMs { get; set; } = 2000;

    private enum Leg
    {
        Seam,
        Fire,
        Counter
    }

    private readonly Map _map;

    private IBotWilful _bot;

    private BotSeam _seam;

    private Leg _leg;

    private IPoint3D _tile;

    private HarvestSystem _system;

    /// <summary>On foot while a rock is chosen and being worked: the engine will not mine from a saddle. See <see cref="BotDeed.Afoot"/>.</summary>
    public override bool Afoot => _leg == Leg.Seam && _tile != null;

    /// <summary>Rocks found only by looking again from the middle of the seam, where the search from the bot found none.</summary>
    public static long FarSide { get; private set; }

    /// <summary>How many times one trip may go on to another seam because the one it was walking to was already held.</summary>
    public static int RepickLimit { get; set; } = 2;

    /// <summary>Trips sent on to another seam because another miner already held the one they were walking to.</summary>
    public static long Repicked { get; private set; }

    /// <summary>Trips given up because another miner held their seam and no other seam was free.</summary>
    public static long Beaten { get; private set; }

    /// <summary>Seams rested rather than struck off because every rock in reach was there and worked out. See the no-rock branch.</summary>
    public static long WorkedOut { get; private set; }

    private Point3D _fire;

    private Point3D _counter;

    private int _swings;

    /// <summary>Beats spent walking at the current rock without getting inside swinging reach.</summary>
    private int _approaches;

    /// <summary>The rock the stand below was chosen for, the stand, and how far it lies from the rock. See the rock walk.</summary>
    private Point3D _standRock;

    private Point3D _standAt;

    private int _standRing;

    /// <summary>How far round a seam to look for ground to walk to, when the seam's own tile is the mountain.</summary>
    public static int SeamStandTiles { get; set; } = 4;

    /// <summary>Rocks written off at once because no floor within swinging reach of them takes a body. See <see cref="Stand"/>.</summary>
    public static long Standless { get; private set; }

    /// <summary>
    /// A floor within <paramref name="most"/> tiles of a point that takes a body, nearest ring first and, within a ring,
    /// nearest <paramref name="toward"/>; nought when there is none. The same answer for the same point every beat, so the
    /// walk it is handed stays one order (a walk order must not change under the walker). <paramref name="ring"/> is how
    /// far the floor lies from the point.
    /// </summary>
    private static Point3D Stand(Map map, Point3D point, int most, Point3D toward, out int ring, bool level = false)
    {
        // Level: every ring at once, the floor nearest the walker's own height first and then nearest the walker — a stand
        // twenty units above the miner is a cliff top, not the foot of the rock.
        if (level)
        {
            var chosen = Point3D.Zero;
            var cheapest = int.MaxValue;
            var chosenRing = -1;

            for (var dx = -most; dx <= most; dx++)
            {
                for (var dy = -most; dy <= most; dy++)
                {
                    var x = point.X + dx;
                    var y = point.Y + dy;

                    if (!BotStep.Settle(map, x, y, out var z) || BotFooting.Footless(map, x, y) || BotStep.Wet(map, x, y))
                    {
                        continue;
                    }

                    var climb = System.Math.Max(0, System.Math.Abs(z - toward.Z) - 4);
                    var cost = climb * 3 + System.Math.Max(System.Math.Abs(toward.X - x), System.Math.Abs(toward.Y - y));

                    if (cost < cheapest)
                    {
                        cheapest = cost;
                        chosen = new Point3D(x, y, z);
                        chosenRing = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
                    }
                }
            }

            ring = chosenRing;

            return chosen;
        }

        for (ring = 0; ring <= most; ring++)
        {
            var best = Point3D.Zero;
            var least = int.MaxValue;

            for (var dx = -ring; dx <= ring; dx++)
            {
                for (var dy = -ring; dy <= ring; dy++)
                {
                    if (System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy)) != ring)
                    {
                        continue;
                    }

                    var x = point.X + dx;
                    var y = point.Y + dy;

                    if (!BotStep.Settle(map, x, y, out var z) || BotFooting.Footless(map, x, y) || BotStep.Wet(map, x, y))
                    {
                        continue;
                    }

                    var away = System.Math.Max(System.Math.Abs(toward.X - x), System.Math.Abs(toward.Y - y));

                    if (away < least)
                    {
                        least = away;
                        best = new Point3D(x, y, z);
                    }
                }
            }

            if (best != Point3D.Zero)
            {
                return best;
            }
        }

        ring = -1;

        return Point3D.Zero;
    }

    /// <summary>The closest this bot has come to the seam on the long walk, and how long since that improved.</summary>
    private int _nearest = int.MaxValue;

    private int _routeStamp = -1;

    /// <summary>How far off the seam was when the long walk began, for the note about the miner's footing.</summary>
    private int _setOut;

    private int _stalled;

    /// <summary>Seams this trip has gone on to because its own was held. See <see cref="RepickLimit"/>.</summary>
    private int _repicks;

    /// <summary>Rocks written off in a row for being unreachable. See <see cref="WalledLimit"/>.</summary>
    private int _walled;

    /// <summary>
    /// How many beats a miner will spend closing on one rock before writing it off.
    ///
    /// Forty, which at the population's own beat is a few seconds of honest walking and well short of the
    /// four minutes it used to take somebody watching to notice.
    /// </summary>
    public static int ApproachLimit { get; set; } = 40;

    /// <summary>
    /// How many beats the long walk to a seam may go without getting any nearer before the seam is struck off.
    ///
    /// <para>
    /// Two hundred, which at a turn every two hundred milliseconds is about forty seconds of not closing the
    /// distance. Generous on purpose: the walk itself is meant to be long, and the thing being caught is not
    /// slowness but a destination that cannot be reached at all.
    /// </para>
    /// </summary>
    public static int TrekLimit { get; set; } = 200;

    /// <summary>
    /// How near a seam the long walk must have got for its giving up to be a finding about the seam.
    ///
    /// Four times <c>BotOre.Reach</c>: the walk ends at one reach, and a bot held up within a few of them is held up by
    /// the seam's own hillside. Past that the fault is somewhere along the road, or under the miner. See
    /// <c>BotGround.ShiedLimit</c> for the numbers.
    /// </summary>
    public static int StrikeWithin { get; set; } = 48;

    /// <summary>
    /// How many rocks in a row may be found unreachable before the seam itself is struck off.
    ///
    /// Three. One is bad luck, two is a coincidence, and three of them behind the same wall is the wall.
    /// </summary>
    public static int WalledLimit { get; set; } = 3;

    /// <summary>Seams struck off because nobody could walk to them. See <see cref="TrekLimit"/>.</summary>
    public static long Unwalkable { get; private set; }

    /// <summary>Rocks written off because nothing could get within swinging reach of them.</summary>
    public static long Unreachable { get; private set; }

    // ---- Two ways a rock stops yielding, and they were the same number. -------------------------------
    //
    // <b>The comment on DryLimit has always named both and nothing ever counted them apart.</b> A rock goes
    // quiet either because the engine's bank under it is empty — the honest end of a vein — or because the
    // bot is not good enough for it and is missing its rolls on rock that is still full. Both write the tile
    // off, both end the trip with "found no more", and the difference between them is the difference between
    // "the island is worked out" and "the miners cannot mine". On 09.09.2026 the shard was reporting the
    // first at 7% finishing in an hour and nobody could say which it was, because one number stood for both.

    /// <summary>Rocks given up on with the engine's own bank under them reading empty.</summary>
    public static long Drained { get; private set; }

    /// <summary>Rocks given up on with ore still in them: a run of missed rolls, not a worked-out vein.</summary>
    public static long Fumbled { get; private set; }

    /// <summary>Trips that ended because the trip's allowance of rocks ran out rather than the ground did.</summary>
    public static long Allowanced { get; private set; }

    /// <summary>
    /// The chance the engine gave the bots that gave up on rock which still held ore, summed, and how many
    /// of them there were — so the summary can print the average.
    ///
    /// <para>
    /// <b>Written because two hypotheses about the fumbles were both wrong and a third guess was not worth
    /// making.</b> The count climbs with the length of a session — 22 drained against 1 fumbled twenty
    /// minutes in, 32 against 219 an hour in, on the same population. A full pack would explain it and does
    /// not: <see cref="Digging"/> leaves for the fire at <c>FillFraction</c> of the carrying ceiling before a
    /// swing is ever taken. Harder veins would explain it and do not: below a vein's requirement the engine
    /// falls back to iron and the roll is against iron either way, so a bot's chance is its mining skill over
    /// a hundred and nothing else.
    /// </para>
    ///
    /// <para>
    /// So the thing to print is the chance itself. If the average sits near what the population's mining
    /// skill implies, <see cref="MostDry"/> is simply too small a cap for a green miner — twenty-four misses
    /// at one chance in ten happens eight times in a hundred. If it sits far above, something is stopping
    /// the swings that has nothing to do with the rolls, and that is a different hunt.
    /// </para>
    /// </summary>
    public static double FumbledChance { get; private set; }

    /// <summary>
    /// Swings the engine could not even begin, because the last one had not let go of the pickaxe.
    ///
    /// <para>
    /// <b>The one thing left that would explain a bot missing twenty-four times at two chances in three.</b>
    /// The measured chance behind the fumbles is 67% — a run of twenty-four misses at that rate happens once
    /// in a million million, so the rolls are not being made. <c>StartHarvesting</c> takes
    /// <c>BeginAction(tool)</c> and hands it back in <c>FinishHarvesting</c>, nine tenths of a second later;
    /// if anything loses that timer — a bot that dies mid-swing, a world reload, a range check that ends the
    /// harvest by another road — the lock is never given back and every swing that bot takes for the rest of
    /// its life is a silent no-op. Nothing anywhere would say so: <c>StartHarvesting</c> returns void.
    /// </para>
    ///
    /// <para>
    /// Counted rather than fixed, because which of those it is decides what the cure is, and a cure applied
    /// to the wrong one is how a morning goes.
    /// </para>
    /// </summary>
    public static long Locked { get; private set; }

    /// <summary>
    /// Quiet swings taken by a bot that had moved since the one before it.
    ///
    /// <para>
    /// <b>The last candidate standing, and the only one that explains "it arrives with time".</b> A harvest
    /// does not resolve where it starts: <c>OnHarvesting</c> and then <c>FinishHarvesting</c> both call
    /// <c>CheckRange(..., timed: true)</c>, nine tenths of a second after the swing, and a bot that has moved
    /// out of the two-tile reach in that time has its harvest ended with <c>TimedOutOfRangeMessage</c> and
    /// nothing in its pack. From inside the errand that is indistinguishable from a missed roll — which is
    /// why the fumbles read as bots missing at 86%, a rate at which twenty-four misses in a row is a one in
    /// a hundred million million million event.
    /// </para>
    ///
    /// <para>
    /// And it fits the one thing every other candidate failed to explain: it grows with the length of a
    /// session, because companies form as a session goes on and a bot on the Bound rung is walked about by
    /// its squad while it holds its own errand.
    /// </para>
    /// </summary>
    public static long Stirred { get; private set; }

    /// <summary>Swings the engine cancelled because the bot had moved while they resolved. See BotHeard.</summary>
    public static long Adrift { get; private set; }

    /// <summary>Swings whose ore was destroyed for want of room in the pack. See BotHeard.</summary>
    public static long Laden { get; private set; }

    /// <summary>Trips that carried on with the next pickaxe in the pack when the one in use wore out.</summary>
    public static long NextPickaxe { get; private set; }

    /// <summary>
    /// Melts the engine took none of the ore for: the forge refused the bot where it stood. See <see cref="Melting"/>.
    ///
    /// <para>
    /// <b>Refused is not burned, and until build 225 both read "the ore burned away".</b> A smelt that fails its roll
    /// halves the pile; a forge the engine will not let the bot use — out of its line of sight, inside a house the bot is
    /// outside of — takes nothing at all. The dig counted both as a burn, ended the trip, and the next trip began at the
    /// same fire with the same ore: Merrick put four piles into the fire of The Hammer's hall nine times in the second
    /// 07:31:32 on 25.09.2026, and 454 of 752 mining trips that session "burned away", 422 of them at that one forge
    /// (1752-1759, 1476-1483, z 0), none of them successful, against 19 melted there from inside the hall at z 7.
    /// </para>
    /// </summary>
    public static long Spurned { get; private set; }

    /// <summary>Times a miner refused at a forge stepped up beside it and tried again. See <see cref="Melting"/>.</summary>
    public static long Nearer { get; private set; }

    /// <summary>Forges rested for everybody because the engine refused a melt at them. See <see cref="Melting"/>.</summary>
    public static long Forsaken { get; private set; }

    /// <summary>How many forges may refuse one trip before its ore is carried on as it is. See <see cref="Melting"/>.</summary>
    public static int MostRefusals { get; set; } = 2;

    /// <summary>
    /// How long a forge that sent a miner closer is remembered: refused again from a distance inside it, the forge is
    /// rested rather than approached again, because the last miner to step closer did not melt there either.
    /// </summary>
    public static int NearingMs { get; set; } = 1800000;

    /// <summary>Forges a refused miner lately stepped up to. Cleared by a melt that works there. See <see cref="NearingMs"/>.</summary>
    private static readonly Dictionary<Point3D, long> _nearing = [];

    /// <summary>The pack's bars by kind before and after a melt, so each metal the fire gave is booked under its own name. See Smelting.</summary>
    private static readonly Dictionary<System.Type, int> _barsBefore = [];

    private static readonly Dictionary<System.Type, int> _barsAfter = [];

    private static void Bars(Mobile body, Dictionary<System.Type, int> into)
    {
        into.Clear();

        var items = body?.Backpack?.Items;

        if (items == null)
        {
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i] is BaseIngot { Deleted: false } bar)
            {
                into[bar.GetType()] = into.GetValueOrDefault(bar.GetType()) + System.Math.Max(1, bar.Amount);
            }
        }
    }

    /// <summary>Whether this trip is standing beside its fire after a refusal from further off.</summary>
    private bool _closer;

    /// <summary>Forges that refused this trip's ore. See <see cref="MostRefusals"/>.</summary>
    private int _refusals;

    /// <summary>Where the bot stood when it last swung, for <see cref="Stirred"/>.</summary>
    private Point3D _swungFrom;

    /// <summary>Whether a swing has been taken, and when. See <see cref="SwingMs"/>.</summary>
    private bool _swung;

    private long _swungTick;

    private int _dry;

    private int _seen;

    private int _made;

    /// <summary>Ore raised on this trip and not yet turned into bars. See <see cref="Made"/>.</summary>
    private int _raw;

    /// <summary>What one of those is reckoned at. Taken once, when the first comes out of the ground.</summary>
    private int _rawWorth;

    /// <summary>What a bar was reckoned at when the takings were counted. Kept so the same number comes back
    /// off <see cref="Made"/> if one is sold into a want a moment later.</summary>
    private int _worth;

    private int _stored;

    private int _bends;

    /// <summary>Rocks this trip has emptied. See <see cref="MaxSpent"/> for why it dies with the trip.</summary>
    private readonly List<Point3D> _spent = [];

    /// <summary>Of those, how many the engine said were actually out of ore.</summary>
    private int _ranDry;

    /// <summary>And how many still held ore this bot could not get out. See <see cref="DryLimit"/>.</summary>
    private int _missed;

    public BotDig(BotSeam seam)
    {
        _seam = seam;
        _map = seam.Map;
    }

    public override string Kind => Trade;

    /// <summary>A mining trip is seen through — the seam, the fire and the counter. See <see cref="BotDeed.Steadfast"/>.</summary>
    public override bool Steadfast => true;

    /// <summary>
    /// Held for <see cref="HoldMinutes"/> rather than the will's eight-minute cap (01.10.2026, build 386): a trip to a far seam
    /// is reckoned past eight minutes, and all fourteen outbid mining trips of build 386 were dropped at 7.9–9.3 minutes, on
    /// the way or at the rock. The trip has its own allowance of swings, its nearing clock and the seam's own endings, and
    /// the trek watchdog runs under the hold. See <see cref="BotDeed.HoldsFor"/>.
    /// </summary>
    public override double HoldsFor => HoldMinutes;

    /// <summary>Minutes a mining trip is held for whatever it pays. See <see cref="HoldsFor"/>.</summary>
    public static double HoldMinutes { get; set; } = 12.0;

    /// <summary>
    /// Back at the seam, or on the way to the fire, after an interruption. The swing clock is from before it,
    /// and ore may have left the pack while the bot was away, which would leave the count of what is carried
    /// above what is there and hide every piece dug from then on.
    /// </summary>
    public override void Resumed(IBotWilful bot)
    {
        _swung = false;
        _dry = 0;

        if (bot?.Self is { } body)
        {
            _seen = System.Math.Min(_seen, BotOre.Carried(body));
        }
    }

    public override Map Map => _map;

    /// <summary>
    /// The seam, for the whole life of the undertaking — <b>never the stage's own destination</b>.
    ///
    /// This is what the ledger files the outcome under, and the thing being learned is whether mining this
    /// patch of ground pays. Returning the bank's location while banking would file a mining trip under the
    /// bank, and the bot would slowly learn that banks are rich in ore.
    /// </summary>
    public override Point3D Where => _seam.Where;

    public override double Expects => Prior;

    public override double Minutes => WorkMinutes;

    /// <summary>Mining, named outright. Smelting is a mining check too — the fire does the rest.</summary>
    public override SkillName? Trains => SkillName.Mining;

    /// <summary>
    /// Nothing at all arrives as coin, and that stays true even though the metal now goes to a market.
    ///
    /// A stall is not a sale: the gold appears whenever a buyer does, which is minutes or hours later and
    /// belongs to whatever the bot is doing then. Counting the asking price as takings here would be counting
    /// the same metal twice — once as produced and once as sold.
    /// </summary>
    public override double Coin => 0.0;

    /// <summary>
    /// What this trip has produced: bars if it has reached a fire, and the ore in the pack if it has not.
    ///
    /// <para>
    /// <b>Ore in a pack was worth nothing, and it is the largest single thing wrong with mining on this
    /// shard.</b> <c>_made</c> only became a number at the forge, so a bot that had dug twenty ore and was
    /// carrying it recorded <c>dropped mine: carrying ore to a fire: 0 in 2.0 min (0/min): 0 coin, 0 made</c>
    /// — and <c>BotCommons.Corrected</c> then drags the whole trade's estimate towards nought. Measured over
    /// forty-five minutes on 09.09.2026: 130 mining trips, 42 finished, 17 failed and <b>71 dropped</b>, of
    /// which 46 were dropped after the digging was done, on the walk to the fire or to the counter. Mining
    /// was teaching itself that mining is worthless, and then acting on it.
    /// </para>
    ///
    /// <para>
    /// The rule is <c>BotRestock</c>'s and it has always been stated there in as many words: goods are worth
    /// what they cost. Ore that exists is produce, whatever leg the errand is on. Priced by the market rather
    /// than by a guess here — the same call the bars use — and replaced rather than added to when it becomes
    /// bars, so nothing is counted twice.
    /// </para>
    /// </summary>
    public override int Made => _made + _raw * _rawWorth;

    public override string Stage => _leg switch
    {
        Leg.Seam => $"digging {_seam.Ore} ({_swings} swings)",
        Leg.Fire => "carrying ore to a fire",
        _ => "putting the metal away"
    };

    /// <summary>
    /// What to do now.
    ///
    /// A bounded loop rather than recursion: a stage that has finished says so by returning nothing, and the
    /// next stage is asked in the same beat — so a bot that fills its pack walks towards the fire
    /// immediately instead of standing in the mine for a beat first. Bounded, because a stage that cannot
    /// decide must not be able to spin.
    /// </summary>
    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null)
        {
            return BotDoing.Failed("no body");
        }

        // Kept for the legs that need to ask the ground on this bot's behalf. Melting is handed only the body,
        // and which forges this particular bot has failed to reach is a fact about the bot.
        _bot = bot;

        // Six: one more than a stage transition per leg plus the instruction that follows. Exactly enough
        // would be four, and exactly enough is what turns adding a fourth leg into a silent "could not
        // settle" rather than a working trip.
        for (var guard = 0; guard < 6; guard++)
        {
            var doing = _leg switch
            {
                Leg.Seam => Digging(bot, body),
                Leg.Fire => Melting(body),
                _ => Banking(bot)
            };

            if (doing.Kind != BotDoingKind.None)
            {
                return doing;
            }
        }

        return BotDoing.Failed("could not settle on a next step");
    }

    private BotDoing Digging(IBotWilful bot, Mobile body)
    {
        var carried = BotOre.Carried(body);

        // Enough, or as much as it can walk with. The load test is the one that usually fires: a pack holds
        // about twelve ore before the engine starts charging stamina for every step, and past that a bot
        // digs itself to a standstill. The first version lost three bots to exactly that for a session —
        // the log insisted six hundred times that the step was allowed, and it was; stamina was not in the
        // message.
        if (carried >= TargetOre || Loaded(body))
        {
            _leg = Leg.Fire;

            return default;
        }

        var tool = BotOre.Tool(body);

        if (tool == null)
        {
            // <b>A pickaxe that wears through ends the digging, not the trip.</b> See BotInscribe.Writing: the engine
            // destroys a tool on its last use, and this failed with the ore just dug still in the pack. Carried to the
            // fire when there is enough to melt, exactly as it is when the seam gives out below.
            if (carried >= BotOre.WorthSmelting)
            {
                _leg = Leg.Fire;

                return default;
            }

            return BotDoing.Failed("nothing to dig with");
        }

        // The long walk. Only when it is out of looking range: the seam is a remembered patch of ground,
        // not a particular tile, and what is actually swung at is chosen on arrival.
        //
        // <b>And only while nothing has been chosen to swing at.</b> A bot that has picked its rock is walking
        // to the rock, and a second destination arguing for the seam behind it is not a fallback — it is the
        // other half of a bot pacing between two tiles. The rock is inside the seam by construction now (see
        // the leash below), so the two never really disagreed; this makes it impossible for them to.
        if (_tile == null && !body.InRange(_seam.Where, BotOre.Reach))
        {
            // <b>And a seam that cannot be walked to is not a seam either.</b> The rock leg below has said so
            // since the day two gatherers stood four minutes each on "digging ShadowIronOre (0 swings)"; this
            // leg, the longer walk that comes before it, had no such limit and could ask for the same journey
            // for ever. On 03.09.2026 Calla the Crafter held mine for ten minutes at 1360,1559, walking to
            // 1361,1574 for seven of them and never getting nearer than the fifteen tiles it started at, with
            // not one line in the log the whole time: the journey never fails, so the work never ends, so
            // nothing is ever written down. Measured as progress rather than as attempts, because the walk
            // itself is legitimate and long — what is not legitimate is a walk that stops getting closer.
            // <b>Held from the first step, not from the first swing.</b> The hold on a seam was taken only while it was being
            // worked, so every miner that chose in the same minute walked to the same rock: at 01:23 on build 44 six of them
            // reached one bronze seam inside thirty-five seconds, the first found nothing there and struck it off, and the
            // other five failed "somebody had already struck the seam off" after a minute's walk each. The walk renews the
            // hold every beat as the swing does, so it lapses the same way when nobody is coming; a miner that finds another
            // holding the seam it is walking to goes on to a free one instead of arriving second.
            if (!BotGround.Free(body, _seam.Where))
            {
                var other = _repicks < RepickLimit ? BotGround.Seam(bot, _seam.Where) : default;

                if (!other.Exists)
                {
                    Beaten++;

                    return BotDoing.Failed($"another miner holds the {_seam.Ore}, and no other seam is free");
                }

                _repicks++;
                Repicked++;

                _seam = other;
                _tile = null;
                _dry = 0;
                _spent.Clear();
                _nearest = int.MaxValue;
                _stalled = 0;
            }

            BotGround.Working(body, _seam.Where);

            var gap = System.Math.Max(System.Math.Abs(body.X - _seam.Where.X), System.Math.Abs(body.Y - _seam.Where.Y));

            // By the road, and started again with each route the walk draws. See BotJourney.RoadLeft.
            if (body is BotMobile { Journey: { } journey })
            {
                gap = journey.RoadLeft(body.Location, _seam.Where);

                if (_routeStamp != journey.RouteStamp)
                {
                    _routeStamp = journey.RouteStamp;
                    _nearest = int.MaxValue;
                    _stalled = 0;
                }
            }

            if (_nearest == int.MaxValue)
            {
                _setOut = gap;
            }

            if (gap < _nearest)
            {
                _nearest = gap;
                _stalled = 0;
            }
            // A held walk is not a walk that stopped closing (12.10.2026, w7): a buyer stood for its carrier (BotRendezvous.Stand)
            // or a trader for its guards stands by somebody else's word, and BotWill's own watch skips those beats too.
            else if (body is not BotMobile { Journey.Held: true } && ++_stalled >= TrekLimit)
            {
                // A walk that closed nothing is a note about this miner's footing, whatever becomes of the seam. This
                // leg gives up at two hundred beats, so the watchdog above every undertaking never sees it to write one.
                BotAppraisal.Becalm(bot.Resolve, body.Location, _setOut, _nearest);

                // <b>Far from the seam, nothing has been proved about the seam.</b> See BotGround.ShiedLimit: struck off
                // only from near enough to be the seam's own ground, or when three walks have given up on it.
                if (gap > StrikeWithin)
                {
                    var third = BotGround.Shy(_map, _seam.Where);

                    if (third)
                    {
                        Unwalkable++;
                    }

                    return BotDoing.Failed(
                        third
                            ? $"the walk to the {_seam.Ore} at ({_seam.Where.X}, {_seam.Where.Y}) stopped closing {gap} tiles short, the {BotGround.ShiedLimit}rd to do so, and the seam is struck off"
                            : $"the walk to the {_seam.Ore} at ({_seam.Where.X}, {_seam.Where.Y}) stopped closing {gap} tiles short, which says nothing of the seam; it rests and stays on the board"
                    );
                }

                // Struck off for everybody, exactly as an empty one is and for the reason given on Barren:
                // what one bot proves by standing on the ground is true for all of them.
                var struck = BotGround.Barren(_seam.Where);

                if (struck)
                {
                    Unwalkable++;
                }

                // The same distinction as the rock leg below: struck off by this bot, or found already gone.
                return BotDoing.Failed(
                    struck
                        ? $"no way through to the {_seam.Ore} in {gap} tiles, and the seam is struck off"
                        : $"no way through to the {_seam.Ore} in {gap} tiles; somebody had already struck it off"
                );
            }

            // Walked to ground beside the seam that takes a body, not to the seam's own height on the mountain. See Stand.
            var seamStand = Stand(_map, _seam.Where, SeamStandTiles, _seam.Where, out _);

            return BotDoing.Walk(_map, seamStand == Point3D.Zero ? _seam.Where : seamStand, BotArrival.Within(2), $"to the {_seam.Ore}");
        }

        if (_tile == null && _spent.Count < MaxSpent)
        {
            // Kept on the seam's lead. Looking outwards from the bot while judging arrival from the seam is
            // what let a rock be chosen that the bot had to leave the seam to reach, and then leave the rock
            // to return to the seam. See BotOre.Find's leash.
            _tile = BotOre.Find(body, out _system, _spent, _seam.Where, BotOre.Reach);

            // <b>And from the middle of the seam before it is called barren.</b> The walk above stops as soon as the bot
            // is within BotOre.Reach of the seam, and the search looks outwards from the bot on the seam's lead of the
            // same length — so from the edge of that reach it covers only the side of the seam the bot is standing on.
            // Build 43 put an instrument on it: all nine "no rock worth swinging at" were looked at from twelve tiles off.
            if (_tile == null && !body.InRange(_seam.Where, 2))
            {
                _tile = BotOre.Find(body, out _system, _spent, _seam.Where, BotOre.Reach, _seam.Where);

                if (_tile != null)
                {
                    FarSide++;
                }
            }
        }

        if (_tile == null || _system == null)
        {
            // Either there was never anything workable here — it happens, the sweep samples every fourth
            // tile — or everything within reach has been emptied. Carrying home what there is beats carrying
            // home nothing.
            if (carried >= BotOre.WorthSmelting)
            {
                _leg = Leg.Fire;

                return default;
            }

            if (_spent.Count > 0)
            {
                // <b>Two endings wearing one sentence, and the commoner of the two was the wrong one.</b>
                // 252 of 309 of these lines on 09.09.2026 read "emptied 8 rocks", which is exactly MaxSpent:
                // the trip had used up its own allowance of rocks and stopped looking, and the seam was then
                // rested as though the ground had run dry. A seam rested for a budget nobody spent is a seam
                // taken off the board while it is still full of ore, and it is the population that pays for
                // it — the board shrinks, the miners walk further, and the log says the island is exhausted.
                if (_spent.Count >= MaxSpent)
                {
                    Allowanced++;
                }

                // <b>Whose fault the rock went quiet decides where the note goes, and until now every note
                // went to the same place.</b> Rock the engine says is empty is a fact about the ground and
                // belongs on the board, where it stops every miner. Rock still full that this bot could not
                // work is a fact about this bot — a run of missed rolls on a vein above its skill — and
                // resting the seam for that takes good ore off the board for everybody, which is how a board
                // of 1,135 seams turns into a population walking further every hour. Its own ledger is where
                // the second one goes: another miner, or this one in an hour, is welcome to it.
                if (_ranDry >= _missed)
                {
                    // Worked out, so it rests. See BotGround.Drained: without this the same hole goes back on
                    // the board and the next miner walks to it, which cost 225 errands and the shard's
                    // finishing rate in one afternoon.
                    BotGround.Drained(_seam.Where);

                    // A seam that gave out under the pick after giving something is the end of a trip, not a failed one.
                    // See the worn-out pickaxe above, and build 193.
                    return Made > 0
                        ? BotDoing.Done($"worked the seam out: emptied {_ranDry} of {_spent.Count} rocks, and it rests")
                        : BotDoing.Failed($"emptied {_ranDry} of {_spent.Count} rocks and found no more, and the seam rests");
                }

                bot?.Resolve?.Ledger?.Beware(Trade, _map, _seam.Where);

                // <b>With the miner's own skill, because this line once meant a green miner and did not on build 42.</b>
                // Ulla and Hale, Mining 77 and 78 by the door, finished every trip until 23:57 on 14.09.2026 and then
                // failed fourteen in a row this way, iron included, where the engine's formula gives them three swings
                // in four; the restart ended it, and nothing left in the log could say what their skill had been.
                return BotDoing.Failed(
                    $"missed too often on {_missed} of {_spent.Count} rocks that still hold ore, at Mining {body.Skills[SkillName.Mining].Value:F1} (base {body.Skills[SkillName.Mining].Base:F1}); the seam keeps its place"
                );
            }

            // Nothing was ever found here, which is a fact about the ground rather than about this bot's
            // afternoon — so it is written where every miner reads it. See BotGround.Barren.
            //
            // <b>And it says which of the two happened, as the walls further down do.</b> Quill failed here six
            // times in a second at 23:00 on 14.09.2026, every line claiming to strike the seam off when only the
            // first had: the other five were the proposer's cached answer handing the struck seam back — see
            // BotGround.Untell — and the one sentence for both hid it.
            //
            // <b>And from how far off, which is a question this line could not answer.</b> The search looks outwards
            // from the bot and is leashed to the seam, while the walk above stops as soon as the bot is within
            // BotOre.Reach of it — so a bot at the edge of that reach searches only the side of the seam it is
            // standing on. Whether that is what struck 44 seams off in build 41's first half hour is what this
            // number is for; an instrument before a fix.
            var off = System.Math.Max(System.Math.Abs(body.X - _seam.Where.X), System.Math.Abs(body.Y - _seam.Where.Y));

            // <b>Rock that is there and empty is a seam worked out, not a seam that never was.</b> "No rock worth swinging
            // at, and the seam is struck off" was the commonest mining failure on build 47, 21 by 11 miners, most of them
            // in the half hour after the boot, all with nought swings. Find answers null both when there is no rock in
            // reach and when every rock's block the engine has emptied — twenty miners share the island's seams — and the
            // second case struck the seam off the board for the rest of the session although the engine refills a block in
            // ten to twenty minutes. Rested instead, as a worked-out seam is; the line now says which it was and where.
            if (BotOre.LastRocks > 0 && BotOre.LastEmpty >= BotOre.LastRocks)
            {
                BotGround.Drained(_seam.Where);
                WorkedOut++;

                return BotDoing.Failed(
                    $"every one of the {BotOre.LastRocks} rocks in reach of the seam at ({_seam.Where.X}, {_seam.Where.Y}) is worked out, and the seam rests (looked from {off} tiles off it)"
                );
            }

            return BotDoing.Failed(
                BotGround.Barren(_seam.Where)
                    ? $"no rock worth swinging at, and the seam at ({_seam.Where.X}, {_seam.Where.Y}) is struck off (looked from {off} tiles off it, {BotOre.LastRocks} rocks seen)"
                    : $"no rock worth swinging at; somebody had already struck the seam off (looked from {off} tiles off it)"
            );
        }

        var at = new Point3D(_tile.X, _tile.Y, _tile.Z);

        if (!body.InRange(at, BotOre.SwingReach))
        {
            // <b>A rock that cannot be walked to is a rock, not an errand.</b> Asked plainly, this leg answers
            // "still walking" for ever: the journey gives the route up, the work asks for the same walk on the
            // next beat, and the pair of them will do that until the world reloads — two gatherers stood four
            // minutes each on "digging ShadowIronOre (0 swings)" that way, which is what it looks like from a
            // client. The tile is written off exactly as a depleted one is, for the same reason: unreachable
            // and empty are different facts about a rock and identical facts about this afternoon.
            // <b>To a tile the rock can be swung at from, and not to the rock (29.09.2026, build 319).</b> The rock is the
            // mountain itself, its height the mountain's; the walk to it was asked within a tile and sixteen units of that
            // height, the ground under the cliff lay twenty and more below, and the far side answered "nowhere there to
            // stand (up to the rock)" 15 times in ten minutes of build 318 — Dorin five of them, then "no way through". The
            // engine's reach is flat (HarvestSystem.CheckRange, InRange by x and y), so any floor within it will do.
            // <b>Chosen once per rock, from where the miner stands, at its own height (build 323).</b> Ranked by ring and by
            // distance from the seam alone, 322's first choice was often the mountain's top — a floor the engine lets a body
            // stand on and no road reaches from below — and "3 rocks of the BronzeOre could not be reached" rose from none
            // on 318 to nine. Asked once when the rock is taken, so the walk order holds while the miner moves.
            if (_standRock != at)
            {
                _standRock = at;
                _standAt = Stand(_map, at, BotOre.SwingReach, body.Location, out _standRing, true);
            }

            var rockStand = _standAt;
            var ring = _standRing;

            if (rockStand == Point3D.Zero)
            {
                Standless++;
                _approaches = ApproachLimit;
            }
            else if (++_approaches < ApproachLimit)
            {
                return BotDoing.Walk(_map, rockStand, BotArrival.Within(System.Math.Max(0, BotOre.SwingReach - ring)), "up to the rock");
            }

            _spent.Add(at);
            _tile = null;
            _approaches = 0;
            Unreachable++;

            // <b>Three rocks in a row that cannot be walked to are a fact about the seam, not about the
            // rocks.</b> Written off one at a time, this costs ApproachLimit beats each and MaxSpent of them
            // before the seam is given up — which on 03.09.2026 at 06:17 was Godric the Architect and Alden
            // the Gatherer standing three minutes apiece at 1361,1559, fifteen tiles from a seam at
            // 1361,1574, treading a patch two tiles across and changing tile 49 times inside it. Calla the
            // Crafter had spent ten minutes at the same coordinates six hours earlier. The seam is behind
            // something, and the way to find that out is not to try eight rocks behind the same something.
            if (++_walled >= WalledLimit)
            {
                // <b>Says which of the two happened, because they read as one and are not.</b> Barren answers
                // false when somebody else has already taken the seam off the list, and on 03.09.2026 that
                // was most of them: 151 of these lines in 37 minutes against 15 seams actually removed, on a
                // list of 502. A message that claims the removal every time turns a healthy rate into what
                // looks like the ore map being stripped, and it cost a good ten minutes of exactly that
                // worry before the ground's own tally settled it.
                var struck = BotGround.Barren(_seam.Where);

                if (struck)
                {
                    Unwalkable++;
                }

                return BotDoing.Failed(
                    struck
                        ? $"{_walled} rocks of the {_seam.Ore} could not be reached, and the seam is struck off"
                        : $"{_walled} rocks of the {_seam.Ore} could not be reached; somebody had already struck the seam off"
                );
            }

            return default;
        }

        _approaches = 0;
        _walled = 0;

        // <b>What the engine actually said about the last swing, which is the only thing here that is not an
        // inference.</b> Patrick's order of 09.09.2026: a vein is worked out when the bot sees the message
        // that there is nothing in it. The harvest system distinguishes six outcomes and says which — see
        // BotHeard — and until the engine was given a seam to say it through, every one of those sentences
        // was dropped before anything could hear it.
        var word = BotHeard.Last(body, Server.Engines.Harvest.Mining.System?.OreAndStone, out _);

        switch (word)
        {
            case BotHeard.Word.Empty:
            case BotHeard.Word.Taken:
                {
                    // The rock is done, in the engine's own words. Nothing to weigh up.
                    BotHeard.Clear(body);
                    Drained++;
                    _ranDry++;
                    _spent.Add(at);
                    _tile = null;
                    _dry = 0;

                    return default;
                }

            case BotHeard.Word.Full:
                {
                    // The ore came out of the bank and was destroyed for want of room. Carrying on here costs
                    // the vein and pays nobody.
                    BotHeard.Clear(body);
                    Laden++;
                    _dry = 0;
                    _leg = BotOre.Carried(body) >= BotOre.WorthSmelting ? Leg.Fire : Leg.Counter;

                    return default;
                }

            case BotHeard.Word.Adrift:
                {
                    // The swing was cancelled rather than rolled, because the bot moved while it resolved.
                    // Not the rock's fault and not a miss, so it does not spend the rock's patience.
                    BotHeard.Clear(body);
                    Adrift++;
                    _dry = _dry > 0 ? _dry - 1 : 0;

                    break;
                }

            case BotHeard.Word.Broken:
                {
                    BotHeard.Clear(body);

                    // <b>The next pickaxe, if the pack holds one, and the trip goes on.</b> Since build 213 the next one is
                    // bought while the last still works and the most worn is swung first (BotOutfit.SpareAt), so the end of
                    // a pickaxe is usually the start of the next: ending the trip here threw that away — "failed at mine:
                    // the pickaxe wore out" six times in forty minutes on 24.09.2026, each with a new one in the pack.
                    if (BotOre.Tool(body) != null)
                    {
                        NextPickaxe++;

                        break;
                    }

                    // Ore or metal already in the pack is the trip's takings; the next trip smelts what is left.
                    return Made > 0
                        ? BotDoing.Done($"the pickaxe wore out, {_raw} ore still to smelt")
                        : BotDoing.Failed("the pickaxe wore out");
                }
        }

        // And the bank as it stands, which agrees with the sentence above almost always and is the guard for
        // the almost: a rock whose bank is empty is empty whether or not anybody was told.
        if (_swung && BotOre.Left(_map, at.X, at.Y) <= 0)
        {
            Drained++;
            _ranDry++;
            _spent.Add(at);
            _tile = null;
            _dry = 0;

            return default;
        }

        // Held while it is actually being worked, and renewed each beat: a claim that is not being used
        // lapses by itself, which is what keeps it from fencing off rock nobody is digging.
        BotGround.Working(body, _seam.Where);

        // <b>A swing a beat is not mining, it is a pickaxe on a trigger.</b> Sewing and writing have had a
        // rest between attempts since they were written — three seconds each — and digging never got one, so
        // it ran at whatever rate the population's clock happened to offer, two to five times a second. Seen
        // from a client it is unmistakable and it is not what a bot doing a day's work looks like. The engine
        // does not mind, which is exactly why nothing complained.
        //
        // <b>And everything that judges a swing now sits below this line, which is the whole of the fault
        // this file was opened for.</b> The dryness counter was above it, so it counted <em>beats</em> and
        // called them swings: the population beats several times a second and a swing is taken once every
        // SwingMs, so six quiet beats is one or two actual attempts. A rock was written off having barely
        // been struck, the seam was rested behind it, and the shard reported an island running out of ore.
        // Counted apart for the first time on 09.09.2026: 16 rocks given up with the engine's bank under
        // them empty against 35 that still held ore. Two in three.
        if (_swung && Core.TickCount - _swungTick < SwingMs)
        {
            return BotDoing.Work($"digging {_seam.Ore}");
        }

        // How many quiet swings mean anything, for this bot at this rock. See DryLimit.
        var patience = System.Math.Clamp(
            (int)System.Math.Ceiling(DryLimit / System.Math.Max(0.05, BotOre.Chance(body, _map, at.X, at.Y))),
            DryLimit,
            MostDry
        );

        // <b>What the last swing produced, judged now that a swing is actually due — and this is the only
        // place on the shard where that can be seen at all.</b> <c>StartHarvesting</c> does not hand the ore
        // over: it starts a <c>HarvestTimer</c>, whose last tick starts a <c>HarvestSoundTimer</c>, and it is
        // that one — <c>EffectSoundDelay</c>, nine tenths of a second later — which calls <c>Harvest</c> and
        // puts the ore in the pack. So reading the pack on the line after the swing reads the pack as it was
        // before it. Two things were doing exactly that: this file's credit to <c>BotQuad.Harvested</c>, so
        // the peril map has never once been told that a piece of ground pays, and my own first attempt at
        // pricing the ore below. Both are here now, a beat later, where the ore has actually arrived.
        if (carried > _seen)
        {
            var raised = carried - _seen;

            _seen = carried;
            _dry = 0;

            // Ground that actually paid, rather than ground a bot stood on and swung at.
            BotQuad.Harvested(_map, body.Location);

            // Priced once, the first time this trip produces anything, and at what the market says ore is
            // fetching rather than at a number chosen here. Half a bar is the fallback because the engine
            // wants about two ore to a bar. See Made.
            if (_rawWorth <= 0)
            {
                _rawWorth = System.Math.Max(
                    1,
                    BotMarket.Worth(typeof(IronOre), System.Math.Max(1, GoldPerIngot / 2))
                );
            }

            _raw += raised;
        }
        else if (_swung && ++_dry >= patience)
        {
            // Emptied, or it never held anything. It has to be written down, not merely dropped: a depleted
            // vein looks exactly like a full one from outside — depletion lives in the engine's own bank of
            // resources, while what a bot can read is the tile's definition — so looking again without
            // remembering hands the bot the same rock for the rest of its life.
            // Which of the two it was, asked of the engine at the moment of giving up. It is one lookup on a
            // dictionary and it is the whole difference between a worked-out island and a population of
            // miners too green for the rock in front of them.
            if (BotOre.Left(_map, at.X, at.Y) > 0)
            {
                Fumbled++;
                _missed++;

                // What the engine actually gave this bot at this rock, kept so the average can be read. See
                // FumbledChance.
                FumbledChance += BotOre.Chance(body, _map, at.X, at.Y);
            }
            else
            {
                Drained++;
                _ranDry++;
            }

            _spent.Add(at);
            _tile = null;
            _dry = 0;

            return default;
        }

        _swung = true;
        _swungTick = Core.TickCount;
        _swings++;

        // Whether the engine will even look at this swing. See Locked: the pickaxe carries the lock and it is
        // handed back by a timer, so a lost timer means every swing after it is a silent no-op.
        if (!body.CanBeginAction(tool))
        {
            Locked++;
        }

        // And whether the last swing had a chance of landing at all: it resolves nine tenths of a second
        // after it is taken, at a range checked again then. See Stirred.
        if (_swung && _dry > 0 && body.Location != _swungFrom)
        {
            Stirred++;
        }

        _swungFrom = body.Location;

        // Nothing is read back off the pack here: the ore is nine tenths of a second away and this line would
        // read the pack as it was before the swing. Everything that judges a swing is a beat above.
        // <b>Off the horse first, because the engine will not dig from a saddle and says so to nobody.</b> Hale bought a
        // horse at 23:59:13 on 14.09.2026 and Ulla at 00:02:41; from 00:02:33 and 00:09:17 every trip either took failed
        // "missed too often on 8 of 8 rocks that still hold ore", fourteen in a row, iron included, until the restart
        // took the horses away — and Kerrin, Hale and Lysa had done the same on the two builds before. BotDeed.Afoot
        // keeps the horse from being called straight back up.
        if (body.Mounted)
        {
            BotStable.Alight(body);
        }

        BotOre.Swing(body, tool, _system, _tile);

        // <b>What came out of this hillside, written down for everybody.</b> The seam list already says what
        // a vein asks of a miner; this says what it has actually paid, which is the fact a miner would want
        // and the one nobody kept. Recorded against the patch rather than the tile, so a mountainside is one
        // place and not two hundred, and shared, because where the bronze is is not a private opinion.
        Note(body, _seam.Where);

        return BotDoing.Work($"digging {_seam.Ore}");
    }

    private BotDoing Melting(Mobile body)
    {
        // Too little to be worth a fire. Two rocks smelt to nothing — the engine wants more ore than that per
        // ingot — and a bot arrives here with two whenever its pack filled up with something else, which now
        // happens routinely because hunters carry loot. Straight to the counter with what it has.
        if (BotOre.Carried(body) < BotOre.WorthSmelting)
        {
            _leg = Leg.Counter;

            return default;
        }

        if (BotOre.Carried(body) <= 0)
        {
            // Nothing to melt. Whatever metal is in hand still wants putting away.
            _leg = Leg.Counter;

            return default;
        }

        if (_fire == Point3D.Zero)
        {
            _fire = BotGround.Fire(_bot, body.Location);

            // <b>A colonist with no fire on its island puts the ore into the colony's store as it is</b> (07.10.2026, sea20): the
            // nearest fire known may lie across the sea, and the run home melts it on the mainland. See BotColonies.StoreOf.
            if ((_fire == Point3D.Zero || !BotGates.Joined(_map, body.Location, _fire)) && BotColonies.StoreOf(body) != null)
            {
                _fire = Point3D.Zero;
                _leg = Leg.Counter;

                return default;
            }

            if (_fire == Point3D.Zero)
            {
                return BotDoing.Failed("nowhere known to melt it");
            }
        }

        if (_closer ? !body.InRange(_fire, 1) : !body.InRange(_fire, BotOre.FireReach))
        {
            // The distance the work itself asks for, on the line above. See BotArrival.Beside. Beside the fire after a
            // refusal from further off; see Spurned.
            return _closer
                ? BotDoing.Walk(_map, _fire, BotArrival.Beside, "closer to the fire")
                : BotDoing.Walk(_map, _fire, BotArrival.Within(BotOre.FireReach), "to a fire");
        }

        var before = BotOre.Carried(body);

        Bars(body, _barsBefore);

        var made = BotOre.Melt(body);

        // <b>The engine took none of it: the forge refused the bot, and nothing burned.</b> See Spurned. Once, the bot
        // steps up beside the fire and tries again — unless the fire stands in a house the bot is outside of, which no
        // step on this side of the wall mends, or a miner lately stepped up to it and was refused anyway. Otherwise the
        // forge is rested for everybody (BotGround.Unfit) and the trip goes on to the next fire.
        if (made <= 0 && BotOre.Carried(body) >= before)
        {
            Spurned++;

            var house = BaseHouse.FindHouseAt(_fire, _map, 16);
            var lately = _nearing.TryGetValue(_fire, out var when) && Core.TickCount - when < NearingMs;

            if (!_closer && !lately && (house == null || BaseHouse.FindHouseAt(body) == house))
            {
                _closer = true;
                _nearing[_fire] = Core.TickCount;
                Nearer++;

                return BotDoing.Walk(_map, _fire, BotArrival.Beside, "closer to the fire");
            }

            BotGround.Unfit(_fire);
            Forsaken++;

            _fire = Point3D.Zero;
            _closer = false;

            if (++_refusals >= MostRefusals)
            {
                _leg = Leg.Counter;
            }

            return default;
        }

        if (made > 0 && _closer)
        {
            _nearing.Remove(_fire);
        }

        if (made <= 0)
        {
            // The ore went in and nothing came out: the smelt is a skill roll, and a poor one burns the rock.
            //
            // <b>Finished rather than failed, and the distinction is not bookkeeping.</b> A failure marks the
            // <em>place</em> with caution, and the place this undertaking is filed under is the seam — so a
            // bad roll at a forge in town would teach the bot that a perfectly good vein two hundred tiles
            // away is not worth digging. The trip really did happen and really did earn its mining checks;
            // what it did not do is produce metal.
            // It really did burn: the ore is gone and no bar came of it, so the trip produced nothing after
            // all. Anything else would credit a bad roll with what it destroyed.
            _raw = 0;

            _leg = Leg.Counter;

            return BotDoing.Done($"the ore burned away, {_swings} swings");
        }

        // What the shard says a bar is worth, not what this file guesses. A funded want for metal is what
        // makes a mining trip worth more than it was yesterday, and this is the line the signal comes in
        // through: the takings go into the ledger, and the ledger raises its estimate of digging here next
        // time. Iron stands for the batch because iron is what an ordinary seam gives and what a population
        // of smiths will be asking for; a coloured vein prices its own bars at the counter below.
        _worth = BotMarket.Worth(typeof(IronIngot), GoldPerIngot);

        // <b>Each metal the fire gave, by its own kind, at the island's price for what the smiths ask for and the surplus
        // price for the rest (01.10.2026, Patrick's point 8).</b> On build 384 the shops showed 945 ingots of seven metals —
        // 292 copper, 166 dull copper, 109 agapite, 20 gold, 9 valorite — that no smith on the island had the base to work,
        // and every bar dug was credited at iron's price, so digging a coloured vein for a shelf paid the miner exactly as
        // digging for a forge did. A smith that reaches the metal asks for it (BotBullion) and the price comes back. Bars the
        // snapshot cannot attribute are counted as iron, as before. See BotGlut.Yield.
        Bars(body, _barsAfter);

        var booked = 0;

        foreach (var (metal, held) in _barsAfter)
        {
            var gained = held - _barsBefore.GetValueOrDefault(metal);

            if (gained <= 0)
            {
                continue;
            }

            gained = System.Math.Min(gained, made - booked);

            if (gained <= 0)
            {
                break;
            }

            BotGlut.Made(metal, gained);
            BotOre.Smelted(metal, gained);
            _made += BotGlut.Yield(metal, gained, GoldPerIngot);
            booked += gained;
        }

        if (booked < made)
        {
            BotGlut.Made(typeof(IronIngot), made - booked);
            BotOre.Smelted(typeof(IronIngot), made - booked);
            _made += BotGlut.Yield(typeof(IronIngot), made - booked, GoldPerIngot);
        }

        // The ore is in the fire and the bars are the produce now. Cleared rather than left, or the trip
        // would be credited with the same metal twice. See Made.
        _raw = 0;

        _leg = Leg.Counter;

        return default;
    }

    private BotDoing Banking(IBotWilful bot)
    {
        var body = bot.Self;

        // <b>A colonist's metal goes into its colony's store, not its own bank</b> (07.10.2026, sea20): the colony is the guild's
        // mine, and its ship carries the store home. See BotColonies.StoreOf and BotColonyRun.
        if (BotColonies.StoreOf(body) is { } store)
        {
            var at = store.GetWorldLocation();

            if (!body.InRange(at, 2))
            {
                return BotDoing.Walk(_map, at, BotArrival.Within(1), "to the colony's store");
            }

            var (bars, ore, _) = BotColonies.Deposit(body, store);

            _stored = bars;

            return BotDoing.Done($"{bars} ingots and {ore} ore into the colony's store, {_swings} swings");
        }

        if (_counter == Point3D.Zero)
        {
            _counter = BotGround.Counter(bot, body.Location);

            if (_counter == Point3D.Zero)
            {
                return BotDoing.Failed("nowhere known to put it away");
            }
        }

        if (!body.InRange(_counter, CounterReach))
        {
            // The distance the work itself asks for, on the line above. See BotArrival.Beside.
            return BotDoing.Walk(_map, _counter, BotArrival.Within(CounterReach), "to a counter");
        }

        var (stored, ordered) = Store(bot);

        _stored = stored;

        // What a want waits for rides in the pack to it (BotCourier) and is paid for there, under the delivery's own name; the
        // bars are counted as made here, once, as every trade counts what it makes (01.10.2026: a fill used to pay the coin now).
        return BotDoing.Done($"{_stored} ingots away and {ordered} kept for somebody's want, {_swings} swings");
    }

    /// <summary>
    /// The best metal in this pack, remembered against the ground it came out of.
    ///
    /// Read off the pack rather than off the swing, because the engine hands ore over on its own schedule and
    /// a swing that produced nothing looks exactly like one that has not landed yet. The best of what is
    /// carried is the honest summary of what this trip has been finding.
    /// </summary>
    private void Note(Mobile body, Point3D where)
    {
        var pack = body?.Backpack;

        if (pack == null)
        {
            return;
        }

        var best = CraftResource.Iron;
        var found = false;

        for (var i = 0; i < pack.Items.Count; i++)
        {
            if (pack.Items[i] is not BaseOre ore)
            {
                continue;
            }

            found = true;

            if (ore.Resource > best)
            {
                best = ore.Resource;
            }
        }

        if (found)
        {
            BotCommons.Dug(_map, where, best);
        }
    }

    /// <summary>Over, however it ended. The seam goes back to whoever wants it.</summary>
    public override void Drop(IBotWilful bot) => BotGround.Leave(_seam.Where);

    /// <summary>
    /// Somewhere else, when the way to where it was going turned out not to exist. Only the seam can be
    /// swapped: a fire or a counter is picked as the nearest known one, so choosing again would choose the
    /// same one and the bot would walk into the same wall. Failing there is the honest answer, and the
    /// ledger will keep the bot off that trip for a while.
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        if (++_bends > MaxBends)
        {
            return false;
        }

        var body = bot?.Self;

        // The forge could not be reached. Ask for a different one rather than giving up: there are several on
        // this shard, and a miner that cannot reach the nearest is holding a pack of rock it can do nothing
        // with. Left as it was, this loops — the undertaking fails, the proposer offers mining again because
        // the bot is still carrying ore, and the same unreachable forge is chosen. Measured at three failures
        // a second, for as long as nobody looked.
        if (_leg == Leg.Fire)
        {
            // Written down before another is asked for, or the bot simply alternates between two forges it
            // cannot reach — which is what it did. In its own ledger, not a shared list: another bot standing
            // on the other bank can reach this forge perfectly well.
            bot?.Resolve?.Ledger?.Beware(BotGround.FireKind, _map, _fire);

            var fire = BotGround.Fire(bot, body?.Location ?? _seam.Where, _fire);

            if (fire == Point3D.Zero)
            {
                return false;
            }

            _fire = fire;

            return true;
        }

        // And the same for the counter, for the same reason: the metal is made and the bot cannot put it
        // anywhere.
        if (_leg == Leg.Counter)
        {
            bot?.Resolve?.Ledger?.Beware(BotGround.CounterKind, _map, _counter);

            var counter = BotGround.Counter(bot, body?.Location ?? _seam.Where, _counter);

            if (counter == Point3D.Zero)
            {
                return false;
            }

            _counter = counter;

            return true;
        }

        // <b>Written down, exactly as an unreachable forge is, and for want of this one line a gatherer spent
        // a whole session on one rock.</b> The exception below only holds for this bend: once the undertaking
        // gives up, the miner proposer scores the seams again from scratch, the same unreachable one is still
        // the nearest, and it is offered again within seconds. Calla asked for (1316, 1544, 20) thirty-four
        // times on the evening of 23.08.2026, dug nothing, and supplied nobody. The ledger is where "I could
        // not get there" has to live, or the next decision cannot know it.
        bot?.Resolve?.Ledger?.Beware(Trade, _map, _seam.Where);

        var other = BotGround.Seam(bot, _seam.Where);

        if (!other.Exists)
        {
            return false;
        }

        _seam = other;
        _tile = null;
        _dry = 0;

        // Another seam is another set of rocks, so the allowance starts again.
        _spent.Clear();

        return true;
    }

    /// <summary>Whether the pack is full enough that the next step starts costing stamina.</summary>
    private static bool Loaded(Mobile body) =>
        BotLadder.Load(body) >= BotLadder.Ceiling(body) * FillFraction;

    /// <summary>
    /// Whether a mining trip would end the moment it began: the pack is at the load that sends a digger to
    /// the fire, and there is no ore in it worth a fire.
    ///
    /// <para>
    /// <b>For the proposer, which was offering seams to a bot with nowhere to put the ore.</b> Ilsa the
    /// scribe, carrying reagents and paper up to her ceiling, took a mine 29 times in half an hour on
    /// 14.09.2026 and finished every one inside the same second: <c>Digging</c> saw the load and left for
    /// the fire, <c>Melting</c> found nothing to melt and left for the counter, and the counter was where she
    /// stood — "putting the metal away: 0 in 0.2 min, 0 swings". A finish, so nothing learned; re-offered
    /// at the next review. The offer should not exist.
    /// </para>
    /// </summary>
    public static bool Burdened(Mobile body) =>
        body != null && Loaded(body) && BotOre.Carried(body) < BotOre.WorthSmelting;

    /// <summary>
    /// Metal a want within a carrier's reach waits for stays in the pack for the walk to it; the rest goes into the box; coin into the
    /// account. Says how much metal was put away and how much was kept for a want. (Until 01.10.2026: a want filled from the
    /// counter, then a stall, then the box.)
    ///
    /// <para>
    /// <b>The coin is taken out of the pack before it is banked</b>, and that order is not a style choice:
    /// the engine's deposit adds to the account without touching what the depositor is carrying, so doing it
    /// the other way round mints money. If the deposit is refused the coin is handed straight back rather
    /// than destroyed.
    /// </para>
    /// </summary>
    private static (int Stored, int Ordered) Store(IBotWilful bot)
    {
        var body = bot.Self;
        var pack = body.Backpack;

        if (pack == null)
        {
            return (0, 0);
        }

        var box = body.BankBox;
        var stored = 0;
        var ordered = 0;
        Dictionary<System.Type, int> held = [];

        // A snapshot: moving things out mutates the list being read.
        List<Item> carried = [.. pack.Items];

        for (var i = 0; i < carried.Count; i++)
        {
            if (carried[i] is not BaseIngot ingot || ingot.Deleted || !ingot.Movable)
            {
                continue;
            }

            var amount = ingot.Amount;
            var kind = ingot.GetType();

            // Somebody near has asked for it: as many bars as the island asks for ride in the pack to them (BotCourier), the rest
            // go into the box.
            var hold = BotMarket.Holding(bot, kind, amount, held);

            if (hold > 0)
            {
                ordered += hold;

                if (hold >= amount)
                {
                    continue;
                }
            }

            if (box == null)
            {
                continue;
            }

            // The engine's split: the bar stack keeps what goes to the box and the kept bars are put back beside it in the pack.
            if (hold > 0 && Mobile.LiftItemDupe(ingot, amount - hold) == null)
            {
                continue;
            }

            stored += ingot.Amount;

            // <b>Onto the box's stack of the metal, not beside it (06.10.2026, box16).</b> DropItem neither joins a stack nor asks
            // the box's count, so every trip laid a new stack of every metal: thirty-five Gatherers' boxes held 132 to 558 things
            // at 05:36 on 06.10.2026, and a full box refuses the porter everything. A new place only for a metal it has none of.
            // See BotSurplus.
            BotSurplus.Put(body, box, ingot);
        }

        // And the stacks the old way laid apart, once in a while. See BotSurplus.TidyIfFull.
        BotSurplus.TidyIfFull(body);

        var purse = pack.GetAmount(typeof(Gold));

        if (purse > 0 && pack.ConsumeTotal(typeof(Gold), purse) && !Banker.Deposit(body, purse))
        {
            pack.DropItem(new Gold(purse));
        }

        return (stored, ordered);
    }
}
