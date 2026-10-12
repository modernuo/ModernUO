using Server.Items;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// The student's half of a class: pay, walk to the field, find your place in the block, and stay in it.
///
/// <para>
/// <b>A separate undertaking on a separate bot, and it has to be.</b> The captain could not simply be given
/// a list of pupils to improve: a bot that is being taught has stopped mining, stopped hunting and stopped
/// spending, for a quarter of an hour, and on this shard that is a decision it makes for itself against
/// everything else it could be doing. So attendance goes through the same auction as every other piece of
/// work and loses to a good ore vein exactly as often as it deserves to. The one place this project has ever
/// modelled being told what to do, it removed it again.
/// </para>
///
/// <para>
/// <b>Paid up front, and paid once.</b> The fee is taken when the bot arrives and enrols rather than when
/// the lesson ends, for the same reason a shop takes money at the counter: an undertaking can be dropped for
/// something better at any beat, and a debt that has to be collected from a bot that has wandered off to a
/// mine is a debt nothing on this shard can collect.
/// </para>
/// </summary>
public sealed class BotAttend : BotDeed
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotAttend));

    /// <summary>The ledger's key.</summary>
    public const string Trade = "drill-in";

    /// <summary>
    /// What an hour on the training field is reckoned at per minute before experience corrects it.
    ///
    /// <para>
    /// <b>Worked out from the shard's own numbers rather than chosen.</b> A beat of drill is worth roughly
    /// <c>Rate</c> points, beats come every <c>BeatMs</c>, and this shard has always valued a skill point at
    /// <c>BotYield.GoldPerSkillPoint</c>. That is a real gold-equivalent rate and it is a large one — which
    /// is correct and is the whole reason a bot would ever choose to stand still for a quarter of an hour.
    /// The fee comes off it through <see cref="Outlay"/>, where the auction can see it.
    /// </para>
    /// </summary>
    public static double PerMinute =>
        BotSchool.Rate * (60000.0 / BotSchool.BeatMs) * BotYield.GoldPerSkillPoint * Discount;

    /// <summary>
    /// What the estimate above is multiplied by to allow for everything that will go wrong with it.
    ///
    /// A third. The full arithmetic assumes the captain is standing over this bot on every single beat and
    /// that it is at the bottom of the curve, and neither will be true: attention is shared between six and
    /// the curve tapers. An estimate that is wrong in a knowable direction should be corrected here rather
    /// than left for the ledger to discover over an evening.
    /// </summary>
    public static double Discount { get; set; } = 0.33;

    private readonly Map _map;

    private readonly BotMobile _student;

    private readonly int _bill;

    private bool _enrolled;

    private bool _paid;

    private double _learned;

    /// <summary>Which skill the mark below belongs to, so moving on to a second one does not count it twice.</summary>
    private SkillName? _marked;

    private double _mark;

    private long _began;

    /// <summary>The station on the roster had no road to it, so this student is taught from where it stands. See <see cref="Bend"/>.</summary>
    private bool _stayPut;

    /// <summary>The nearest this student has come to its rank, and how many beats since that improved. See <see cref="NearEnough"/>.</summary>
    private int _nearest = int.MaxValue;

    private int _stalled;

    /// <summary>
    /// How near its rank a student may be when its walk stops closing and it stays where it is.
    ///
    /// <para>
    /// Three tiles, forty beats. The ranks are arithmetic round the field and some of them stand behind tiles with no
    /// footing: Jarek at 15:50 on 22.09.2026 "failed at drill-in: 0.0 points — the walk stopped closing 2 tiles short of
    /// (1462, 1499) after 600 beats", two minutes of asking for a step onto ground the planner had refused, with the fee
    /// paid and the captain teaching two tiles away. A tile of slack (build 188) caught the case one tile short; this
    /// catches the shape rather than the distance — a walk to the rank that has stopped closing, near enough to be
    /// taught — and the captain's attention arithmetic loses a tile or two of nearness, which it can measure and the
    /// student can afford.
    /// </para>
    /// </summary>
    public static int NearEnough { get; set; } = 3;

    public static int StalledBeats { get; set; } = 40;

    /// <summary>Enrolled students taught from where they stood because their station had no road. See <see cref="Bend"/>.</summary>
    public static long Unstationed { get; private set; }

    public BotAttend(Map map, BotMobile student, int bill)
    {
        _map = map;
        _student = student;
        _bill = bill;
        _began = Core.TickCount;
    }

    public override string Kind => Trade;

    /// <summary>A lesson with a start time, which the bot is paying for. See <see cref="BotDeed.Summons"/>.</summary>
    public override bool Summons => true;

    /// <summary>Speaks for a place on the roll the moment the lesson is chosen. See <see cref="BotSchool.Promise"/>.</summary>
    public override void Taken(IBotWilful bot) => BotSchool.Promise(_student);

    public override Map Map => _map;

    public override Point3D Where => BotSchool.Ground;

    public override double Expects => PerMinute;

    public override double Minutes => BotSchool.LessonMs / 60000.0;

    /// <summary>
    /// The skill being taught, so the shard's own reckoning of what this work came to counts the points.
    ///
    /// Read live rather than fixed at the moment the offer was made: a student that reaches the captain's
    /// ceiling in one skill during the lesson is taught the next one it is furthest behind in, and the
    /// takings should follow the teaching.
    /// </summary>
    public override SkillName? Trains => _taught ?? BotSchool.Lacking(_student);

    /// <summary>
    /// The skill the fee bought, kept from the moment it was paid.
    ///
    /// <para>
    /// <b>The takings are reckoned after the class has closed, and by then <c>Lacking</c> answers nothing.</b> The
    /// captain's ending calls <c>BotSchool.Close</c>, which lets the master go; the student's own ending comes on its
    /// next beat, "the class ended — 0.7 points", and <c>BotYield.Takings</c> then asks this deed what it trains and
    /// is told null — so Pell's lesson of 12:21 on 22.09.2026 was booked "−60 coin, 0 made, 0.0 skill" with 0.7 points
    /// on his sheet, and three such lessons taught the population, again, that a lesson pays −5 a minute. Fixed at
    /// the fee, because the stake the takings are measured against was taken on the same skill; the note above about
    /// following the teaching applies before the fee, when the offer is being weighed.
    /// </para>
    /// </summary>
    private SkillName? _taught;

    /// <summary>The fee, named where the auction can see it: a bot that cannot afford this is not offered it.</summary>
    public override int Outlay => _bill;

    /// <summary>Nothing here is coin. A bot that is short of money should go and earn some instead.</summary>
    public override double Coin => 0.0;

    public override bool Alongside => true;

    /// <summary>
    /// Standing in one place is what this work is — <b>once it has got there</b>. See <see cref="BotDeed.Still"/>.
    ///
    /// <para>
    /// Not simply <c>true</c>, and the difference is the whole reason the exemption is safe. Before it is
    /// enrolled this errand is an ordinary walk across Britain with a fee in its pocket, and a walk that
    /// stops is exactly what the stall watch is for. Blanketing the errand would have bought three cancelled
    /// lessons at the price of a student able to stand in a doorway for ever with nothing watching.
    /// </para>
    /// </summary>
    public override bool Still => _enrolled;

    /// <summary>
    /// Kept, once the fee is paid, against the next thing on the board. See <see cref="BotDeed.Steadfast"/>.
    ///
    /// Ronan paid 60gp at 14:49 on 22.09.2026 and "dropped drill-in: 4.4 points so far — outbid by peddle at 152/min after
    /// 3.5 of 10.3 minutes reckoned, taken at 339/min": a student that leaves a paid lesson for two pairs of boots has
    /// thrown the fee away, and the ledger then files the lesson at what it earned before it walked out.
    /// </summary>
    public override bool Steadfast => _enrolled;

    /// <summary>A rank with no footing is a place to stand, not the lesson blocked. See <see cref="BotDeed.BendIsTrouble"/>.</summary>
    public override bool BendIsTrouble => !_enrolled;

    /// <summary>The lesson, with a minute over. See <see cref="BotDeed.HoldsFor"/>.</summary>
    public override double HoldsFor => _enrolled ? Minutes + 1.0 : 0.0;

    public override string Stage =>
        !_enrolled
            ? $"going to the training field, {_bill}gp in hand"
            : $"being drilled at ({BotSchool.Ground.X}, {BotSchool.Ground.Y}), {_learned:F1} points so far";

    public override BotDoing Advance(IBotWilful bot)
    {
        if (bot?.Self is not BotMobile body || !ReferenceEquals(body, _student))
        {
            return BotDoing.Failed("no body");
        }

        var master = BotSchool.Master;

        if (master is not { Deleted: false, Alive: true })
        {
            return _enrolled
                ? BotDoing.Done($"the class ended — {_learned:F1} points")
                : BotDoing.Failed("the class was over before it got there");
        }

        if (!body.InRange(BotSchool.Ground, BotSchool.Pace * BotSchool.Rank + BotSchool.Pace))
        {
            if (_enrolled)
            {
                // Shoved out of the block, or walked out of it. Its own tile is still its own tile: the
                // station is derived from the roster, so going back to it is going back to the same place.
                // Back to its own tile, or, when that tile has no road, back inside the block. See Bend.
                return _stayPut
                    ? BotDoing.Walk(_map, BotSchool.Ground, BotArrival.Within(BotSchool.Pace * BotSchool.Rank), "back into the ranks")
                    : BotDoing.Walk(_map, BotSchool.Station(body), BotArrival.Within(0), "back to my place in the ranks");
            }

            // The place is kept for as long as the walk goes on. See BotSchool.Promise.
            BotSchool.Promise(body);

            return BotDoing.Walk(_map, BotSchool.Ground, BotArrival.Within(BotSchool.Pace * BotSchool.Rank), "going to be taught");
        }

        if (!_enrolled)
        {
            if (!BotSchool.Gathering)
            {
                return BotDoing.Failed("the roll had already been closed");
            }

            // Money first, and only then a place. The other order gives a bot a station it has not paid for
            // and a captain a pupil it cannot charge.
            if (!_paid)
            {
                if (!BotMarket.Charge(body, _bill))
                {
                    return BotDoing.Failed($"could not find the {_bill}gp for a lesson");
                }

                _paid = true;

                // Into the captain's account, which is where every other seller on this shard is paid.
                Banker.Deposit(master, _bill);

                BotSchool.Paid(_bill);

                // Booked here and nowhere else: a lesson that was offered, walked to and never paid for is
                // not a lesson, and charging the cooldown for one would quietly bar a bot for a day over an
                // errand that failed.
                BotSchool.Learned(body);

                // Teaching pays the captain in coin, and coin is what its contentment is short of when it
                // has spent the morning patrolling for nothing.
                master.Resolve.Urges.Paid(_bill);
            }

            if (!BotSchool.Enrol(body))
            {
                return BotDoing.Failed("there was no room left in the class");
            }

            _enrolled = true;
            _began = Core.TickCount;
            _taught = BotSchool.Lacking(body);

            logger.Information(
                "{Name} paid {Bill}gp to be taught {Skill} by {Master}",
                body.Name,
                _bill,
                BotSchool.Lacking(body)?.ToString() ?? "something",
                master.Name
            );
        }

        var station = BotSchool.Station(body);

        // Standing in the right square is the whole of a student's job, and it is not a metaphor: the
        // captain's own arithmetic asks how near he is to this bot, so a pupil in the wrong place learns
        // less, measurably.
        // <b>Within one tile, and it was exactly the tile.</b> The ranks are arithmetic round the field, and at (1460,
        // 1500) the tiles at x 1462–1464 have no footing — the same ones the captain's circuit skips. A student sent to
        // (1462, 1500) walked to the tile beside it and stood there for two minutes asking for the last step, until the
        // walk's watchdog failed the lesson it had paid for: Hale Ashdown and Ysolt at 14:50 on 22.09.2026, 1.2 and 0.0
        // points of a ten-minute class. A tile of slack costs the captain's attention arithmetic nothing it can measure.
        if (!_stayPut && !body.InRange(station, 1))
        {
            var gap = System.Math.Max(System.Math.Abs(body.X - station.X), System.Math.Abs(body.Y - station.Y));

            if (gap < _nearest)
            {
                _nearest = gap;
                _stalled = 0;
            }
            // A held walk is not a walk that stopped closing (12.10.2026, w7): a buyer stood for its carrier (BotRendezvous.Stand)
            // or a trader for its guards stands by somebody else's word, and BotWill's own watch skips those beats too.
            else if (body is not BotMobile { Journey.Held: true } && ++_stalled >= StalledBeats && gap <= NearEnough)
            {
                _stayPut = true;
                Unstationed++;

                return BotDoing.Work($"being drilled from {gap} tiles off my place, which has no footing");
            }

            return BotDoing.Walk(_map, station, BotArrival.Within(1), "taking my place in the ranks");
        }

        // <b>The clock the answer below needs.</b> Work is the one reply nothing judges, so a student that
        // stood in a field being taught nothing by a captain that had wandered off would stand there for
        // ever. The lesson's own length is the fence.
        if (Core.TickCount - _began >= BotSchool.LessonMs)
        {
            return BotDoing.Done($"the lesson ran its course — {_learned:F1} points");
        }

        // <b>Eyes on the captain, every beat, for as long as it is standing there.</b> Ordered on 25.08.2026
        // and it is not decoration: the block is derived arithmetic and the captain's circuit is derived
        // arithmetic, so without this a rank of six stares off in six directions it happened to arrive
        // facing, and a drill field looks like a queue. Set on the student rather than by the captain
        // because facing is a fact about the bot, and a captain turning six other people's heads by hand is
        // six writes a beat that can go stale the moment anybody steps.
        //
        // Cheap and idempotent: the engine's Direction setter does nothing when the value is unchanged, and
        // a bot that is standing still keeps whatever it was last given. Walking overwrites it, which is
        // correct — a student on its way back to its place should face where it is going.
        var facing = body.GetDirectionTo(master);

        if (body.Direction != facing)
        {
            body.Direction = facing;
        }

        // Nothing left this captain can give. Said as finished rather than failed: the fee bought everything
        // that was for sale.
        if (BotSchool.Lacking(body) == null)
        {
            return BotDoing.Done($"there is nothing more {master.Name} can teach me — {_learned:F1} points");
        }

        Learned(body);

        return BotDoing.Work($"being drilled, {_learned:F1} points so far");
    }

    /// <summary>
    /// What this bot has picked up, measured as the difference from where it stood when it enrolled.
    ///
    /// <para>
    /// The captain adds the points and this only reads them, because a second counter on this side would be
    /// a second opinion about the same event and the two would disagree the first time a beat was missed.
    /// The mark has to be taken per skill: a student that reaches the ceiling in one and is moved on to the
    /// next would otherwise report the whole of the second skill as this afternoon's work.
    /// </para>
    /// </summary>
    private double Learned(BotMobile body)
    {
        var which = Trains;

        if (which == null)
        {
            return _learned;
        }

        if (_marked != which)
        {
            _marked = which;
            _mark = body.Skills[which.Value].Base;

            return _learned;
        }

        // <b>The gain since the mark is taken and the mark moves with it, and the version that did not was
        // counting the same points on every beat.</b> This used to return the running total plus the gap
        // between the skill and a mark that never moved, and the caller assigned that back into the running
        // total — so a student a tenth of a point above its mark was credited a tenth of a point several
        // times a second. One warrior stood on the field reporting 1992.9 points learned, which is two
        // hundred times what a whole lesson can give and about sixty times a grandmaster's entire career.
        //
        // It also broke the one thing that made the number worth printing: at the master's ceiling the gap
        // becomes nought, so the total froze — and "he is learning nothing" and "he has stopped learning"
        // read identically, from a figure that was wrong in both cases.
        var now = body.Skills[which.Value].Base;

        _learned += now - _mark;
        _mark = now;

        return _learned;
    }

    /// <summary>
    /// The walk to this student's station was refused. Enrolled and paid, it stays and is taught from where it stands.
    ///
    /// <para>
    /// <b>The captain's corner, from the other side.</b> The roster's stations are arithmetic round the field at (1460, 1500),
    /// and the same corner that ended classes (see <c>BotLesson.Bend</c>) holds stations with no road: at 03:40:37 on build
    /// 47 Quenna paid 60gp, enrolled, and failed "no way through to (1462, 1499, 0)" three seconds later, and one or two
    /// students a build did the same before. Teaching needs the student inside the block, which it already is; the station
    /// only decides how the block looks and how near the captain passes. Once: a second refusal is the old failure.
    /// </para>
    /// </summary>
    public override bool Bend(IBotWilful bot)
    {
        // An enrolled student is never failed for a road: it has paid, the captain is teaching, and wherever it stands
        // is a place to be taught from. Emrys at 17:01 on 22.09.2026, 6.3 points in, was refused the tile the roster had
        // moved him to and failed "no way through to (1462, 1498, 0)" — booked −60 and no skill, which is the sentence
        // that closes the school. Before enrolling it is an ordinary walk, and an ordinary walk refused fails.
        if (!_enrolled)
        {
            return false;
        }

        if (!_stayPut)
        {
            _stayPut = true;
            Unstationed++;
        }

        return true;
    }

    public override void Drop(IBotWilful bot)
    {
        if (bot?.Self is BotMobile body)
        {
            BotSchool.Leave(body);
        }
    }
}
