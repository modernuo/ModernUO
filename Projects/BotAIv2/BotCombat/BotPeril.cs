using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.BotAI.V2;

/// <summary>
/// Where the shard is dangerous, learned from the only two facts that actually say so: where bots are being
/// hit, and where they are dying.
///
/// <para>
/// <b>Everything else about danger on this shard is a private opinion, and that is why this exists.</b>
/// <see cref="BotLedger"/> already remembers that a piece of work went badly in a place — but it lives on
/// one bot, it is keyed by trade as well as by ground, and it is a record of <em>disappointment</em> rather
/// than of harm. Fifteen bots each learning separately that the same field kills people is fifteen bots
/// learning it fifteen times, none of them able to tell anybody, and a captain deciding where to take a
/// company has to read a fact about the island rather than a mood of its own. So this is deliberately
/// shard-wide, deliberately not keyed by trade, and deliberately fed from the two hooks that cannot lie:
/// <c>OnDamage</c> and <c>OnDeath</c>.
/// </para>
///
/// <para>
/// <b>A frequency, never a total, and the difference decides everything the captain does.</b> A tally that
/// only rises names the graveyard for ever, because the graveyard has always been the worst place on the
/// map and always will be — which is a fact about history and not about tonight. What is wanted is where
/// blood is being spilt <em>lately</em>, so every reading decays towards nothing on its own clock: a square
/// that has gone quiet for an hour stops being the answer without anybody having to clear it, and a square
/// that has just killed two bots outranks one that killed ten yesterday.
/// </para>
///
/// <para>
/// <b>Squares rather than points, because a patrol cannot stand on a coordinate.</b> Harm arrives at exact
/// tiles and is useless at that resolution — thirty single blows scattered over a wood are one dangerous
/// wood, and a map keyed by tile would report thirty places each worth nothing. <see cref="Side"/> is the
/// one number that decides what "a place" means here.
/// </para>
/// </summary>
public static class BotPeril
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotPeril));

    /// <summary>
    /// How many tiles across one square is.
    /// </summary>
    /// <remarks>
    /// Wide enough that a running fight stays inside one, narrow enough that "go and deal with it" names a
    /// walkable errand rather than a region. Twenty-four is about eight seconds' run corner to corner, which
    /// is also roughly how far a squad's scouting knots spread — so a company standing in the middle of a
    /// square covers it, which is the property that makes the number right rather than merely round.
    /// </remarks>
    public static int Side { get; set; } = 24;

    /// <summary>
    /// How long it takes a square's reading to fall by half with nothing further happening in it. Forty minutes.
    ///
    /// <para>
    /// Long enough that a patrol dispatched to a square finds the trouble still there when it arrives, short enough that
    /// last night's massacre does not rule tonight.
    /// </para>
    ///
    /// <para>
    /// <b>Doubled from twenty minutes on 15.09.2026, because the deaths faded before they closed anything.</b> Ground is
    /// closed to all work but flight at <see cref="CloseDeaths"/> deaths lately, summed over nine squares and each fading
    /// by half every half-life. In the plague beast's bog on build 63 four bots had died by 20:14, and companies were still
    /// being raised for its squares at 20:41, while a death twenty minutes old counted for half of one. Fable's first step
    /// for the ground, taken on Patrick's word.
    /// </para>
    /// </summary>
    public static int HalfLifeMs { get; set; } = 2400000;

    /// <summary>What one blow landed on a bot adds to a square.</summary>
    public static double PerBlow { get; set; } = 1.0;

    /// <summary>
    /// What one death adds.
    ///
    /// <para>
    /// Twenty-five blows' worth, and the gap is meant to be enormous. Being hit is what happens all day to a
    /// population that fights for a living — every hunt is a hundred blows and none of them is a problem. A
    /// bot that did not come back is the only unambiguous evidence that a place is beyond whoever went
    /// there. Weighted evenly, the reading would be a map of where bots hunt rather than of where they die,
    /// which is nearly the opposite question.
    /// </para>
    /// </summary>
    public static double PerDeath { get; set; } = 25.0;

    /// <summary>
    /// How high a square's reading has to be before it is worth sending a company to at all.
    ///
    /// <para>
    /// <b>Set at one death, this never fired once, and the counter said so in as many words.</b> Twenty-one
    /// offers to the captain in five minutes, twenty-one of them "found nowhere dangerous enough", on
    /// twenty-seven blows across three squares and no deaths at all. The bar was written as "a death has
    /// happened here", which is a fact worth patrolling but a rare one — this population fights well and
    /// mostly wins. Twelve is a sustained beating instead: at a twenty-minute half-life it takes about four
    /// blows every ten minutes in one square to hold, which is somewhere bots are actually being hurt on a
    /// regular basis. A death still counts double it on its own.
    /// </para>
    /// </summary>
    public static double Worrying { get; set; } = 12.0;

    /// <summary>
    /// How far a square's reading is knocked down when a company has swept it.
    ///
    /// <para>
    /// <b>Not to nothing, and this is the difference between a patrol and a loop.</b> Cleared outright, a
    /// square reads as safe the instant the company arrives, the patrol ends, the captain looks again, and
    /// the same square — still full of whatever was killing people — is once more the worst on the map by
    /// the time the walk home is over. Knocked down, a swept square has to earn its way back up by hurting
    /// somebody again, which is the true test of whether the sweep worked.
    /// </para>
    /// </summary>
    public static double SweptTo { get; set; } = 0.25;

    /// <summary>Most squares remembered at once. Beyond this the calmest is forgotten.</summary>
    public static int MostSquares { get; set; } = 256;

    /// <summary>
    /// How long a square that a company could not work is left off the captain's list.
    ///
    /// <para>
    /// <b>This is the difference between a captain with a map and a captain with one square on it.</b>
    /// <see cref="Worst"/> answers with the highest reading, so a square that cannot be walked stays the
    /// answer for as long as it stays dangerous — and a patrol that fails there is offered the identical
    /// square within the second. Four marches on (1380, 1500) in thirty-six seconds on the night of
    /// 25.08.2026, and the rest of the island unpatrolled the whole while. The reading is <em>not</em>
    /// knocked down here: the square really is dangerous, and saying otherwise would be a lie told to
    /// every bot that reads this map. It is simply not offered again for a while.
    /// </para>
    /// </summary>
    public static int BaulkMs { get; set; } = 600000;

    /// <summary>
    /// How far down the list of dangerous squares a caller may be taken before it is told there is nowhere.
    ///
    /// Five, and it is a bound on work rather than a policy: each step past the first is another scan of the
    /// table, and a captain that would not go to any of the five worst places within reach is a captain with
    /// nothing to do.
    /// </summary>
    public static int Tries { get; set; } = 5;

    private sealed class Square
    {
        public Map Map;

        public int X;

        public int Y;

        public double Reading;

        public long Tick;

        public int Blows;

        public int Deaths;

        /// <summary>The dead, faded on the reading's own half-life and kept apart from the blows. See <see cref="Lethal"/>.</summary>
        public double Dying;

        public long DyingTick;

        /// <summary>Whether anybody has died here at all. A flag, never "is the tick zero".</summary>
        public bool HasDied;

        /// <summary>Whether a company has given this square up. A flag, never "is the tick zero" — see BotJourney.</summary>
        public bool Baulked;

        public long BaulkedTick;

        /// <summary>Whether somewhere in this square has been looked for yet. Terrain does not move, so once.</summary>
        public bool Sounded;

        /// <summary>Whether that search found anywhere at all, and where.</summary>
        public bool Standable;

        public Point3D Foot;
    }

    private static readonly Dictionary<(int Map, int X, int Y), Square> _squares = [];

    /// <summary>
    /// Squares already offered to the caller inside one <see cref="Worst"/> call. Kept between calls rather
    /// than built per call: this runs on the population's beat, and the answer is thrown away every time.
    /// </summary>
    private static readonly HashSet<(int Map, int X, int Y)> _passed = [];

    /// <summary>Blows and deaths ever recorded, for the summary. Not decayed: these are the raw evidence.</summary>
    public static long Blows { get; private set; }

    public static long Deaths { get; private set; }

    public static long Sweeps { get; private set; }

    /// <summary>Squares taken off the board outright by a harrowing. See <see cref="Cleared"/>.</summary>
    public static long Clearances { get; private set; }

    /// <summary>Squares a company set out for and could not work. A named nought: see <see cref="BaulkMs"/>.</summary>
    public static long Baulks { get; private set; }

    /// <summary>
    /// Squares with nowhere in them a body could stand — open water, and very little else.
    ///
    /// <para>
    /// <b>Squares, and counted once each, because the ground is looked at once each.</b> The first version of
    /// this number was times rather than squares and read 1807 in fifty minutes, which is a number about how
    /// often a captain thinks and not about the island at all. Counted so that "the island is quiet" and "the
    /// worst place on it is a coordinate in the sea" stay different facts.
    /// </para>
    /// </summary>
    public static long Unfooted { get; private set; }

    /// <summary>Something hit a bot here.</summary>
    public static void Struck(Map map, Point3D where)
    {
        Blows++;

        Add(map, where, PerBlow, blow: true);
    }

    /// <summary>A bot died here. The only unambiguous evidence this map has.</summary>
    public static void Fell(Map map, Point3D where)
    {
        Deaths++;

        Add(map, where, PerDeath, blow: false);
    }

    private static void Add(Map map, Point3D where, double weight, bool blow)
    {
        if (map == null || map == Map.Internal || weight <= 0.0)
        {
            return;
        }

        var key = Key(map, where);
        var now = Core.TickCount;

        if (!_squares.TryGetValue(key, out var square))
        {
            // Room is made by forgetting the calmest square rather than the oldest. An old square that is
            // still the worst on the map is the one thing this table must never lose.
            if (_squares.Count >= MostSquares)
            {
                Forget(now);
            }

            square = new Square { Map = map, X = key.X, Y = key.Y, Tick = now };
            _squares[key] = square;
        }

        square.Reading = Faded(square, now) + weight;
        square.Tick = now;

        if (blow)
        {
            square.Blows++;
        }
        else
        {
            square.Deaths++;

            square.Dying = DeadLately(square, now) + 1.0;
            square.DyingTick = now;
            square.HasDied = true;
        }
    }

    /// <summary>How many have died in this square lately: every death fades by half each <see cref="HalfLifeMs"/>.</summary>
    private static double DeadLately(Square square, long now)
    {
        if (!square.HasDied)
        {
            return 0.0;
        }

        var since = now - square.DyingTick;

        return since <= 0 ? square.Dying : square.Dying * Math.Pow(0.5, since / (double)HalfLifeMs);
    }

    /// <summary>
    /// The most dangerous square within reach that the caller will actually take, or nothing when the island
    /// is quiet enough to leave alone.
    /// </summary>
    /// <param name="map">Which facet. Squares never compare across one.</param>
    /// <param name="from">Where the company would be setting out from.</param>
    /// <param name="within">How far it is prepared to go, in tiles.</param>
    /// <param name="reading">What that square's reading came to, or nought when nothing is returned.</param>
    public static Point3D Worst(Map map, Point3D from, int within, out double reading) =>
        Worst(map, from, within, out reading, null);

    /// <summary>
    /// The same, with the caller allowed to refuse a square and be offered the next one down.
    ///
    /// <para>
    /// <b>The answer is a place a body can stand, and it did not used to be.</b> A square's middle is
    /// arithmetic — the cell's corner plus half a side — and arithmetic has no height, so this returned
    /// <c>(x, y, 0)</c> for every square on the map. On the plains north of Britain nought is the real
    /// ground and nothing looked wrong; on a hill it is thirty units under the grass, which is further than
    /// <see cref="BotArrival.PersonHeight"/>, so the walk could never arrive. The company marched, stood on
    /// the very tile it had been sent to, was told it had not arrived, redrew its route twelve times without
    /// getting a tile closer to a place it was standing on, and the patrol was failed with "no way through".
    /// Ten of eighteen patrols on the night of 25.08.2026, every one of them on high ground, while every
    /// patrol that finished was on ground that happens to lie at nought. <see cref="BotStep.Settle"/> is how
    /// the rest of this project turns two numbers into a place, and <c>BotHunter.Noisy</c> — reading this
    /// very table for a prowl — was already calling it. One caller settled the height and the other invented
    /// it, from the same square, on the same night.
    /// </para>
    ///
    /// <para>
    /// <b>And a refusal moves down the list rather than ending it.</b> The worst square is an opinion about
    /// danger and says nothing about whether a company can get there; a caller that can see further — a
    /// sealed pocket, a square already given up on — needs somewhere else to be offered, or the one square
    /// it cannot use is the only square it is ever shown.
    /// </para>
    /// </summary>
    /// <param name="fit">Asked of each candidate in turn. Null accepts the first that has ground under it.</param>
    public static Point3D Worst(Map map, Point3D from, int within, out double reading, Func<Point3D, bool> fit)
    {
        reading = 0.0;

        if (map == null || map == Map.Internal)
        {
            return Point3D.Zero;
        }

        var now = Core.TickCount;

        _passed.Clear();

        for (var tries = 0; tries < Tries; tries++)
        {
            (int Map, int X, int Y) at = default;
            Square worst = null;
            var highest = 0.0;

            foreach (var (key, square) in _squares)
            {
                if (square.Map != map || _passed.Contains(key))
                {
                    continue;
                }

                var faded = Faded(square, now);

                if (faded < Worrying || faded <= highest)
                {
                    continue;
                }

                var middle = Middle(square);

                // The larger of the two axes, which is how everything else on this shard measures a distance.
                if (Math.Max(Math.Abs(middle.X - from.X), Math.Abs(middle.Y - from.Y)) > within)
                {
                    continue;
                }

                highest = faded;
                worst = square;
                at = key;
            }

            if (worst == null)
            {
                return Point3D.Zero;
            }

            // Written down before anything can reject it, or the next turn of the loop finds the same square
            // and the caller is offered it five times instead of five squares.
            _passed.Add(at);

            if (worst.Baulked && now - worst.BaulkedTick < BaulkMs)
            {
                continue;
            }

            if (!Footing(map, worst))
            {
                continue;
            }

            var footed = worst.Foot;

            if (fit != null && !fit(footed))
            {
                continue;
            }

            reading = highest;

            return footed;
        }

        return Point3D.Zero;
    }

    /// <summary>
    /// Fewest deaths on a piece of ground before the Baron will take a company to it.
    ///
    /// <para>
    /// One, by order, and the number moved for a reason worth keeping. It was two, on the argument that one
    /// death is somebody's bad afternoon and two is the ground — which reads well and was wrong in practice
    /// for the same reason <see cref="Worrying"/> had to be lowered a day earlier: this population fights
    /// well and rarely dies, so a bar written in deaths is a bar that never clears. An hour of the evening of
    /// 26.08.2026 recorded 451 blows and no deaths at all.
    /// </para>
    ///
    /// <para>
    /// What makes one death safe to act on is that it is <em>one death on ground nobody has dealt with</em>,
    /// and the dead never fade. A square that killed somebody an hour ago is still a square that killed
    /// somebody; the only thing that takes it off the board is a company going there. That is the whole
    /// design, and at a bar of two it could not start.
    /// </para>
    /// </summary>
    public static int Deadly { get; set; } = 1;

    /// <summary>
    /// The ground that has killed the most people within reach, or nothing when no ground has taken
    /// <see cref="Deadly"/> of them.
    ///
    /// <para>
    /// <b>Ranked by the dead and not by the reading, which is the difference between this errand and a
    /// patrol.</b> <see cref="Worst"/> answers "where is it dangerous now" - a decaying frequency, exactly
    /// right for a captain deciding where a company should be standing before anything happens. This answers
    /// "where has it already gone wrong", which does not decay, cannot be reached by a hundred harmless
    /// scratches, and is not satisfied by the ground going quiet on its own. The two rankings disagree
    /// constantly and both are correct about their own question.
    /// </para>
    ///
    /// <para>
    /// <b>Over a neighbourhood, not over one cell, and that scale decides whether this ever fires at all.</b>
    /// A cell is <see cref="Side"/> tiles across because that is the resolution at which "a place" means
    /// something to a map. The company sent here walks a box three times that. Counted per cell, "two have
    /// died in this square" is very nearly unsatisfiable: this population fights well and mostly wins, and one
    /// hour of the evening of 26.08.2026 recorded 451 blows and not one death, while the nine deaths of the
    /// whole evening fell in five different cells with the two nearest of them twenty-seven tiles apart. Two
    /// bots killed twenty-seven tiles apart died on the same ground by any reading a person would give the
    /// word, and both would be inside the box the Baron walks. So the dead are counted over the ground he
    /// will actually work. The same mistake in the other direction is what the captain's own bar was lowered
    /// for - see <see cref="Worrying"/>, which used to be written as "a death has happened here" and never
    /// once fired.
    /// </para>
    /// </summary>
    /// <param name="spread">Half the width of the ground the caller will work. The dead are summed over it.</param>
    /// <param name="deaths">How many died on the ground returned, or nought when nothing is.</param>
    /// <param name="fit">Asked of each candidate in turn. Null accepts the first that has ground under it.</param>
    public static Point3D Deadliest(
        Map map,
        Point3D from,
        int within,
        int spread,
        out int deaths,
        Func<Point3D, bool> fit
    )
    {
        deaths = 0;

        if (map == null || map == Map.Internal)
        {
            return Point3D.Zero;
        }

        var now = Core.TickCount;

        _passed.Clear();

        for (var tries = 0; tries < Tries; tries++)
        {
            (int Map, int X, int Y) at = default;
            Square worst = null;
            var most = Deadly - 1;

            foreach (var (key, square) in _squares)
            {
                // Only a cell that has taken somebody can be the middle of a candidate. Every other cell is
                // reached as a neighbour, and starting from the empty ones would scan the whole table against
                // itself for no extra answer.
                if (square.Map != map || square.Deaths <= 0 || _passed.Contains(key))
                {
                    continue;
                }

                var middle = Middle(square);

                // The larger of the two axes, which is how everything else on this shard measures a distance.
                if (Math.Max(Math.Abs(middle.X - from.X), Math.Abs(middle.Y - from.Y)) > within)
                {
                    continue;
                }

                var total = Around(map, middle, spread);

                if (total <= most)
                {
                    continue;
                }

                most = total;
                worst = square;
                at = key;
            }

            if (worst == null)
            {
                return Point3D.Zero;
            }

            // Written down before anything can reject it, or the next turn of the loop finds the same ground
            // and the caller is offered it five times instead of five places.
            _passed.Add(at);

            if (worst.Baulked && now - worst.BaulkedTick < BaulkMs)
            {
                continue;
            }

            if (!Footing(map, worst))
            {
                continue;
            }

            var footed = worst.Foot;

            if (fit != null && !fit(footed))
            {
                continue;
            }

            deaths = most;

            return footed;
        }

        return Point3D.Zero;
    }

    /// <summary>How many have died within <paramref name="spread"/> tiles of a place, all cells counted.</summary>
    private static int Around(Map map, Point3D middle, int spread)
    {
        var total = 0;

        foreach (var square in _squares.Values)
        {
            if (square.Map != map || square.Deaths <= 0)
            {
                continue;
            }

            var at = Middle(square);

            if (Math.Max(Math.Abs(at.X - middle.X), Math.Abs(at.Y - middle.Y)) <= spread)
            {
                total += square.Deaths;
            }
        }

        return total;
    }

    /// <summary>
    /// How many of the dead within reach still lie on ground nobody has dealt with.
    ///
    /// <para>
    /// The dead are the one figure on this map that never decays and never falls, so this only ever comes
    /// down when somebody harrows the ground they died on - which makes it the single number that answers
    /// both halves of what the Baron is content by: the deaths of others, and the squares nobody has
    /// cleared. One number rather than two, because they are the same fact counted once.
    /// </para>
    /// </summary>
    public static int Unavenged(Map map, Point3D from, int within)
    {
        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        var total = 0;

        foreach (var square in _squares.Values)
        {
            if (square.Map != map || square.Deaths <= 0)
            {
                continue;
            }

            var middle = Middle(square);

            if (Math.Max(Math.Abs(middle.X - from.X), Math.Abs(middle.Y - from.Y)) <= within)
            {
                total += square.Deaths;
            }
        }

        return total;
    }

    /// <summary>
    /// This square is finished with: forgotten outright, dead and all.
    ///
    /// <para>
    /// <b>The one thing <see cref="Swept"/> deliberately refuses to do, and the reasons do not conflict.</b> A
    /// patrol knocks a square down rather than clearing it because a patrol is a company standing in a place
    /// for a while — it proves nothing about whether the place is still full of what was killing people, so
    /// the square has to earn its way back onto the board by hurting somebody rather than by never having
    /// been visited. A harrowing is the opposite errand: twenty things dead or forty minutes of hunting
    /// inside seventy-five tiles, and what it has actually done is spend the square's contents. Leaving the
    /// dead on the count afterwards would send the Baron back to the same coordinates for ever, because the
    /// dead are exactly the number that never decays.
    /// </para>
    ///
    /// <para>
    /// So the row goes. Anything that happens there afterwards writes a new one, from nothing, which is the
    /// honest state of a place nobody has been hurt in since it was dealt with.
    /// </para>
    /// </summary>
    /// <param name="spread">
    /// Half the width of the ground that was worked. Everything inside it goes, not only the cell at the
    /// middle: the company walked the whole box, and clearing one cell of it would offer the Baron the
    /// neighbouring cell of the ground he has just spent the afternoon on.
    /// </param>
    public static int Cleared(Map map, Point3D where, int spread)
    {
        if (map == null || map == Map.Internal)
        {
            return 0;
        }

        _going.Clear();

        var blows = 0;
        var dead = 0;

        foreach (var (key, square) in _squares)
        {
            if (square.Map != map)
            {
                continue;
            }

            var middle = Middle(square);

            if (Math.Max(Math.Abs(middle.X - where.X), Math.Abs(middle.Y - where.Y)) > spread)
            {
                continue;
            }

            _going.Add(key);
            blows += square.Blows;
            dead += square.Deaths;
        }

        if (_going.Count == 0)
        {
            return 0;
        }

        for (var i = 0; i < _going.Count; i++)
        {
            _squares.Remove(_going[i]);
        }

        Clearances += _going.Count;

        logger.Information(
            "The ground around {Where} has been harrowed: {Count} squares off the board altogether, carrying {Blows} blows and {Deaths} deaths between them",
            where,
            _going.Count,
            blows,
            dead
        );

        return _going.Count;
    }

    /// <summary>
    /// Keys being removed by one clearance. Static and reused, like the passed set above: a dictionary cannot
    /// be written to while it is being walked, and a fresh list per harrowing is garbage for nothing.
    /// </summary>
    private static readonly List<(int Map, int X, int Y)> _going = [];

    /// <summary>
    /// A company has been through this square. Knocked down rather than cleared — see <see cref="SweptTo"/>.
    /// </summary>
    public static void Swept(Map map, Point3D where)
    {
        if (map == null || !_squares.TryGetValue(Key(map, where), out var square))
        {
            return;
        }

        Sweeps++;

        var was = Faded(square, Core.TickCount);

        square.Reading = was * SweptTo;
        square.Tick = Core.TickCount;

        // A company has just worked this ground, so whatever it was that could not be walked last time no
        // longer applies. Cleared here rather than left to time out, or a square swept an hour after it was
        // given up on stays off the list for the remainder of its baulk.
        square.Baulked = false;

        logger.Information(
            "The square around {Where} has been swept: it read {Was:F0} and now reads {Now:F0}, on {Blows} blows and {Deaths} deaths",
            where,
            was,
            square.Reading,
            square.Blows,
            square.Deaths
        );
    }

    /// <summary>Squares a scout found quiet and knocked down. See <see cref="Scouted"/>.</summary>
    public static long ScoutedQuiet { get; private set; }

    /// <summary>
    /// A scout watched this square's ground for a minute and saw nothing there that attacks first (07.10.2026, officers20; see
    /// <c>BotGuildScout</c>). Knocked down as a sweep is (<see cref="SweptTo"/>), never cleared: the dead stay on the count, and a square
    /// that hurts somebody again rises again. Returns whether there was a reading to knock down.
    /// </summary>
    public static bool Scouted(Map map, Point3D where)
    {
        if (map == null || !_squares.TryGetValue(Key(map, where), out var square))
        {
            return false;
        }

        var was = Faded(square, Core.TickCount);

        if (was <= 0.0)
        {
            return false;
        }

        square.Reading = was * SweptTo;
        square.Tick = Core.TickCount;
        ScoutedQuiet++;

        logger.Information(
            "A scout found the square around {Where} quiet: it read {Was:F0} and now reads {Now:F0}, on {Blows} blows and {Deaths} deaths",
            where,
            was,
            square.Reading,
            square.Blows,
            square.Deaths
        );

        return true;
    }

    /// <summary>
    /// A company set out for this square and could not work it — no way through to it, or nowhere in it that
    /// could be walked.
    ///
    /// <para>
    /// The reading is left exactly as it stands. This is not a sweep and must never read as one: the square
    /// is every bit as dangerous as it was, and the only thing learned is that this errand does not work.
    /// See <see cref="BaulkMs"/>.
    /// </para>
    ///
    /// <para>
    /// <b>Shard-wide, like everything else in this table, and that is on purpose.</b> The mark is left by a
    /// company but it is a fact about ground, so a lone bot reading this map for somewhere to prowl is kept
    /// off it too — five bots could not walk in there and one will not do better. It lapses on its own, and a
    /// prowl has its own sampled candidates to fall back on, so nothing starves waiting for it.
    /// </para>
    /// </summary>
    public static void Baulked(Map map, Point3D where)
    {
        if (map == null || !_squares.TryGetValue(Key(map, where), out var square))
        {
            return;
        }

        Baulks++;

        square.Baulked = true;
        square.BaulkedTick = Core.TickCount;

        logger.Information(
            "The square around {Where} could not be worked and is off the captain's list for {For} minutes: it still reads {Now:F0}, on {Blows} blows and {Deaths} deaths",
            where,
            BaulkMs / 60000,
            Faded(square, Core.TickCount),
            square.Blows,
            square.Deaths
        );
    }

    /// <summary>What a square reads right now, which is what it read when last touched, faded since.</summary>
    public static double Reading(Map map, Point3D where) =>
        map != null && _squares.TryGetValue(Key(map, where), out var square) ? Faded(square, Core.TickCount) : 0.0;

    /// <summary>Whether ordinary work is kept out of ground where bots have been dying lately. Off, it is weighed as it always was.</summary>
    public static bool KeepsOut { get; set; } = true;

    /// <summary>
    /// How many times the strength that goes there the hostile strength at a place has to be before the place is closed to
    /// every kind of work but running from it.
    ///
    /// <para>
    /// <b>Patrick's answer of 26.09.2026: bots "go round only extremely dangerous places, like the swamp, where the hostility
    /// of the forces is several times theirs".</b> Until then ground was closed by a count of the dead — three lately in a
    /// square and the eight round it closed it to everything but running (<see cref="CloseDeaths"/>), two kept ordinary work
    /// out (<see cref="KeepOutDeaths"/>) — and with the world awake round the bots that closed the ground round the halls:
    /// between 13:16 and 13:54 the auction printed 150 restocks, 84 harrows and 72 reclaims refused as the first veto of a
    /// review, The Lantern's harrow of the ratmen by its own hall among them, while of the 83 bots a creature killed in that
    /// time only 19 had met something three times their company's strength. The dead now say where to look; what closes the
    /// ground is the fight there. Three, the smallest whole "several times".
    /// </para>
    /// </summary>
    public static double Overwhelm { get; set; } = 3.0;

    /// <summary>How far round a place the creatures there are counted. See <see cref="Hostility"/>.</summary>
    public static int HostileReach { get; set; } = 14;

    /// <summary>The grain of that count, in tiles: places in one cell of this side share one reckoning.</summary>
    public static int HostileGrain { get; set; } = 8;

    /// <summary>How long one reckoning of a cell stands. Creatures walk; ten seconds is a few steps of theirs.</summary>
    public static int HostileMs { get; set; } = 10000;

    /// <summary>Offers refused because the hostile strength where they lay was <see cref="Overwhelm"/> times what went. Appraisals.</summary>
    public static long Overwhelmed { get; private set; }

    /// <summary>Cells reckoned afresh, for the cost of it.</summary>
    public static long Reckoned { get; private set; }

    private static readonly Dictionary<(int Map, int X, int Y), (double Fight, long Tick)> _hostility = [];

    /// <summary>
    /// Whether the hostile strength at <paramref name="where"/> is <see cref="Overwhelm"/> times <paramref name="brought"/>
    /// or more, for work that has to go there from <paramref name="from"/>. A bot already within
    /// <see cref="HostileReach"/> of the place is not walking into anything; what it meets there is its fight's business
    /// (<c>BotThreat.Decide</c>, <c>BotFugitive</c>). Counted only when <paramref name="count"/>.
    /// </summary>
    public static bool Overwhelms(Map map, Point3D where, Point3D from, double brought, out double hostile, bool count = true)
    {
        hostile = 0.0;

        if (!KeepsOut || map == null || map == Map.Internal || where == Point3D.Zero || Utility.InRange(from, where, HostileReach))
        {
            return false;
        }

        hostile = Hostility(map, where);

        if (hostile <= 0.0 || hostile < Overwhelm * Math.Max(1.0, brought))
        {
            return false;
        }

        if (count)
        {
            Overwhelmed++;
        }

        return true;
    }

    /// <summary>
    /// The fight a place holds, in <see cref="BotThreat.Power"/>'s units: the wild creatures that attack unprovoked within
    /// <see cref="HostileReach"/> of it now, the strongest in full and the rest at <see cref="BotThreat.Secondary"/> — the
    /// reckoning of a fight <c>BotThreat</c> makes — or what the spawners near keep alive within sight of it
    /// (<c>BotLairs.MetFight</c>), whichever is more. The first sees what wanders; the second what is kept there when it has
    /// wandered off. Kept per cell of <see cref="HostileGrain"/> for <see cref="HostileMs"/>.
    /// </summary>
    public static double Hostility(Map map, Point3D where)
    {
        if (map == null || map == Map.Internal)
        {
            return 0.0;
        }

        var grain = Math.Max(1, HostileGrain);
        var key = (map.MapID, where.X / grain, where.Y / grain);
        var now = Core.TickCount;

        if (_hostility.TryGetValue(key, out var held) && now - held.Tick < HostileMs)
        {
            return held.Fight;
        }

        var middle = new Point3D(key.Item2 * grain + grain / 2, key.Item3 * grain + grain / 2, where.Z);
        var worst = 0.0;
        var sum = 0.0;

        foreach (var creature in map.GetMobilesInRange<BaseCreature>(middle, HostileReach))
        {
            if (creature is not { Deleted: false, Alive: true, Controlled: false, Summoned: false } || creature is BaseVendor
                || creature.FightMode is FightMode.None or FightMode.Aggressor or FightMode.Evil)
            {
                continue;
            }

            var power = BotThreat.Power(creature);

            sum += power;
            worst = Math.Max(worst, power);
        }

        var seen = worst + BotThreat.Secondary * (sum - worst);
        var kept = BotLairs.MetFight(map, middle, BotKept.Sight, out _, out _);
        var fight = Math.Max(seen, kept);

        if (_hostility.Count >= 8192)
        {
            _hostility.Clear();
        }

        _hostility[key] = (fight, now);
        Reckoned++;

        return fight;
    }

    /// <summary>
    /// How many deaths lately, summed over a square and the eight around it, keep ordinary work out. Two, each fading by
    /// half every <see cref="HalfLifeMs"/>: one death is somebody's bad afternoon, two on the same stretch of ground inside
    /// the same twenty minutes is the ground.
    /// </summary>
    public static double KeepOutDeaths { get; set; } = 2.0;

    /// <summary>Offers kept out of ground where bots have been dying lately. Counts appraisals, not decisions.</summary>
    public static long KeptOut { get; private set; }

    /// <summary>
    /// Whether <paramref name="where"/> lies in or beside a square where bots have been dying lately, for work that has to
    /// go there from <paramref name="from"/>.
    ///
    /// <para>
    /// <b>This map was kept for captains and never asked by the auction, and on the evening of 14.09.2026 that was
    /// twenty-eight deaths in twenty minutes.</b> A field north-east of Britain filled with plague beasts and their spawn
    /// after a revel had sent companies into it, and bots went on taking work there - unloading, selling, cooking, mining,
    /// chopping, going back for their own corpses - with every offer reading "safe 1.00", because the only caution the
    /// appraisal read was the bot's own ledger, which is keyed by trade and remembers disappointment rather than harm. This
    /// table had every one of those deaths in it.
    /// </para>
    ///
    /// <para>
    /// <b>The dead and not the reading.</b> Blows are what a population that fights for a living takes all day, and a
    /// hunting ground reads high on them without being a place anybody dies. The deaths fade on the reading's half-life and
    /// are summed over the square and the eight around it, for the reason <see cref="Deadliest"/> gives: two bots killed
    /// thirty tiles apart died on the same ground. Work in the square the bot already stands in is not a walk into anything
    /// and is left alone.
    /// </para>
    /// </summary>
    public static bool Lethal(Map map, Point3D where, Point3D from, out double dead)
    {
        dead = KeepsOut ? DeadAround(map, where, from) : 0.0;

        if (dead < KeepOutDeaths)
        {
            return false;
        }

        KeptOut++;

        return true;
    }

    /// <summary>
    /// How many deaths lately close ground to every kind of work but running away from it.
    ///
    /// <para>
    /// <b>The work that deals with such ground cannot deal with a plague beast.</b> The exemption for fighting work in
    /// <see cref="Lethal"/> rests on that work being the remedy, and on build 47 it was not: companies took on a plague
    /// beast of 19,162 north-east of Britain and the spawn it keeps making at four thousand apiece, and between 04:07 and
    /// 04:20 nine bots died in that field — Fenna and Faron Ashdown and Edda Ashdown mending, Isolde, Faron Ashdown again,
    /// Cassia and Corwin running, Joss Ashdown going to a fire — with 18 lone prowls already kept out of it. Four deaths
    /// lately in a square and the eight round it is past what a company is the answer to; the ground rests on the
    /// deaths' own half-life, and anyone standing in it may still run.
    /// </para>
    ///
    /// <para>
    /// <b>Three since 15.09.2026.</b> The same field did it again on build 63: four bots dead there by 20:14 and some twenty
    /// companies raised for its squares between 20:00 and 20:45, most of them asking no strength at all because the quadrant
    /// record read the bog safe. The third death lately in one place now closes it, and <see cref="HalfLifeMs"/> keeps
    /// those deaths counting twice as long. Fable's first step for the ground, taken on Patrick's word.
    /// </para>
    /// </summary>
    public static double CloseDeaths { get; set; } = 3.0;

    /// <summary>Asks turned away because the ground had been closed to every kind of work. See <see cref="CloseDeaths"/>.</summary>
    public static long Closed { get; private set; }

    /// <summary>Whether ground is closed to every kind of work but flight, for work that has to go there from <paramref name="from"/>.</summary>
    public static bool Closes(Map map, Point3D where, Point3D from, out double dead)
    {
        dead = KeepsOut ? DeadAround(map, where, from) : 0.0;

        if (dead < CloseDeaths)
        {
            return false;
        }

        Closed++;

        return true;
    }

    /// <summary>The deaths lately in the destination's square and the eight round it, or nought for work in the bot's own square.</summary>
    private static double DeadAround(Map map, Point3D where, Point3D from)
    {
        if (map == null || map == Map.Internal || _squares.Count == 0)
        {
            return 0.0;
        }

        var goal = Key(map, where);

        if (goal == Key(map, from))
        {
            return 0.0;
        }

        var now = Core.TickCount;
        var dead = 0.0;

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (_squares.TryGetValue((goal.Map, goal.X + dx, goal.Y + dy), out var square))
                {
                    dead += DeadLately(square, now);
                }
            }
        }

        return dead;
    }

    /// <summary>
    /// Somewhere inside a square that a body can actually stand on, worked out once and kept.
    ///
    /// <para>
    /// <b>The middle of a square is arithmetic, and arithmetic can land on a tree.</b> Asking
    /// <see cref="BotStep.Settle"/> about the middle tile alone and throwing the square away when it says no
    /// puts the fate of five hundred and seventy-six tiles on one of them — a rock, a wall, a trunk, the
    /// corner of somebody's house — and on the first evening it ran, that is what happened: 1807 refusals in
    /// fifty minutes over squares that were perfectly walkable everywhere except the one tile anybody asked
    /// about. This is the same fault as the height that was invented rather than looked up, wearing the
    /// opposite hat, and it was introduced fixing that one.
    /// </para>
    ///
    /// <para>
    /// So the middle first — it is the point the reading is about, and it nearly always answers — and then a
    /// handful of places spread through the square. All of them are well inside it, so what comes back is
    /// still an answer about this square and not about its neighbour.
    /// </para>
    ///
    /// <para>
    /// <b>Looked at once and never again</b>, because terrain does not move. That is what makes the search
    /// affordable and it is also what makes the tally above mean something: a square counts here once, when
    /// it is first considered, rather than on every beat of every captain for the rest of the night.
    /// </para>
    /// </summary>
    private static bool Footing(Map map, Square square)
    {
        if (square.Sounded)
        {
            return square.Standable;
        }

        square.Sounded = true;

        var middle = Middle(square);
        var quarter = Math.Max(1, Side / 4);
        var third = Math.Max(2, Side / 3);

        ReadOnlySpan<(int X, int Y)> offsets =
        [
            (0, 0),
            (-quarter, -quarter),
            (quarter, -quarter),
            (quarter, quarter),
            (-quarter, quarter),
            (-third, 0),
            (third, 0),
            (0, -third),
            (0, third)
        ];

        foreach (var (dx, dy) in offsets)
        {
            var x = middle.X + dx;
            var y = middle.Y + dy;

            if (!BotStep.Settle(map, x, y, out var z))
            {
                continue;
            }

            square.Standable = true;
            square.Foot = new Point3D(x, y, z);

            return true;
        }

        Unfooted++;

        logger.Information(
            "Nowhere in the square around ({X}, {Y}) can be stood on, so nobody will ever be sent there: {Blows} blows and {Deaths} deaths happened in it",
            middle.X,
            middle.Y,
            square.Blows,
            square.Deaths
        );

        return false;
    }

    private static double Faded(Square square, long now)
    {
        var since = now - square.Tick;

        if (since <= 0 || square.Reading <= 0.0)
        {
            return square.Reading;
        }

        // Halving every HalfLifeMs. Written as a power of a half rather than as a subtraction so that a
        // square nobody has touched for a day reads as nearly nothing instead of as a negative number.
        return square.Reading * Math.Pow(0.5, since / (double)HalfLifeMs);
    }

    private static void Forget(long now)
    {
        (int Map, int X, int Y) calmest = default;
        var lowest = double.MaxValue;
        var found = false;

        foreach (var (key, square) in _squares)
        {
            var faded = Faded(square, now);

            if (faded < lowest)
            {
                lowest = faded;
                calmest = key;
                found = true;
            }
        }

        if (found)
        {
            _squares.Remove(calmest);
        }
    }

    private static (int Map, int X, int Y) Key(Map map, Point3D where) =>
        (map.MapID, Cell(where.X), Cell(where.Y));

    /// <summary>
    /// Which square a coordinate falls in.
    ///
    /// Floor division rather than integer division, because the two disagree on negative coordinates and a
    /// map with a negative corner would otherwise fold two squares into one.
    /// </summary>
    private static int Cell(int value) => (int)Math.Floor(value / (double)Side);

    private static Point3D Middle(Square square) =>
        new(square.X * Side + Side / 2, square.Y * Side + Side / 2, 0);

    /// <summary>
    /// The worst squares the map holds, bloodiest first. For the dashboard and nothing else.
    ///
    /// Built fresh on the ask rather than kept in order: it is read when somebody opens a window, and an
    /// ordering nothing looks at between times is bookkeeping the population pays for and never uses.
    /// </summary>
    public static List<(Point3D Where, double Reading, int Blows, int Deaths)> Worst(int most)
    {
        var now = Core.TickCount;

        List<(Point3D Where, double Reading, int Blows, int Deaths)> found = [];

        foreach (var square in _squares.Values)
        {
            var faded = Faded(square, now);

            if (faded <= 0.0)
            {
                continue;
            }

            found.Add((Middle(square), faded, square.Blows, square.Deaths));
        }

        found.Sort((left, right) => right.Reading.CompareTo(left.Reading));

        if (most > 0 && found.Count > most)
        {
            found.RemoveRange(most, found.Count - most);
        }

        return found;
    }

    /// <summary>
    /// What the death map holds about one place, for the console: its square's reading and dead, the dead lately in it and
    /// the eight round it, and what the two rules that read them would say about work there. See <c>BotHand</c>'s peril.
    /// </summary>
    public static string Tell(Map map, Point3D where)
    {
        if (map == null || map == Map.Internal)
        {
            return "no map to read";
        }

        var now = Core.TickCount;
        var key = Key(map, where);
        var dead = 0.0;
        var heaviest = 0.0;
        var heaviestAt = Point3D.Zero;

        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dy = -1; dy <= 1; dy++)
            {
                if (!_squares.TryGetValue((key.Map, key.X + dx, key.Y + dy), out var near))
                {
                    continue;
                }

                var lately = DeadLately(near, now);
                dead += lately;

                if (lately > heaviest)
                {
                    heaviest = lately;
                    heaviestAt = Middle(near);
                }
            }
        }

        var own = _squares.TryGetValue(key, out var square)
            ? $"its square of {Side} reads {Faded(square, now):F1} on {square.Blows} blows and {square.Deaths} dead, {DeadLately(square, now):F1} of them lately"
            : $"its square of {Side} has no record since the shard came up";

        var rule = (KeepsOut, dead) switch
        {
            (false, _) => "the rules that read it are off",
            (_, var d) when d >= CloseDeaths => $"closed to every kind of work but running ({d:F1} dead lately in and round it, against {CloseDeaths:F1})",
            (_, var d) when d >= KeepOutDeaths => $"kept from ordinary work ({d:F1} dead lately in and round it, against {KeepOutDeaths:F1}) and open to fighting work and calls",
            (_, var d) => $"open to every kind of work ({d:F1} dead lately in and round it)"
        };

        var worst = heaviestAt == Point3D.Zero ? "" : $"; the most lately dead round it at {heaviestAt}, {heaviest:F1}";

        return $"({where.X}, {where.Y}): {own}; {rule}{worst}; the dead fade by half every {HalfLifeMs / 60000} minutes, and none of this survives a restart";
    }

    /// <summary>One line: how much harm this map is built on, and where the worst of it is.</summary>
    public static string Describe()
    {
        if (_squares.Count == 0)
        {
            return "nowhere has hurt anybody yet";
        }

        var now = Core.TickCount;
        Square worst = null;
        var reading = 0.0;

        foreach (var square in _squares.Values)
        {
            var faded = Faded(square, now);

            if (faded > reading)
            {
                reading = faded;
                worst = square;
            }
        }

        // <b>How far under the bar it came, because "nowhere is dangerous enough" is two different facts.</b>
        // A map whose worst square reads 11 against a bar of 12 is a bar set a hair too high; a map whose
        // worst square reads 0.4 is a map that is not filling up, and no amount of moving the bar will help
        // it. This sentence used to end at the bar and could not tell the two apart, so the only way to
        // choose between them was to guess at the code.
        if (worst == null || reading < Worrying)
        {
            var quiet = worst == null
                ? "nothing on it reads at all"
                : $"the worst of them is around ({Middle(worst).X}, {Middle(worst).Y}) at {reading:F1} on {worst.Blows} blows and {worst.Deaths} deaths";

            return
                $"{_squares.Count} squares remembered on {Blows} blows and {Deaths} deaths; none of them reads above {Worrying:F0} now, so nowhere needs a company — {quiet}; {Sweeps} swept, {Clearances} harrowed off the board, {Baulks} given up on, {Unfooted} with nowhere in them to stand";
        }

        var middle = Middle(worst);

        return
            $"{_squares.Count} squares remembered on {Blows} blows and {Deaths} deaths; worst is around ({middle.X}, {middle.Y}) at {reading:F0} — {worst.Blows} blows and {worst.Deaths} deaths there; {Sweeps} swept, {Clearances} harrowed off the board, {Baulks} given up on, {Unfooted} with nowhere in them to stand";
    }

    public static void Forget()
    {
        _squares.Clear();
        _passed.Clear();
        _hostility.Clear();
        Overwhelmed = 0;
        Reckoned = 0;
        Blows = 0;
        Deaths = 0;
        Sweeps = 0;
        Clearances = 0;
        Baulks = 0;
        Unfooted = 0;
    }
}
