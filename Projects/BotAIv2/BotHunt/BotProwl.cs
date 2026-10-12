using System.Collections.Generic;
using Server.Regions;
using Server.Logging;

namespace Server.BotAI.V2;

/// <summary>
/// Going to look for a fight, when there is nothing to fight where the bot is standing.
///
/// <para>
/// <b>Without this a fighter is only as good as where somebody put it.</b> Hunting begins when something
/// hostile is within sight, and a town forbids fighting outright — so a population raised in a city has
/// warriors that stand about for ever, and a population raised at a graveyard hunts only that graveyard until
/// it is empty. Neither is a bot deciding anything; both are a bot being placed well or badly.
/// </para>
///
/// <para>
/// <b>It is priced at almost nothing on purpose.</b> Walking somewhere produces no coin, no goods and no
/// skill, so it wins only when the auction has nothing else to offer at all — which is exactly when a bot
/// should be out looking rather than standing still. The moment anything worth fighting comes into reach, the
/// hunt itself scores an order of magnitude higher and takes over at the next review.
/// </para>
///
/// <para>
/// And it ends itself the instant it has worked: a bot that walks into sight of a quarry does not finish the
/// walk first. "Stand and complete the errand" is the shape of bug this project keeps finding in itself.
/// </para>
///
/// <para>
/// <b>A sweep of named squares, not a look round where the bot stands (06.10.2026, build next).</b> Patrick, 06.10 ~10:00:
/// "Ideally, when they go on a prowl they are given several quadrants to clear; if they walked them and found nobody, that is
/// fine, that is a success." On build 400 (06.10, 06:12–09:42) 1761 of the 2669 prowls that ended said "nothing here" after a
/// walk to one point a median of 0.6 minutes long, and each counted as finished work. A prowl is now a route over two to four
/// squares of <see cref="BotQuad.Side"/> tiles (<c>BotHunter.Sweep</c>), each entered and crossed — its waypoint reached —
/// before the next. A fight found on the way ends this sweep as finished ("something worth fighting", with the square), the
/// hunt is fought by the hunter's own rules, and the squares left are offered again afterwards (<see cref="BotSweepRecord.Left"/>).
/// "Found nothing" is finished only when every square was walked; a sweep that could not reach its squares fails with the
/// reason. Its price is what sweeps of its squares paid lately (<see cref="BotSweepRecord.Worth"/>), not a flat number.
/// </para>
/// </summary>
public sealed class BotProwl : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotProwl));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "prowl";

    /// <summary>Whether a prowl is a sweep of several squares priced by what sweeps of them paid. Off: one point at <see cref="Prior"/>.</summary>
    public static bool Sweeps { get; set; } = true;

    /// <summary>The fewest and the most squares one sweep is given. See <c>BotHunter.Sweep</c>.</summary>
    public static int SweepLeast { get; set; } = 2;

    public static int SweepMost { get; set; } = 4;

    /// <summary>How long one square of a sweep is reckoned to take, walk and look: a square is thirty tiles across.</summary>
    public static double MinutesPerSquare { get; set; } = 1.0;

    /// <summary>Sweeps offered by the hunter, and the squares they named; offered with one square only because no other passed.</summary>
    public static long SweepsOffered { get; internal set; }

    public static long SquaresOffered { get; internal set; }

    public static long ShortSweeps { get; internal set; }

    /// <summary>
    /// Squares looked at for a sweep and refused for no way (07.10.2026, work18): a sweep's walk to them failed within the hour, or
    /// the reach map calls them an island or a pocket; and prowls not offered because their first square was such. See BotHunter.NoWay.
    /// </summary>
    public static long SquaresNoWay { get; internal set; }

    public static long SquaresNoGround { get; internal set; }

    public static long FirstNoWay { get; internal set; }

    /// <summary>Squares passed over at the offer for being the one the bot stands in, or one it lately swept and found empty.</summary>
    public static long Underfoot { get; internal set; }

    /// <summary>Sweeps offered again after a fight took the bot off them, with the squares they had left.</summary>
    public static long ResumeOffered { get; internal set; }

    /// <summary>Sweeps taken, of them taken up again after a fight, squares walked to their waypoint, and fights found on the way.</summary>
    public static long SweepsTaken { get; private set; }

    public static long ResumedTaken { get; private set; }

    public static long SquaresWalked { get; private set; }

    public static long FightsFound { get; private set; }

    /// <summary>How sweeps ended: every square walked and nothing found; a fight found; a raised company's fights done.</summary>
    public static long EndedEmpty { get; private set; }

    public static long EndedFight { get; private set; }

    public static long EndedCompany { get; private set; }

    /// <summary>
    /// Sweeps failed, by cause: could not reach a square (no nearer, no way through), could not raise the company its ground
    /// asked, the company came apart or withdrew, the ground was closed, and ended by the will (turned back by what it saw,
    /// outbid, put down, the stall watch) or by death.
    /// </summary>
    public static long FailedUnreached { get; private set; }

    public static long FailedUnraised { get; private set; }

    public static long FailedCompany { get; private set; }

    public static long FailedClosed { get; private set; }

    public static long EndedByWill { get; private set; }

    public static long EndedDead { get; private set; }

    /// <summary>Sweeps that have ended, and the minutes they took between them.</summary>
    public static long SweepsEnded { get; private set; }

    public static double SweepMinutes { get; private set; }

    /// <summary>
    /// What looking for a fight is reckoned at per minute. Below every trade on the shard, and above nothing,
    /// which is the whole of its place in the order.
    /// </summary>
    public static double Prior { get; set; } = 8.0;

    public static double WorkMinutes { get; set; } = 2.0;

    /// <summary>
    /// How near the ground a bot has to get before the look counts as taken.
    ///
    /// <para>
    /// <b>Its own number, and sharing one with the search radius made this errand a no-op by
    /// construction.</b> Arrival used to be <see cref="BotQuarry.Reach"/> — the same fifty tiles that
    /// <see cref="BotHunter"/> requires a candidate to be <em>outside</em> of before it may be offered at
    /// all. So a prowl was legal only beyond the line and finished the moment the bot came back inside it:
    /// the whole errand was crossing one boundary, which is one step from where it was handed out. The
    /// evening of 24.08.2026 has a hundred and twenty-three of them, every one paying nothing, ninety
    /// lasting twelve seconds and several lasting one — a population that read as busy hunting and was
    /// flickering on the spot.
    /// </para>
    ///
    /// <para>
    /// Small, because the point of walking somewhere is to be there. Nothing is wasted by going the whole
    /// way: the bot searches its full reach every beat as it walks, and the check above ends the errand the
    /// instant anything worth fighting appears — so the journey <em>is</em> the search, and only its end is
    /// being defined here.
    /// </para>
    /// </summary>
    public static int ArriveWithin { get; set; } = 8;

    /// <summary>
    /// How many beats the walk may go without getting any nearer before the ground is given up on.
    ///
    /// <para>
    /// <b>Because <see cref="Bend"/> only fires when the road refuses, and a road that never refuses is the
    /// commoner failure.</b> A walk that is simply never completed says nothing to anybody: on the night of
    /// 02-03.09.2026 the roll-call found Edda the Brawler holding a prowl for 250 seconds at 1651,1112 and
    /// Kerrin the Warrior for 324 seconds at 1296,1560, neither arriving, neither finishing, neither leaving
    /// a patch two tiles across — and the only thing that noticed was the debugger, four minutes late. The
    /// same silence and the same cure as the long walk to a seam in <c>BotDig</c>: measure the closing of the
    /// distance rather than the attempts, because the walk is meant to be long and it is stopping to get
    /// closer that is the fault.
    /// </para>
    ///
    /// <para>
    /// Two hundred beats, about forty seconds at a turn every two hundred milliseconds, and the same number
    /// the seam uses for the same reason.
    /// </para>
    /// </summary>
    public static int TrekLimit { get; set; } = 200;

    /// <summary>Prowls given up because the bot stopped getting nearer. See <see cref="TrekLimit"/>.</summary>
    public static long Baulked { get; private set; }

    /// <summary>
    /// Whether a step along the plan the bot is walking to its ground counts as getting nearer, as well as a step nearer as
    /// the crow flies.
    ///
    /// <para>
    /// <b>A way round is not a walk that stopped closing.</b> On build 99 (23:57–01:06, 16–17.09.2026) 49 of 1222 prowls
    /// failed "got no nearer than N tiles", 4% against 0.2–2.6% in every session of the day before; nearly half of them were
    /// bound west, for the ground round the brigands at (850, 1695) where bots had been dying that evening. The door's road from
    /// Alden Ashdown's (1092, 1644) to its dart at (991, 1686), 101 tiles off, is a plan of 573 tiles round the hills: two
    /// hundred beats of it walk no nearer as the crow flies, and the prowl gave up at 00:04:07 on a road it was walking. The
    /// tiles left on a plan fall with every step taken along it and do not fall for a bot standing still, which is what
    /// <see cref="TrekLimit"/> was written for (Kerrin, 324 seconds in a patch two tiles across). A bot planning afresh
    /// every few steps is the journey's own business (<c>BotJourney.Hopeless</c>); each new plan starts its own count.
    /// </para>
    /// </summary>
    public static bool ByRoad { get; set; } = true;

    /// <summary>Beats a prowl was kept going because it stepped along its plan while getting no nearer as the crow flies.</summary>
    public static long RoadKept { get; private set; }

    /// <summary>
    /// How many times a prowl that stopped getting nearer may look again for ground on its own side before it gives up.
    ///
    /// <para>
    /// <b>The largest failure on the shard was a walk standing at a riverbank for two hundred beats and then failing.</b>
    /// On build 43, 69 of 388 failures were "got no nearer", 33 bots, at the same few banks west and south of Britain:
    /// the darts land on trodden ground beyond a river whose way round no search is funded to find, the destination is
    /// baulked for everybody, and the bot that walked two hundred tiles to learn it goes home with nothing. What it is
    /// standing on is ground that was reached, and a fight within forty tiles of it on this side is exactly what the
    /// errand was for. <see cref="BotBarrier"/> tried to keep the darts off such places and could not — a river is a
    /// line and its stops are points — so this answers at the bank instead. Once, and never for a company, whose
    /// strength was weighed for the square it was raised for.
    /// </para>
    /// </summary>
    public static int Redarts { get; set; } = 1;

    /// <summary>How far round the tile it stopped on a prowl looks for new ground. See <see cref="Redarts"/>.</summary>
    public static int RedartReach { get; set; } = 40;

    /// <summary>Tiles sampled for one look, most dangerous first once they pass the free tests.</summary>
    public static int RedartSamples { get; set; } = 8;

    /// <summary>
    /// How many of them one look vets with a real search, at most. Two, because a path search is the shard's shared
    /// clock — <see cref="BotHunter"/> once spent all of it vetting prowl points per idle bot per review — and this runs
    /// only when a walk has already stalled.
    /// </summary>
    public static int RedartVets { get; set; } = 2;

    /// <summary>The clock one vetting search is given: the scout's. See <see cref="BotScoutmaster.VetMs"/>.</summary>
    public static double RedartVetMs { get; set; } = 300.0;

    /// <summary>Stalled prowls sent on to ground a search proved they could walk to. See <see cref="Redarts"/>.</summary>
    public static long Redarted { get; private set; }

    /// <summary>Stalled prowls that looked again and found nothing walkable, and so were given up.</summary>
    public static long Unredarted { get; private set; }

    /// <summary>Prowls whose road was refused on the way, sent on to ground on their own side a search proved walkable. See <see cref="Bend"/>.</summary>
    public static long Turned { get; private set; }

    /// <summary>Prowls whose road was refused on the way that looked again, found nothing walkable, and so were given up.</summary>
    public static long Unturned { get; private set; }

    private readonly Map _map;

    /// <summary>The ground being walked to. Moved once at most, by <see cref="Redart"/>.</summary>
    private Point3D _where;

    /// <summary>How many times this errand has looked again on its own side. See <see cref="Redarts"/>.</summary>
    private int _redarts;

    /// <summary>A look's candidates, reused.</summary>
    private static readonly List<Point3D> _candidates = [];

    /// <summary>The closest this bot has come, and how many beats since that last improved.</summary>
    private int _nearest = int.MaxValue;

    /// <summary>Which of the journey's plans the count below belongs to. See <see cref="Along"/>.</summary>
    private int _plans = -1;

    /// <summary>The fewest tiles left on that plan seen so far.</summary>
    private int _planLeast = int.MaxValue;

    /// <summary>
    /// Whether this beat found the bot further along the plan it is walking to this prowl's ground than any beat before on
    /// that plan. See <see cref="ByRoad"/>.
    /// </summary>
    private bool Along(Mobile body)
    {
        if (!ByRoad || body is not BotMobile { Journey: { Current: { } errand } journey } || errand.Follow != null
            || errand.Where.X != _where.X || errand.Where.Y != _where.Y || journey.Remaining <= 0)
        {
            return false;
        }

        if (journey.Plans != _plans)
        {
            _plans = journey.Plans;
            _planLeast = journey.Remaining;

            return false;
        }

        if (journey.Remaining >= _planLeast)
        {
            return false;
        }

        _planLeast = journey.Remaining;

        return true;
    }

    private int _stalled;

    /// <summary>Where the walk began, so that a walk that never got going is not written down as a barrier. See BotBarrier.</summary>
    private Point3D _setOut;

    /// <summary>Who is standing here to be raised, strongest first. Reused between raisings.</summary>
    private static readonly List<Mobile> _recruits = [];

    public BotProwl(Map map, Point3D where) : this(map, where, false)
    {
    }

    /// <param name="company">
    /// Whether this ground asks more strength than the bot has on its own. When it does, the first beat
    /// raises a company out of whoever is standing there and the errand fails honestly if it cannot — the
    /// obligation in Patrick's order of 03.09.2026: pass the threshold, or bring people who together do.
    /// </param>
    /// <param name="legs">The waypoints of the squares to sweep, in order, the first being <paramref name="where"/>. Null: that one.</param>
    /// <param name="resumed">Whether these are the squares an earlier sweep left for a fight, offered again. See <see cref="BotSweepRecord.Left"/>.</param>
    public BotProwl(Map map, Point3D where, bool company, List<Point3D> legs = null, bool resumed = false)
    {
        _map = map;
        _company = company;
        _legs = legs is { Count: > 0 } ? legs : [where];
        _where = _legs[0];
        _resumedSweep = resumed;
        _price = Price(map, _legs);
    }

    /// <summary>The waypoints of the squares this sweep walks, and which of them it is walking to.</summary>
    private readonly List<Point3D> _legs;

    private int _leg;

    /// <summary>Squares of this sweep whose waypoint was reached.</summary>
    private int _walked;

    /// <summary>Whether <see cref="_where"/> is a creature seen from a waypoint rather than the next square's waypoint.</summary>
    private bool _chasing;

    private readonly bool _resumedSweep;

    private readonly double _price;

    private long _began;

    /// <summary>How this sweep ended itself, for the counters in <see cref="Drop"/>; null when the will ended it.</summary>
    private string _ending;

    /// <summary>What a sweep of these squares is offered at: what sweeps of them brought lately, over the minutes they take.</summary>
    private static double Price(Map map, List<Point3D> legs)
    {
        if (!Sweeps || map == null)
        {
            return Prior;
        }

        var worth = 0.0;

        for (var i = 0; i < legs.Count; i++)
        {
            worth += BotSweepRecord.Worth(map, legs[i]);
        }

        return worth / System.Math.Max(0.1, MinutesPerSquare * legs.Count);
    }

    /// <summary>The squares, by their waypoints, for the log.</summary>
    private string Squares()
    {
        var text = "";

        for (var i = 0; i < _legs.Count; i++)
        {
            text += (i == 0 ? "" : " ") + $"({_legs[i].X}, {_legs[i].Y})";
        }

        return text;
    }

    /// <summary>Where the sweep stands in words: "2 of 3 squares walked".</summary>
    private string Progress => $"{_walked} of {_legs.Count} squares walked";

    /// <summary>Whether this ground was accepted on the strength of a company that does not exist yet.</summary>
    private readonly bool _company;

    /// <summary>
    /// How far round its ground a prowl sent by a zone looks for quarry on arrival: the zone's patrol reach (01.10.2026). Nought
    /// for any other prowl, which looks only as far as BotQuarry.Reach. See <see cref="Advance"/>.
    /// </summary>
    public int Seek { get; init; }

    /// <summary>How many times one prowl may walk on after quarry seen in its zone's patrol.</summary>
    public static int Seeks { get; set; } = 2;

    private int _sought;

    /// <summary>Zone prowls that walked on after quarry in the zone's patrol, and zone prowls that found the zone empty after all.</summary>
    public static long ZoneFollowed { get; private set; }

    public static long ZoneEmpty { get; private set; }

    /// <summary>Prowls for company ground given up because the company raised for it came apart on the way.</summary>
    public static long CameApart { get; private set; }

    /// <summary>Prowls given up because their ground was closed (a bar or a quarantine) while they walked there. See <see cref="Advance"/>.</summary>
    public static long ClosedOn { get; private set; }

    /// <summary>How far round its ground any prowl looks on arrival before it calls the ground empty (01.10.2026, build next).</summary>
    public static int LookWider { get; set; } = 48;

    /// <summary>Prowls that walked on after quarry seen beyond the hunter's reach on arrival, with no zone behind them.</summary>
    public static long Widened { get; private set; }

    /// <summary>How many fights a prowl leading a company it raised takes on with it before the prowl is done. Six.</summary>
    public static int CompanyFights { get; set; } = 6;

    /// <summary>Fights a prowl's raised company took on for what its leader found, rather than the leader walking off alone.</summary>
    public static long CompanyTookOn { get; private set; }

    private int _companyFights;

    /// <summary>The company this errand raised and charged, so that <see cref="Drop"/> can discharge it.</summary>
    private BotSquad _squad;

    private bool _raised;

    /// <summary>
    /// A prowl for company ground raises its company at the gate on its first beat (<c>BotHunter</c> counted it there with
    /// <c>BotQuad.Together</c>), so until it has, it brings a company's worth of this bot; after, the company itself.
    /// </summary>
    public override double Brings(Mobile body) =>
        _company && !_raised ? System.Math.Max(BotQuad.Strength(body), BotThreat.Power(body) * BotSquad.Usual) : BotQuad.Strength(body);

    private bool _gated;

    private Point3D _gate;

    /// <summary>How near the gate counts as being at it. A place to meet, not a tile to stand on.</summary>
    public static int GatherWithin { get; set; } = 3;

    /// <summary>Companies raised because the ground asked more than one bot could bring.</summary>
    public static long Raised { get; private set; }

    /// <summary>Errands given up because the company could not be raised after all.</summary>
    public static long Unraised { get; private set; }

    /// <summary>Companies that came up short of what their ground asked and went to ground they could take instead.</summary>
    public static long Regathered { get; private set; }

    /// <summary>
    /// How long one bot's claim on a square holds off everybody else's company.
    ///
    /// Long enough for the company to form and set out, and short enough that a claim by a bot which then
    /// died or was outbid does not fence the square off. A minute.
    /// </summary>
    public static int ClaimMs { get; set; } = 60000;

    private static readonly Dictionary<(int Map, int X, int Y), long> _raising = [];

    public static bool Raising(Map map, Point3D where)
    {
        var key = BotQuad.Key(map, where);

        if (_short.TryGetValue(key, out var fell))
        {
            if (Core.TickCount - fell < ShortRestMs)
            {
                return true;
            }

            _short.Remove(key);
        }

        return _raising.TryGetValue(key, out var when) && Core.TickCount - when < ClaimMs;
    }

    /// <summary>
    /// <b>Ground a company came up short for is rested, not handed to the next bot a minute later (01.10.2026).</b> The claim
    /// above lasts a minute, and a raise that failed let it lapse into the next offer of the same ground: 14 prowls in the first
    /// 26 minutes of 379–380 failed "could not raise enough strength for (1065, 1125): 1656 … 9778 of the 13500 it asks", one
    /// after another (the analyst's r29, section 4). The hunter reads this through <see cref="Raising"/> and passes the ground
    /// over, as it passes over ground a company is being raised for.
    /// </summary>
    public static int ShortRestMs { get; set; } = 600000;

    private static readonly Dictionary<(int Map, int X, int Y), long> _short = [];

    /// <summary>Grounds rested because the company raised for them came up short. See <see cref="ShortRestMs"/>.</summary>
    public static long ShortRested { get; private set; }

    private static void Claim(Map map, Point3D where) => _raising[BotQuad.Key(map, where)] = Core.TickCount;

    public override string Kind => Trade;

    /// <summary>Goes where the fighting is on purpose. See <see cref="BotDeed.Braves"/>.</summary>
    public override bool Braves => true;

    /// <summary>
    /// <b>A prowl put down for a rescue or a mend is taken up again (29.09.2026).</b> Seventeen of two hundred and eleven
    /// prowls on build 300 ended "dropped: summoned by mend" or "jumped by rescue" — the bot did the better thing and the
    /// prowl was counted lost. It resumes, with its walk counted afresh; hurt, it stays down as every braving deed does.
    /// </summary>
    public override bool Resumes => true;

    public override void Resumed(IBotWilful bot)
    {
        _plans = -1;
        _planLeast = int.MaxValue;
        _nearest = int.MaxValue;
        _stalled = 0;
    }

    /// <summary>Its ground is a dart, and another dart is not news. See <see cref="BotDeed.Guess"/>.</summary>
    public override bool Guess => true;

    public override Map Map => _map;

    public override Point3D Where => _where;

    /// <summary>
    /// <b>What sweeps of these squares paid lately, over the minutes the sweep takes (06.10.2026, build next).</b> The flat
    /// <see cref="Prior"/> was corrected towards what prowls pay — nothing, by construction, since the fight is another piece of
    /// work — and then by the bot's own ledger of prowls at the place, also nothing; what was left was a number that said
    /// nothing about the ground, so a walk to an empty field and a walk into a troll's wood were offered alike. See
    /// <see cref="BotSweepRecord.Worth"/>: an untried square is offered at <see cref="BotSweepRecord.SquarePrior"/>, one whose
    /// sweeps keep finding nothing falls towards nought, and one whose finds paid rises with them.
    /// </summary>
    public override double Expects => _price;

    /// <summary>
    /// <b>The walk pays nothing on purpose, and its worth is read from the squares instead (06.10.2026, build next).</b> The
    /// trade's correction and the ledger would measure the walk's own takings — always nought — and pull every sweep's price
    /// down alike, which is the reading <see cref="Expects"/> replaces. At nought it is still let in at the auction's floor,
    /// below anything that pays. See <see cref="BotDeed.Unpaid"/>.
    /// </summary>
    public override bool Unpaid => Sweeps;

    public override double Minutes => Sweeps ? MinutesPerSquare * _legs.Count : WorkMinutes;

    /// <summary>Nothing. Walking teaches a bot nothing, and saying otherwise would make walking a career.</summary>
    public override SkillName? Trains => null;

    public override int Outlay => 0;

    /// <summary>
    /// <b>Reckoned as coin, because it is the walk to a hunt (01.10.2026, build next).</b> The purse factor discounts work that
    /// pays no coin by the bot's need of money (<c>BotAppraisal</c>, floored at a tenth), and a prowl paid none — so the poorer
    /// a fighter, the less it would walk out to find a fight, the one trade that brings new gold into the world. On build 384
    /// 59.5 % of the bots held under 100gp and 21 % of those were hunting or in a company (the analyst's
    /// sonnet-trade-behaviour-384). Patrick, 30.09: a bot without money hunts. A prowl's own price (<see cref="Prior"/>) still
    /// puts it under every real offer; only the poor bot's discount on it is gone.
    /// </summary>
    public override double Coin => 1.0;

    public override int Made => 0;

    /// <summary>Prowls taken by a bot holding under <see cref="BotHunter.PoorBelow"/>. See <see cref="Coin"/>.</summary>
    public static long PoorTaken { get; private set; }

    public override void Taken(IBotWilful bot)
    {
        if (bot?.Self is { } body && BotYield.Spendable(body) < BotHunter.PoorBelow)
        {
            PoorTaken++;
        }

        // Once per sweep: Taken is not asked again when the will takes a paused sweep up.
        if (_began == 0)
        {
            _began = Core.TickCount;
            SweepsTaken++;

            if (_resumedSweep)
            {
                ResumedTaken++;
            }

            BotSweepRecord.Taken(bot?.Self);
        }
    }

    public override string Stage =>
        _legs.Count > 1 || _walked > 0 ? $"looking for a fight near {_where}, square {System.Math.Min(_leg + 1, _legs.Count)} of {_legs.Count}" : $"looking for a fight near {_where}";

    public override BotDoing Advance(IBotWilful bot)
    {
        var body = bot?.Self;

        if (body == null || _map == null || _map == Map.Internal)
        {
            return BotDoing.Failed("no body");
        }

        // <b>Closed ground is left, not walked into (03.10.2026).</b> The hunter never offers ground a bar or a quarantine closes
        // (BotRefused.Refusing); one declared on it while the bot or its company walks there — three of ours dead there in half
        // an hour, or one to a plague beast — ends the walk, company and all (Drop puts the charge down). On build 395 (03:20–04:19)
        // 60 of the 245 deaths fell inside a quarantine that had stood half a minute or more, 47 of them at work taken after it was
        // declared, nine in companies a prowl raised.
        if (BotBarred.Holds(_map, _where))
        {
            ClosedOn++;
            _ending = "closed";

            return BotDoing.Failed($"({_where.X}, {_where.Y}) was closed to the population while it walked there; {Progress}");
        }

        // <b>The obligation, discharged before a step is taken.</b> This ground was accepted only because the
        // bots standing here would clear its threshold together, so the company is raised now, out of whoever
        // is still standing here — which may be fewer than when it was weighed, and if it is, the errand ends
        // rather than walking one bot into ground that was refused to one bot.
        if (_company && !_raised)
        {
            // <b>Gathered at the edge of the town rather than in the middle of it.</b> Patrick's order of
            // 03.09.2026: bots live and bank and buy in the middle of Britain, so that is where one of them
            // is standing when it decides it needs help — and the company it raises there walks the whole
            // width of the town before it has gone anywhere at all. The edge in the right direction is the
            // shortest honest place to meet, and more of the population passes it than passes anywhere
            // inside the walls. Outside a town Gate answers with nothing and the company forms where it
            // stands, which is what it did before.
            if (!_gated)
            {
                _gated = true;
                _gate = BotPopulation.Gate(_map, body.Location, _where);

                // Claimed as soon as it commits to gathering rather than on arrival, or every other bot in
                // town decides on the same square while this one is still walking to the gate.
                Claim(_map, _where);
            }

            if (_gate != Point3D.Zero && !body.InRange(_gate, GatherWithin))
            {
                return BotDoing.Walk(_map, _gate, BotArrival.Within(GatherWithin), "to the edge of town to gather");
            }

            _raised = true;

            if (bot is IBotSquadMember { Squad: null } member && BotSquads.Running)
            {
                var squad = BotSquads.Form(member);

                if (squad != null)
                {
                    // <b>The strongest first, because that is what the choice counted.</b> Taken in whatever order the map
                    // hands them out, a company filled to its ceiling from a crowd came to less than the strongest few the
                    // proposer had added up. See BotQuad.Together.
                    _recruits.Clear();

                    foreach (var mobile in _map.GetMobilesInRange<Mobile>(body.Location, BotMuster.Reach))
                    {
                        if (mobile != body && mobile is IBotSquadMember { Squad: null } && mobile is IBotAlly { AbleToFight: true }
                            && mobile is { Deleted: false, Alive: true })
                        {
                            _recruits.Add(mobile);
                        }
                    }

                    _recruits.Sort((a, b) => BotThreat.Power(b).CompareTo(BotThreat.Power(a)));

                    // <b>As many as the ground asks, pressed to the enlistment (01.10.2026).</b> Joined only, the company's members
                    // kept their own errands in hand and a march hands out no stations (BotSquad.MarchUnstationed), so they stayed
                    // where they were raised: at 23:14 on 379 Rosalind's company of four marched with its members "going home",
                    // "out to the woods" and on prowls of their own. Each is now pressed to the enlistment and follows its leader
                    // (BotEnlist.Holding); a bot whose work the enlistment would cost it is left at it; and the company grows past
                    // the usual five only while it is short of twice what the ground asks (BotSquads.Enough), up to twenty.
                    var asked = BotQuad.Muscle(_map, _where);
                    var strength = BotQuad.Strength(body);

                    for (var i = 0; i < _recruits.Count && !BotSquads.Enough(squad, strength, asked); i++)
                    {
                        if (_recruits[i] is not IBotSquadMember other)
                        {
                            continue;
                        }

                        var past = squad.Count >= BotSquad.Usual;

                        if (BotSquads.Enrol(squad, other, _map, body.Location, $"called to {body.Name}'s company for ({_where.X}, {_where.Y})"))
                        {
                            strength += BotThreat.Power(_recruits[i]);

                            if (past)
                            {
                                BotSquads.Past();
                            }
                        }
                    }

                    _recruits.Clear();

                    // A company without a medic levies the nearest free healer (01.10.2026, build 388). See BotBand.LevyMedic.
                    BotBand.LevyMedic(squad, body, _map, body.Location, $"called to {body.Name}'s company for ({_where.X}, {_where.Y}), as its medic");

                    // Held together for the walk, exactly as a scouting party is: a company with nothing to
                    // fight yet is dissolved on its next beat otherwise, and there is nothing to fight until
                    // it arrives. See BotSquad.Charged.
                    //
                    // <b>And kept, so that it can be given back.</b> Every other charge on this shard has a
                    // matching release — BotScout, BotSweep and BotHarrow all put the flag down when their
                    // errand ends — and this one had none, so a company raised for a walk to a hunting
                    // ground outlived the walk, the hunt and everything after it. A charged company never
                    // ages (see BotSquad.Quiet), so it stood for ever with its leader holding "nothing":
                    // 94 of the 136 stall reports in the small hours of 04.09.2026, some of them nineteen
                    // minutes long, all of them with the words "in a company" on the end. See Drop.
                    _squad = squad;
                    squad.Charged = true;

                    // On the road until it gets there: nothing sought but what stands by the road, nobody scattered while the
                    // leader holds for a straggler (build 338). See BotSquad.OnTheMarch; Drop puts it down.
                    squad.OnTheMarch = true;
                }
            }

            if (!BotQuad.Dares(body, _map, _where))
            {
                // <b>A company that came up short is still a company.</b> Nine prowls in a quarter of an hour on
                // build 269 ended "could not raise enough strength": the proposer weighed the bots standing where
                // it asked, the raise counted the bots standing at the gate, and the two crowds were not the same
                // crowd. The bots who answered are standing here armed, so the prowl is turned to the strongest
                // ground within reach that what was raised can take, the same way a prowl refused a road turns.
                // Only when there is none is the shortfall a failure.
                var instead = Redart(body, company: true);

                if (instead != Point3D.Zero)
                {
                    Regathered++;

                    logger.Information(
                        "{Name} raised {Strength:F0} of the {Asked:F0} that ({X}, {Y}) asks, so the company of {Count} goes to ({IX}, {IY}), which asks {Less:F0}, instead",
                        body.Name,
                        BotQuad.Strength(body),
                        BotQuad.Muscle(_map, _where),
                        _where.X,
                        _where.Y,
                        _squad?.Count ?? 1,
                        instead.X,
                        instead.Y,
                        BotQuad.Muscle(_map, instead)
                    );

                    _where = instead;

                    // One square, the one what was raised can take: nothing else on the route was weighed against it.
                    _legs.Clear();
                    _legs.Add(instead);
                    _leg = 0;

                    return BotDoing.Walk(_map, _where, BotArrival.Within(ArriveWithin), $"to ({_where.X}, {_where.Y}) with what was raised");
                }

                Unraised++;
                _ending = "unraised";
                ShortRested++;
                _short[BotQuad.Key(_map, _where)] = Core.TickCount;

                // With the two numbers and the place, because the proposer weighed the bots standing where it asked
                // and this raised the bots standing here. See BotHunter.Hunting.
                return BotDoing.Failed(
                    $"could not raise enough strength for ({_where.X}, {_where.Y}): {BotQuad.Strength(body):F0} of the {BotQuad.Muscle(_map, _where):F0} it asks, gathered at ({body.X}, {body.Y})"
                );
            }

            Raised++;

            logger.Information(
                "{Name} raised a company for ({X}, {Y}), which asks {Muscle:F0} of strength",
                body.Name,
                _where.X,
                _where.Y,
                BotQuad.Muscle(_map, _where)
            );
        }

        // <b>With the company it raised (01.10.2026).</b> Its company in a fight on the way holds the walk until the fight is done
        // — the company's own beat answers whatever set upon it (BotSquad.Retarget) — and a company that came apart on the road
        // leaves its leader short of what this ground asks, which it was never offered alone: the errand ends there, honestly.
        if (_company && _raised)
        {
            if (_squad is { Disbanded: false, Stance: BotSquadStance.Fighting } fighting && (bot as IBotSquadMember)?.Squad == fighting)
            {
                return BotDoing.Work($"standing to a fight on the way to ({_where.X}, {_where.Y}) with the company");
            }

            // <b>A company that withdrew from something it could not beat does not leave its leader to walk on alone (03.10.2026).</b>
            // See BotSquad.Overmatched: the leader runs by its own rung if the thing is on it (BotSquad.Withdrawing), and the ground
            // is not taken up again by one bot.
            if (_squad is { Disbanded: true, WithdrewFrom: { } withdrewFrom })
            {
                _squad = null;

                _ending = "company";

                return BotDoing.Failed($"the company raised for ({_where.X}, {_where.Y}) withdrew from {withdrewFrom.Name}, which it could not beat");
            }

            if (_squad is { Disbanded: true } && !BotQuad.Dares(body, _map, _where))
            {
                _squad = null;
                CameApart++;
                _ending = "company";

                return BotDoing.Failed($"the company raised for ({_where.X}, {_where.Y}) came apart on the way, and it asks {BotQuad.Muscle(_map, _where):F0} of one bot's {BotQuad.Strength(body):F0}");
            }
        }

        // The errand's whole purpose, met. Finished rather than pressed on with: the hunt is worth ten times
        // this and will be chosen the moment anything is asked.
        //
        // Asked through BotQuarry.Worthwhile rather than by looking for a quarry directly, because "there is
        // something here" and "the hunter will offer it to me" are not the same statement, and where they
        // came apart the bot finished and retook this errand ten times a second. See that method.
        // <b>Something has already chosen, and a bot out looking for a fight does not get to walk past one.</b>
        // Worthwhile below asks what this bot would <em>pick</em> — the biggest thing it could take alone,
        // unclaimed, not shunned, not crowded — and every one of those filters is right for choosing and
        // wrong for being chosen. An orc that has settled on a mage is not a candidate the mage is weighing:
        // it is coming either way, and the only question left is whether the mage spends the next minute
        // running in front of it. Cedric spent it running.
        var picked = BotThreat.Hunter(body, BotMobile.NoticeRange);

        // <b>A prowl leading the company it raised fights with it, and does not walk off alone (01.10.2026, build next).</b> The
        // endings below are a lone hunter's: something it could take is in reach, so the prowl ends and the auction offers the
        // hunt. A leader holding a company it raised ended the same way — and the company, its charge put down with the prowl,
        // stood eight quiet seconds and dissolved, its members ending "company N went on without it". On build 384 (00:42–10:20)
        // 935 prowls raised a company; 533 ended "something worth fighting" a median of 10 seconds after the raise and 92
        // "something has picked this fight for us" after 15, and 2878 of the 3509 hollow enlistments of the session were the
        // members of such companies (a mean of 1.4 minutes each). The company takes on what the leader found, and the prowl holds
        // it through the fight and walks on with it after, up to CompanyFights fights.
        if (_company && _raised && _squad is { Disbanded: false } led && bot is IBotSquadMember leading && leading.Squad == led)
        {
            var foe = picked;

            // What one bot could take; a company's size of quarry by the road is the company's own look (BotSquad's hunt beat).
            if (foe == null && BotThreat.Decide(body, BotMobile.NoticeRange) != BotStand.Outmatched)
            {
                foe = BotQuarry.Best(body, BotQuarry.Reach);
            }

            // <b>One fight is counted once (01.10.2026, Fable review).</b> The squad's stance turns to Fighting on the squad's
            // own beat, not on this one, so a prowl beating before it saw the same creature again, counted a second fight and
            // engaged a company already on it: six beats, not six fights, and the prowl was done. A company already on the
            // creature is standing to it (the line above), and nothing is counted.
            if (foe != null && ReferenceEquals(led.Focus, foe))
            {
                return BotDoing.Work($"standing to a fight on the way to ({_where.X}, {_where.Y}) with the company");
            }

            // <b>Not a fight the company would withdraw from (03.10.2026, build next).</b> Whatever had picked the leader was taken
            // on here unweighed, as it was wherever a company was set upon: Beatrix's company 47, raised for this errand, took on a
            // wandering plague beast of 20262 at 03:18:11 on build 395 and lost seven. The company withdraws instead and the prowl
            // ends with it; a caster is fought, as before (Patrick, 30.09: fight it to the end). See BotSquad.Overmatched.
            // A thing that picked the leader is defence, judged at the withdrawal's odds; one the leader found is a choice (company16).
            if (foe != null && led.Beyond(foe, 0.0, picked != null ? BotSquad.WithdrawOdds : 0.0) && !BotSquad.Caster(foe))
            {
                var reason = led.Withdraw(foe, BotSquad.Reckon(foe), led.Able());

                _squad = null;
                BotSquads.Disband(led, reason);
                _ending = "company";

                return BotDoing.Failed($"the company raised for ({_where.X}, {_where.Y}) withdrew from {foe.Name}, which it could not beat");
            }

            if (foe != null && _companyFights < CompanyFights)
            {
                _companyFights++;
                CompanyTookOn++;

                // Found rather than set upon, the company musters short of it when its strength is spread (company16). See BotSquad.Engage.
                led.Engage(foe, leading, picked == null);

                return BotDoing.Work($"the company takes on {foe.Name} on the way to ({_where.X}, {_where.Y})");
            }

            if (foe != null)
            {
                _ending = "companydone";

                return BotDoing.Done($"the company put down {led.Kills} with it, and {CompanyFights} fights is a prowl's worth");
            }
        }

        // <b>Unless it is a fight this bot has already been refused, in which case handing over is a circle.</b>
        // BotSlay gives a fight up on the numbers standing round the quarry and marks the creature crowded —
        // one bot cannot have it. But the creature is still on this bot, so this test fired again, the errand
        // finished again, the auction offered the fight again, and it was refused again. On 02.09.2026 between
        // 22:46 and 22:56 that came to 123 prowls taken, 97 ended, 51 of those ended here, against 50 refusals
        // for "too many of them around" — Joss went round it 25 times and Orin 17, and each lap wrote another
        // nought per minute against the very ground the crown had just started sending companies to.
        //
        // Walking on is also the right answer and not merely the quiet one: a bot that has judged itself
        // outnumbered and stays where the numbers are has decided nothing. Crowded lasts two minutes, so the
        // creature gets one more honest hand-over after that if it is still interested.
        // <b>Once, and the first attempt at this was wrong in an instructive way.</b> The condition was
        // Crowded alone — the mark BotSlay writes when it gives a fight up on the numbers — on the theory
        // that the loop ran between prowl and a refused rescue. The window of 23:18 to 23:31 on 02.09.2026
        // refuted it: 345 prowls taken, 189 ending here, and no fight of any kind recorded in between. The
        // auction was not refusing the fight; it was never being offered one, because it only offers a fight
        // the bot would choose and something that has chosen the bot is very often not that. A hand-over
        // with no receiver is a loop. So it is offered once — see BotQuarry.Hand — and after that walking on
        // is the answer, which is also what a bot nobody will fight for ought to be doing.
        if (picked != null && !BotQuarry.Crowded(picked) && !BotQuarry.Handed(picked))
        {
            BotQuarry.Hand(picked);

            return Found(body, "something has picked this fight for us");
        }

        if (BotQuarry.Worthwhile(body))
        {
            return Found(body, "something worth fighting");
        }

        if (body.InRange(_where, ArriveWithin))
        {
            // <b>A square is walked when its waypoint is reached (06.10.2026, build next).</b> Counted once, on the arrival at the
            // square's own waypoint and not at the end of a walk after a creature seen from it. See the class's note.
            if (!_chasing)
            {
                _walked++;
                SquaresWalked++;
                BotSweepRecord.Walked(_map, _legs[_leg]);
            }

            _chasing = false;

            // <b>A prowl sent to a zone looks over the zone's patrol before it calls the ground empty (01.10.2026).</b> The zone
            // hands the hunter a tile to stand on — a spawner's own — and its creatures walk up to their spawner's range from it
            // (sixty-odd tiles at the widest, BotZone.DeclaredRange, ObservedRange), while the arrival asks only BotQuarry.Reach
            // (thirty): on 378 prowls sent into hostile zones ended "nothing here" 20 times in 33, into clear ones 27 in 71,
            // against 14 in 50 with no zone (the coordinator's measurement, 23:06). Within the zone's own reach, the quarry the
            // hunter would pick is walked after, at most Seeks times; only then is the ground empty.
            // <b>Every prowl looks wider before it calls the ground empty, not only a zone's (01.10.2026, build next).</b> The zone
            // branch above asked Seek > BotQuarry.Reach, and the zones' reach is clamped to 8–40: on build 384 it walked on after
            // quarry 0 times in ten hours while 2148 of 4767 prowls ended "nothing here" (45 %). The look on arrival is the zone's
            // reach or LookWider, whichever is the more; a company looks for what it was raised for as well.
            var look = System.Math.Max(Seek, LookWider);
            var seen = look > BotQuarry.Reach && _sought < Seeks
                ? BotQuarry.Best(body, look) ?? (_raised && _squad is { Disbanded: false } ? BotQuarry.Company(body, look) : null)
                : null;

            if (seen != null)
            {
                _sought++;

                if (Seek > BotQuarry.Reach)
                {
                    ZoneFollowed++;
                }
                else
                {
                    Widened++;
                }

                _where = seen.Location;
                _chasing = true;
                _nearest = int.MaxValue;
                _plans = -1;
                _stalled = 0;

                return BotDoing.Walk(_map, _where, BotArrival.Within(ArriveWithin), $"after {seen.Name} in the zone's patrol");
            }

            if (Seek > 0)
            {
                ZoneEmpty++;
            }

            return NextSquare(body);
        }

        var gap = System.Math.Max(System.Math.Abs(body.X - _where.X), System.Math.Abs(body.Y - _where.Y));

        var along = Along(body);

        if (gap < _nearest || along)
        {
            if (gap >= _nearest)
            {
                RoadKept++;
            }

            _nearest = System.Math.Min(_nearest, gap);
            _stalled = 0;
        }
        // A held walk is not a walk that stopped closing (12.10.2026, w7): a buyer stood for its carrier (BotRendezvous.Stand)
        // or a trader for its guards stands by somebody else's word, and BotWill's own watch skips those beats too.
        else if (body is not BotMobile { Journey.Held: true } && ++_stalled >= TrekLimit)
        {
            // A creature seen from a waypoint that could not be walked after: the square it was seen from was walked, so the
            // sweep goes on to the next one rather than failing for a walk that was never one of its squares.
            if (_chasing)
            {
                _chasing = false;

                return NextSquare(body);
            }

            // Said on the map first, exactly as Bend does and for the reason given there: what one bot proves
            // about ground is true for all of them, and the worst square goes to the front of every prowl's
            // list. The reading is not touched — the square is as dangerous as it was; what is learned is
            // that this bot could not get there.
            BotPeril.Baulked(_map, _where);

            // <b>And to the quadrant record, which is the other map this ground can be chosen from and the
            // one that was never told.</b> BotHunter.Feared asks BotQuad.WorstNear; the baulk above goes to
            // BotPeril; so a square the quadrant record thinks is the worst on the island went on being
            // handed to every hunter that asked, however many of them had already failed to reach it. On
            // 03.09.2026, 119 prowls ended at this same line against one square, (1005, 1335), and hunting
            // finished a third as often as in the run before.
            BotQuad.Baulk(_map, _where);
            Baulked++;

            // Where it stopped as well as where it was going. The destination is baulked above, but it is the ground
            // between that refuses: on build 41 these were 49 in half an hour, nearly one destination each, and not
            // one of them said where the bot had stood still.
            // And written down as a place walks that way stop, so the next dart past it is not thrown — only for a walk
            // that got going, because one that never left its start has found nothing about the ground between.
            if (_setOut != Point3D.Zero && !body.InRange(_setOut, 2 * BotBarrier.Near))
            {
                BotBarrier.Stopped(_map, body.Location, _where);
            }

            // <b>One more look, on this side of whatever stopped it.</b> The destination above is already baulked and
            // the stop written down; what is left is a bot standing on ground it did reach. See Redarts.
            if (!_company && _redarts < Redarts)
            {
                _redarts++;

                var next = Redart(body);

                if (next != Point3D.Zero)
                {
                    Redarted++;

                    _where = next;
                    _legs[_leg] = next;
                    _nearest = int.MaxValue;
                    _plans = -1;
                    _stalled = 0;
                    _setOut = Point3D.Zero;

                    return BotDoing.Walk(_map, _where, BotArrival.Within(ArriveWithin), "looking for a fight on this side instead");
                }

                Unredarted++;
            }

            _ending = "unreached";

            return BotDoing.Failed($"got no nearer than {gap} tiles to ({_where.X}, {_where.Y}) from ({body.X}, {body.Y}, {body.Z}); {Progress}");
        }

        if (_setOut == Point3D.Zero)
        {
            _setOut = body.Location;
        }

        return BotDoing.Walk(_map, _where, BotArrival.Within(ArriveWithin), "looking for a fight");
    }

    /// <summary>
    /// The square in hand was walked and nothing found in it: on to the next, or the sweep is done, every square walked.
    ///
    /// <para>
    /// Not a failure — the ground was worth a look and now this bot knows it is empty, which is what the ledger is for, and
    /// what the next choice of ground reads (<c>BotHunter.EmptyRestMs</c>). <b>"Found nothing" is finished work only here, with
    /// every square of the sweep walked</b> (Patrick, 06.10.2026: "if they walked them and found nobody, that is fine, that is a
    /// success"); the walk to one point that ended "nothing here" before build next is not offered any more.
    /// </para>
    /// </summary>
    private BotDoing NextSquare(Mobile body)
    {
        BotHunter.FoundEmpty(body, _legs[_leg]);

        _leg++;

        if (_leg >= _legs.Count)
        {
            _leg = _legs.Count - 1;
            _ending = "empty";

            return BotDoing.Done(
                Sweeps ? $"swept {_walked} square{(_walked == 1 ? "" : "s")} and found nothing: {Squares()}" : "nothing here"
            );
        }

        _where = _legs[_leg];
        _nearest = int.MaxValue;
        _plans = -1;
        _stalled = 0;
        _setOut = Point3D.Zero;

        return BotDoing.Walk(_map, _where, BotArrival.Within(ArriveWithin), "looking for a fight");
    }

    /// <summary>
    /// A fight was found on the way: this sweep is finished, the hunt is the hunter's (offered at the next review), and the
    /// squares not yet walked are kept for the bot to come back to (<see cref="BotSweepRecord.Leave"/>). The square the bot
    /// stands in is credited with the find, and with what the hunt comes to when it ends (<c>BotSlay.Drop</c>).
    /// </summary>
    private BotDoing Found(Mobile body, string what)
    {
        FightsFound++;
        _ending = "fight";

        BotSweepRecord.Found(body, _map, body.Location);

        // The square in hand is left for later unless its waypoint was reached (a chase starts from a reached one).
        var from = _chasing ? _leg + 1 : _leg;

        // A sweep taken up again that finds the same fight before a step is not left again: that would be a circle with the
        // hunter, not a sweep (the hunter offers the hunt, not the sweep, whenever this test passes — see BotQuarry.Worthwhile).
        var keep = Sweeps && !_company && from < _legs.Count && !(_resumedSweep && _walked == 0 && Core.TickCount - _began < 5000);

        if (keep)
        {
            BotSweepRecord.Leave(body, _map, _legs, from);
        }

        if (!Sweeps || _legs.Count == 1 && _walked == 0)
        {
            return BotDoing.Done(what);
        }

        return BotDoing.Done($"{what} in the square at ({body.X}, {body.Y}), {Progress}{(keep ? "; the rest are left for after it" : "")}");
    }

    /// <summary>
    /// Ground within <see cref="RedartReach"/> of where a stalled walk stands that a search proves this bot can walk to,
    /// or nothing.
    ///
    /// <para>
    /// The free tests the hunter asks of a dart first — somewhere else, out of town, not a square a road was lately
    /// refused to, not a pocket proved sealed from here, ground this bot dares alone — then the noisiest of what is left,
    /// and a real search only for the best <see cref="RedartVets"/> of them. A found road is the one proof of reaching
    /// there is, and it is bought here at most twice, for a walk that has already failed.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <paramref name="company"/>: for a company raised short of its ground, only ground that asks some strength — a fight — is
    /// looked at (01.10.2026). The raise's fallback took the noisiest walkable ground the company dared, and quiet ground dares
    /// anybody: of 146 companies sent elsewhere on 30.09, 92 went to ground "which asks 0" and their prowls ended "nothing here"
    /// seconds later (Rosalind's four at 23:13:50–23:13:58 on 379), a company raised for nothing.
    /// </remarks>
    private Point3D Redart(Mobile body, bool company = false)
    {
        _candidates.Clear();

        for (var i = 0; i < RedartSamples; i++)
        {
            var x = body.X + Utility.RandomMinMax(-RedartReach, RedartReach);
            var y = body.Y + Utility.RandomMinMax(-RedartReach, RedartReach);

            if (!BotStep.Settle(_map, x, y, out var z))
            {
                continue;
            }

            var where = new Point3D(x, y, z);

            if (Utility.InRange(body.Location, where, 2 * ArriveWithin)
                || Region.Find(where, _map)?.IsPartOf<TownRegion>() == true
                || BotRefused.Refusing(_map, where)
                || BotReach.Ask(_map, body.Location, where, BotArrival.Within(ArriveWithin)) == BotReachVerdict.Sealed
                || !BotQuad.Dares(body, _map, where)
                || company && BotQuad.Muscle(_map, where) <= 0.0)
            {
                continue;
            }

            _candidates.Add(where);
        }

        var map = _map;

        // The noisiest first, as the hunter breaks its ties, and the less safe of two equally quiet ones.
        _candidates.Sort(
            (a, b) =>
            {
                var noise = BotPeril.Reading(map, b).CompareTo(BotPeril.Reading(map, a));

                return noise != 0 ? noise : BotQuad.Safety(map, a).CompareTo(BotQuad.Safety(map, b));
            }
        );

        for (var i = 0; i < _candidates.Count && i < RedartVets; i++)
        {
            if (BotPath.CanReach(_map, body.Location, _candidates[i], BotArrival.Within(ArriveWithin), RedartVetMs))
            {
                return _candidates[i];
            }
        }

        return Point3D.Zero;
    }

    /// <summary>
    /// The ground could not be walked to. It says so on the map first, then looks once on its own side, as a stalled
    /// walk does, and gives up if that finds nothing.
    ///
    /// <para>
    /// <b>Marking it was the first reason for overriding this.</b> The peril map's worst square goes to
    /// the front of every prowl's candidate list, so a dangerous square nobody can reach is offered to every
    /// idle fighter on the island, over and over: 209 failures on (1308, 1380) in twenty-five minutes on
    /// 27.08.2026, out of 257 failed prowls in total. <c>BotPeril.Baulked</c> was written for exactly this
    /// and says so in its own note — "a lone bot reading this map for somewhere to prowl is kept off it
    /// too" — but only companies ever wrote the mark, and a company is the rarest thing on the shard. What
    /// one bot proves about ground is true for all of them.
    /// </para>
    ///
    /// <para>
    /// The reading is not touched. The square is every bit as dangerous as it was; the only thing learned is
    /// that nobody can get there.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        BotPeril.Baulked(_map, _where);
        BotQuad.Baulk(_map, _where);

        // <b>And the square written down for an hour, for every sweep (07.10.2026, work18).</b> Only a road refused or given up on
        // the way to the square's own waypoint: a stalled step is about the walker, a chase about the creature. See
        // BotSweepRecord.Wayless and BotHunter.NoWay.
        if (!_chasing && bot?.Self is BotMobile { LastWalk: BotWalkResult.Refused or BotWalkResult.GaveUp })
        {
            BotSweepRecord.Wayless(_map, _where);
        }

        // <b>And then the one look on its own side that a stalled walk gets, because a refused road is the same failure
        // told sooner.</b> Build 45 gave the stall a look within RedartReach of where the bot stands, and on build 61 it sent
        // on 77 of the 103 that looked; a road refused on the way was still given up. Between 17:21 and 18:07 on 15.09.2026
        // "no way through" was the largest prowl failure, 32 against 31 stalls, and none of them came at the start: 0.2 to
        // 6.6 minutes into the walk, a median of 1.4, which is a bot that walked a partial plan to a bank and was refused
        // there — standing on ground it reached, exactly as the stall is. The allowance is the stall's, so one errand looks
        // once whichever way it fails, and never for a company.
        var body = bot?.Self;

        if (body == null || _company || _redarts >= Redarts || body.Map != _map)
        {
            _ending = "unreached";

            return false;
        }

        _redarts++;

        var next = Redart(body);

        if (next == Point3D.Zero)
        {
            Unturned++;
            _ending = "unreached";

            return false;
        }

        Turned++;

        _where = next;
        _legs[_leg] = next;
        _chasing = false;
        _nearest = int.MaxValue;
        _plans = -1;
        _stalled = 0;
        _setOut = Point3D.Zero;

        return true;
    }

    /// <summary>
    /// The charge goes back where it came from, whatever ended this.
    ///
    /// <para>
    /// In <c>Drop</c> rather than at any of the endings, because there are five of them — arrived, gave up,
    /// outbid, stalled, died — and this shard has paid for a lesson written under exactly one of them three
    /// times already. Drop is the single gate every ending goes through.
    /// </para>
    ///
    /// <para>
    /// The company itself is not dissolved. Discharging is enough and is the honest thing: with the flag
    /// down the quiet clock starts, and a company that has found something to fight in the meantime goes on
    /// fighting it rather than being broken up by the errand that introduced them.
    /// </para>
    /// </summary>
    public override void Drop(IBotWilful bot)
    {
        if (_squad is { Disbanded: false })
        {
            _squad.Charged = false;
            _squad.OnTheMarch = false;
        }

        _squad = null;

        Ended(bot?.Self);
    }

    private bool _counted;

    /// <summary>
    /// The sweep's ending, counted by cause and written down with its squares (06.10.2026, build next). Here rather than at each
    /// ending for the reason <see cref="Drop"/> gives: the will ends a sweep in ways this errand never sees — turned back by
    /// what the bot saw on the road (BotSight), outbid, put down, the stall watch, a death — and each still ends here.
    /// </summary>
    private void Ended(Mobile body)
    {
        if (_counted)
        {
            return;
        }

        _counted = true;

        var minutes = _began == 0 ? 0.0 : (Core.TickCount - _began) / 60000.0;

        SweepsEnded++;
        SweepMinutes += minutes;

        string how;

        switch (_ending)
        {
            case "empty":
                EndedEmpty++;
                how = "every square walked and nothing found";
                break;
            case "fight":
                EndedFight++;
                how = "a fight found";
                break;
            case "companydone":
                EndedCompany++;
                how = "its company's fights done";
                break;
            case "unreached":
                FailedUnreached++;
                how = "failed: could not reach its square";
                break;
            case "unraised":
                FailedUnraised++;
                how = "failed: could not raise the company its ground asked";
                break;
            case "company":
                FailedCompany++;
                how = "failed: its company came apart or withdrew";
                break;
            case "closed":
                FailedClosed++;
                how = "failed: its ground was closed";
                break;
            default:
                if (body is not { Deleted: false, Alive: true })
                {
                    EndedDead++;
                    how = "the bot died";
                    break;
                }

                EndedByWill++;
                how = "ended by the will (turned back, outbid, put down or stalled: the will's line says which)";

                // <b>Met by a fight while the will took it off its squares</b> (a hunt or a company outbidding it, which the will
                // counts as the prowl's purpose met): the squares left wait for it, as after a find of its own.
                if (Sweeps && !_company && body.Map == _map && (body.Combatant != null || BotThreat.Hunter(body, BotMobile.NoticeRange) != null))
                {
                    var from = _chasing ? _leg + 1 : _leg;

                    if (from < _legs.Count)
                    {
                        BotSweepRecord.Leave(body, _map, _legs, from);
                        how += "; a fight at hand, the squares left wait for it";
                    }
                }

                break;
        }

        if (Sweeps)
        {
            logger.Information(
                "{Name} ended a sweep of {Count} squares {Squares}{Again}: {Walked} walked, {How}, in {Minutes:F1} min",
                body?.Name,
                _legs.Count,
                Squares(),
                _resumedSweep ? " (taken up again after a fight)" : "",
                _walked,
                how,
                minutes
            );
        }
    }

    /// <summary>The sweeps, for the island's line. See BotHunter.Describe.</summary>
    public static string DescribeSweeps() =>
        Sweeps
            ? $"prowls as sweeps of {SweepLeast}-{SweepMost} squares: {SweepsOffered} offered naming {SquaresOffered} squares ({ShortSweeps} with one square only, {ResumeOffered} offered again after a fight; {Underfoot} squares passed over for being where the bot stood or had lately found nothing; {SquaresNoWay} looks at a square refused for a sweep's walk to it failing within the hour and {SquaresNoGround} for ground the reach map calls an island or a pocket, {FirstNoWay} prowls not offered for their first square), "
              + $"{SweepsTaken} taken ({ResumedTaken} taken up again), {SquaresWalked} squares walked, {FightsFound} fights found; "
              + $"of {SweepsEnded} ended: {EndedEmpty} every square walked and nothing found, {EndedFight} a fight found, {EndedCompany} a company's fights done; failed {FailedUnreached} could not reach a square, {FailedUnraised} could not raise its company, {FailedCompany} company came apart or withdrew, {FailedClosed} ground closed; "
              + $"{EndedByWill} ended by the will (turned back, outbid, put down, stalled), {EndedDead} died; {(SweepsEnded > 0 ? SweepMinutes / SweepsEnded : 0.0):F1} minutes a sweep; {BotSweepRecord.Describe()}"
            : "prowls as sweeps: off";

    /// <summary>
    /// <b>A prowl that leads the company it raised rides beside it, as a sweep does (01.10.2026).</b> A bot in a company is on the
    /// Bound rung, where only work that runs <see cref="BotDeed.Alongside"/> is carried on (BotWill), and the company's charge is
    /// taken back on its next beat from a leader holding anything else (BotSquad.Update). So the company a prowl raised for ground
    /// one bot could not take lost its charge the second after it was raised ("charges taken back because the errand holding
    /// them had ended", 14 in 17 minutes on 378), its leader stood with its walk set aside, and eight quiet seconds later the
    /// company was gone and the leader walked on to that ground alone. While it holds its company, it is the company's walk.
    /// </summary>
    public override bool Alongside => _squad is { Disbanded: false };
}
