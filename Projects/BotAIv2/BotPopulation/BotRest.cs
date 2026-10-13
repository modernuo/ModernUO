using System;
using System.Collections.Generic;
using Server.Guilds;
using Server.Logging;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// How long a bot plays before it is tired, and the rest it takes before it plays again.
///
/// <para>
/// <b>Patrick's order of the night of 24.09.2026: bots have their own stamina for the game.</b> One is fresh for five
/// hours, another plays three, another ten — from the worker who plays after his shift to the player who never
/// stops — and then they tire, leave the world, rest for five to eight hours and come back. Everybody tires at a
/// different time, so the island is never empty.
/// </para>
///
/// <para>
/// <b>A tired bot finishes what it is doing, and only then goes.</b> Tiredness takes nothing away: it closes the
/// auction, so the bot is offered nothing new (<see cref="BotWill"/>), and the bot leaves the first time it is free —
/// nothing in hand, in no company, not in a fight, not underground, not in a cell, not in a duel. A bot still not free
/// <see cref="GraceMs"/> after it tired is let go of its work and its company and leaves anyway; a fight, a dungeon and
/// a cell are waited out, because nobody walks out of those.
/// </para>
///
/// <para>
/// <b>Leaving is logging out, the engine's own way.</b> The body goes to <c>Map.Internal</c> with its place written to
/// <c>LogoutLocation</c> — exactly where the engine parks a player with no connection — and its slot in the population
/// becomes a hole (<see cref="BotPopulation.Park"/>), so nothing that walks the population can see it or offer it
/// anything: not the auction, not the companies, not the guilds' muster, not the watchers. It comes back where it left,
/// or at home when that ground no longer takes a body.
/// </para>
///
/// <para>
/// How long a bot plays is a fact about the bot rather than a roll each session: its kind is dealt once, from a deck
/// holding the kinds in their exact shares (<see cref="Deal"/>), and kept, so a keen player stays keen across restarts
/// and the log can say which kind each one is. How long it rests is rolled every time.
/// Play counts only while the bot is in the world and the shard is up; rest counts on the wall clock, shard or no shard,
/// because a player's evening does not stop when the server does. Both are kept in <see cref="BotRestStore"/>.
/// </para>
/// </summary>
public static class BotRest
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotRest));

    /// <summary>Whether bots tire at all. Off, nobody tires and everybody resting comes back at the next look.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often the clock looks. Twenty seconds: a tired bot that has come free stands idle at most this long.</summary>
    public static int LookMs { get; set; } = 20000;

    /// <summary>The shortest rest, in hours. Patrick's five.</summary>
    public static double LeastRestHours { get; set; } = 5.0;

    /// <summary>The longest rest, in hours. Patrick's eight.</summary>
    public static double MostRestHours { get; set; } = 8.0;

    /// <summary>How long a tired bot is given to come free before its work and its company are let go of. An hour.</summary>
    public static int GraceMs { get; set; } = 3600000;

    /// <summary>
    /// The furthest into its first session a bot is put the first time the clock meets it. Eighty bots met at once and
    /// all started fresh would all tire inside the same few hours; spread over most of a session, they leave one by one.
    /// </summary>
    public static double FirstShare { get; set; } = 0.85;

    /// <summary>
    /// The least share of the island that stays in the world, whoever is past their hours. A half.
    ///
    /// <para>
    /// <b>Everybody met at once and started somewhere in a session was a wave, not a spread.</b> At 04:12 on 24.09.2026,
    /// under two hours after the clock started, 22 of 82 were resting and none due back before 08:09: every evening player
    /// leaves within four hours of the start and nobody returns inside five, so by the morning the reckoning came to under
    /// twenty in the world — the empty shard Patrick asked for everybody's hours to be spread against. A bot past its hours
    /// while the island is below this share stays on and works; when it rises again, whoever has played on longest goes first.
    /// The trough of the first cycle is the whole of what it catches; after that the steady share of a mix like this is just
    /// under a half, so it binds lightly.
    /// </para>
    ///
    /// <para>
    /// <b>Since 02.10.2026 the floor is the prime time's target by the hour (<see cref="BotPrime"/>), and this half only when
    /// the schedule is off or no band holds the hour.</b>
    /// </para>
    /// </summary>
    public static double FloorShare { get; set; } = 0.5;

    /// <summary>
    /// Whether a guild in a fight may call its members back from their rest.
    ///
    /// <para>
    /// <b>Patrick's order of 24.09.2026: in a war, a great war, or when the guild's squares are being taken, bots may be
    /// called back from offline to help in the fight.</b> A war declared calls both sides and every ally that joins it
    /// (<c>BotWar.Declare</c>); a claim laid on a square a guild holds calls the holder (<c>BotClaim.Open</c>). The ones
    /// called are the members who have rested longest; a called bot tires on nothing while the fight stands, and
    /// <see cref="CallGraceMs"/> after it is over it goes back to its rest for what it still owed
    /// (<see cref="Record.OwedHours"/>), at least <see cref="CalledLeastRestHours"/>.
    /// </para>
    /// </summary>
    public static bool Calls { get; set; } = true;

    /// <summary>How many members a guild in one war may have back from rest at once.</summary>
    public static int CallMost { get; set; } = 5;

    /// <summary>And in a great war — a guild fighting more than one guild at once, its allies' wars joined to its own.</summary>
    public static int CallMostBig { get; set; } = 10;

    /// <summary>How long a called bot stays once the fight it was called to is over. Ten minutes.</summary>
    public static int CallGraceMs { get; set; } = 600000;

    /// <summary>The least rest a called bot goes back to, whatever it owed. An hour.</summary>
    public static double CalledLeastRestHours { get; set; } = 1.0;

    /// <summary>Bots called back from rest to fight, this session.</summary>
    public static long Called { get; private set; }

    /// <summary>Of those, how many have gone back to their rest.</summary>
    public static long Dismissed { get; private set; }

    /// <summary>How near the end of its rest a bot may be brought back early when the island is under its floor. An hour.</summary>
    public static int EarlyMinutes { get; set; } = 60;

    /// <summary>Bots brought back early to hold the floor, this session.</summary>
    public static long Early { get; private set; }

    /// <summary>The ones that may tire this look: past their hours, or at their bedtime (<see cref="BotDay.AtBed"/>), who go first.</summary>
    private static readonly List<(BotMobile Bot, Record Record, double Over, bool AtBed)> _due = [];

    private static int _heldByFloor;

    /// <summary>Traders past their hours kept in the world this look by their own floor. See <c>BotTrading.RestShare</c> (build 388).</summary>
    private static int _heldTraders;

    /// <summary>Bots past their hours kept in the world this look because a critical post would fall under its floor. See <see cref="BotCritical"/> (02.10.2026).</summary>
    private static int _heldCritical;

    /// <summary>Bots past their hours put off to a later look by the schedule's pace. See <see cref="BotPrime.TirePerLook"/>.</summary>
    private static int _heldPace;

    /// <summary>The island's floor in bots at the last look: the schedule's target, or <see cref="FloorShare"/> of the island.</summary>
    private static int _floor;

    /// <summary>How many traders are resting or already tired, and how many there are in all, in the world and away. See <c>BotTrading.RestShare</c>.</summary>
    private static int Traders(out int all)
    {
        var resting = 0;
        var away = BotPopulation.Away;
        var bots = BotPopulation.Bots;

        all = 0;

        for (var i = 0; i < away.Count; i++)
        {
            if (away[i] is { Deleted: false } bot && BotTrading.Trader(bot))
            {
                all++;
                resting++;
            }
        }

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && bot.Map != null && bot.Map != Map.Internal && BotTrading.Trader(bot))
            {
                all++;

                if (bot.Tired)
                {
                    resting++;
                }
            }
        }

        return resting;
    }

    /// <summary>The kinds of player, with the share of the population each is and how many hours each plays.</summary>
    private static readonly (string Name, double Share, double Least, double Most)[] Kinds =
    [
        ("an evening player", 0.30, 2.5, 4.0),
        ("a steady player", 0.35, 4.0, 6.0),
        ("a keen player", 0.20, 6.0, 8.0),
        ("a player who never stops", 0.15, 9.0, 12.0)
    ];

    internal sealed class Record
    {
        /// <summary>Which of <see cref="Kinds"/> this bot is, dealt once; below nought until it is dealt.</summary>
        public int Kind = -1;

        /// <summary>How many hours it plays in a session, drawn once within its kind's range.</summary>
        public double Hours;

        public double PlayedMinutes;

        /// <summary>When the rest is over; the default while the bot is in the world.</summary>
        public DateTime RestUntil;

        /// <summary>When it last left, for the line that says how long it rested.</summary>
        public DateTime LeftAt;

        public int Sessions;

        /// <summary>The process clock when it tired; not saved, a tick belongs to the process that took it.</summary>
        public long TiredTick;

        /// <summary>The process clock when it was first kept in the world for a critical post, so that is said once; not saved.</summary>
        public long HeldTick;

        /// <summary>The guild that called this bot back from its rest to fight, or null. See <see cref="Calls"/>.</summary>
        public string CalledBy;

        /// <summary>The rest it still owed when it was called, in hours; what it goes back to when the call is over.</summary>
        public double OwedHours;

        /// <summary>The process clock when its call was first found over; not saved.</summary>
        public long CallOverTick;

        /// <summary>Where the bot left the world from: an inn's name, or null for the open ground. See BotInns (29.09.2026).</summary>
        public string Inn;

        /// <summary>What it paid for the bed; nought buys no bonus.</summary>
        public int Paid;

        /// <summary>Until when the rested buff runs, so a restart neither loses it nor grants it twice.</summary>
        public DateTime BuffUntil;

        /// <summary>
        /// Tired at its bedtime before its hours were up (<see cref="BotDay.AtBed"/>), so the next look does not read "under its
        /// hours" and wake it again; cleared when it leaves or comes back. Not saved: a process that restarts reads the hour afresh.
        /// </summary>
        public bool AtBed;

        /// <summary>At its bedtime and held by the floor this spell, so that is counted once a bot and not once a look. Not saved.</summary>
        public bool HeldAtBed;
    }

    /// <summary>Where a record says the bot sleeps, read off the inn's name the leave wrote into it. See <see cref="BotDay"/>.</summary>
    internal static BotBedPlace BedOf(Record record) =>
        record?.Inn == null ? BotBedPlace.Ground
        : record.Inn == "its own house" ? BotBedPlace.Home
        : record.Inn.StartsWith(BotCamp.FirePrefix, StringComparison.Ordinal) ? BotBedPlace.Field
        : BotBedPlace.Inn;

    /// <summary>Who is asleep where, now: the resting bots by the place their record says they left from. For the <c>Day:</c> line.</summary>
    public static void Sleepers(out int home, out int inn, out int field, out int ground)
    {
        home = 0;
        inn = 0;
        field = 0;
        ground = 0;

        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            if (away[i] is not { Deleted: false } bot || !_records.TryGetValue(bot.Name ?? "", out var record) || record.RestUntil == default)
            {
                continue;
            }

            switch (BedOf(record))
            {
                case BotBedPlace.Home:
                    home++;
                    break;
                case BotBedPlace.Inn:
                    inn++;
                    break;
                case BotBedPlace.Field:
                    field++;
                    break;
                default:
                    ground++;
                    break;
            }
        }
    }

    private static readonly Dictionary<string, Record> _records = new(StringComparer.OrdinalIgnoreCase);

    internal static IReadOnlyDictionary<string, Record> Records => _records;

    private static RestTimer _timer;

    private static long _lookedTick;

    /// <summary>Bots that left the world to rest, this session.</summary>
    public static long Left { get; private set; }

    /// <summary>Of those, how many were let go of their work or company after the grace.</summary>
    public static long LetGo { get; private set; }

    /// <summary>Bots that came back from a rest, this session.</summary>
    public static long Returned { get; private set; }

    /// <summary>Of those, how many were put at home because the ground they left would not take a body.</summary>
    public static long ReturnedHome { get; private set; }

    // What held the tired back at the last look, one bucket each; see Holds.
    private static int _tired;
    private static int _heldWorking;
    private static int _heldCompany;
    private static int _heldFight;
    private static int _heldUnder;
    private static int _heldCell;
    private static int _heldDead;

    public static void Start()
    {
        _timer?.Stop();
        _lookedTick = Core.TickCount;
        _timer = new RestTimer(TimeSpan.FromMilliseconds(Math.Max(1000, LookMs)));
        _timer.Start();

        var dealt = Deal();
        var kinds = new int[Kinds.Length];

        foreach (var (_, record) in _records)
        {
            if (record.Kind >= 0 && record.Kind < Kinds.Length)
            {
                kinds[record.Kind]++;
            }
        }

        logger.Information(
            "Bots tire: {Evening} evening players (2.5–4h), {Steady} steady (4–6h), {Keen} keen (6–8h) and {Never} who never stop (9–12h), {Dealt} of them dealt just now — each finishes what it holds, leaves the world and rests {Least}–{Most}h; {Away} are resting now, and a tired bot still not free after {Grace} minutes is let go of its work",
            kinds[0],
            kinds[1],
            kinds[2],
            kinds[3],
            dealt,
            LeastRestHours,
            MostRestHours,
            BotPopulation.Away.Count,
            GraceMs / 60000
        );

        // The schedule and the floors, said at the boot so the live values are on record (02.10.2026). See BotPrime, BotCritical.
        logger.Information(
            "Prime time {State}: {Schedule} of the island in the world, local time, the target moving over {Ramp} minutes at a band's start, at most {Tire} tired and {Back} brought back a look; without it the floor is {Floor:P0}; critical floors {Critical}: {Floors}, filled from rest up to {Early} minutes early",
            BotPrime.Running ? "ON" : "OFF",
            BotPrime.Schedule(),
            BotPrime.RampMinutes,
            BotPrime.TirePerLook,
            BotPrime.ReturnPerLook,
            FloorShare,
            BotCritical.Running ? "ON" : "OFF",
            Floors(),
            BotCritical.EarlyMinutes
        );
    }

    /// <summary>The critical floors as configured, for the boot line.</summary>
    private static string Floors()
    {
        var posts = BotCritical.Posts;

        if (posts.Count == 0)
        {
            return "none";
        }

        using var line = ValueStringBuilder.Create(160);

        for (var i = 0; i < posts.Count; i++)
        {
            if (i > 0)
            {
                line.Append(", ");
            }

            line.Append(posts[i]);
            line.Append(' ');
            line.Append(BotCritical.Floor(posts[i]));
        }

        return line.ToString();
    }

    /// <summary>
    /// A newcomer born in the burst (<see cref="BotGrowth.FillTo"/>) is put somewhere into its first session, as the ones met
    /// at a boot are (<see cref="FirstShare"/>), so a hundred raised in seven minutes do not tire inside the same few hours. A
    /// newcomer born on its own still starts fresh.
    /// </summary>
    internal static void Spread(BotMobile bot)
    {
        if (bot is not { Deleted: false } || string.IsNullOrEmpty(bot.Name))
        {
            return;
        }

        var record = Of(bot);

        if (record.Sessions == 0 && record.RestUntil == default)
        {
            record.PlayedMinutes = record.Hours * 60 * Utility.RandomDouble() * Math.Clamp(FirstShare, 0, 1);
        }
    }

    /// <summary>
    /// Deals a kind to every bot that has none, from a deck holding the kinds in their exact shares, so a population
    /// met all at once is the mix it is meant to be rather than whatever eighty coin tosses come to.
    ///
    /// <para>
    /// <b>The first version read the kind off the name, and eighty names came out 35 evening players where 24 were
    /// meant</b> (22 steady, 10 keen, 13 never-stops, on 24.09.2026 at 02:09): a population that would have spent the
    /// night half empty by accident. A kind dealt once is kept in the record, so it is as stable across restarts as
    /// the name was.
    /// </para>
    /// </summary>
    private static int Deal()
    {
        List<(string Name, Record Record)> undealt = [];

        void Collect(IReadOnlyList<BotMobile> bots)
        {
            for (var i = 0; i < bots.Count; i++)
            {
                if (bots[i] is not { Deleted: false } bot || string.IsNullOrEmpty(bot.Name))
                {
                    continue;
                }

                if (!_records.TryGetValue(bot.Name, out var record))
                {
                    record = new Record();
                    _records[bot.Name] = record;
                    undealt.Add((bot.Name, record));
                }
                else if (record.Kind < 0 || record.Kind >= Kinds.Length)
                {
                    undealt.Add((bot.Name, record));
                }
            }
        }

        Collect(BotPopulation.Bots);
        Collect(BotPopulation.Away);

        if (undealt.Count == 0)
        {
            return 0;
        }

        // The deck: each kind's exact share of the batch, the remainders to the largest fractions.
        var counts = new int[Kinds.Length];
        var given = 0;

        for (var k = 0; k < Kinds.Length; k++)
        {
            counts[k] = (int)Math.Floor(Kinds[k].Share * undealt.Count);
            given += counts[k];
        }

        while (given < undealt.Count)
        {
            var best = 0;
            var bestFraction = -1.0;

            for (var k = 0; k < Kinds.Length; k++)
            {
                var fraction = Kinds[k].Share * undealt.Count - counts[k];

                if (fraction > bestFraction)
                {
                    bestFraction = fraction;
                    best = k;
                }
            }

            counts[best]++;
            given++;
        }

        List<int> deck = [];

        for (var k = 0; k < Kinds.Length; k++)
        {
            for (var n = 0; n < counts[k]; n++)
            {
                deck.Add(k);
            }
        }

        for (var i = deck.Count - 1; i > 0; i--)
        {
            var j = Utility.Random(i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        for (var i = 0; i < undealt.Count; i++)
        {
            var (_, record) = undealt[i];
            var fresh = record.RestUntil == default;

            record.Kind = deck[i];
            record.Hours = Kinds[deck[i]].Least + (Kinds[deck[i]].Most - Kinds[deck[i]].Least) * Utility.RandomDouble();

            // Somewhere into its session, so the ones met together leave one by one. That goes for a record read back
            // from the first shape too, whose play was measured against a kind read off the name; a bot resting keeps
            // its rest.
            if (fresh)
            {
                record.PlayedMinutes = record.Hours * 60 * Utility.RandomDouble() * Math.Clamp(FirstShare, 0, 1);
            }
        }

        return undealt.Count;
    }

    /// <summary>A kind drawn by the shares, for a bot met on its own — a newcomer — rather than in a batch.</summary>
    private static int Draw()
    {
        var roll = Utility.RandomDouble();
        var sum = 0.0;

        for (var k = 0; k < Kinds.Length; k++)
        {
            sum += Kinds[k].Share;

            if (roll < sum)
            {
                return k;
            }
        }

        return Kinds.Length - 1;
    }

    public static void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    /// <summary>Which kind of player this is, by name, for the log.</summary>
    private static string KindName(Record record) =>
        record.Kind >= 0 && record.Kind < Kinds.Length ? Kinds[record.Kind].Name : "a player";

    /// <summary>Whether a bot of this name is resting right now.</summary>
    public static bool Resting(string name) =>
        Running && name != null && _records.TryGetValue(name, out var record) && record.RestUntil > Core.Now;

    /// <summary>The bot's record; a bot met for the first time on its own — a newcomer — starts a fresh session.</summary>
    private static Record Of(BotMobile bot)
    {
        if (!_records.TryGetValue(bot.Name, out var record))
        {
            record = new Record();
            _records[bot.Name] = record;
        }

        if (record.Kind < 0 || record.Kind >= Kinds.Length)
        {
            record.Kind = Draw();
            record.Hours = Kinds[record.Kind].Least + (Kinds[record.Kind].Most - Kinds[record.Kind].Least) * Utility.RandomDouble();
        }

        return record;
    }

    private static void Look()
    {
        var tick = Core.TickCount;

        // Capped, so a loop that stalled for a minute does not age everybody by it.
        var minutes = Math.Clamp((tick - _lookedTick) / 60000.0, 0, 2.0);

        _lookedTick = tick;

        _tired = 0;
        _heldWorking = 0;
        _heldCompany = 0;
        _heldFight = 0;
        _heldUnder = 0;
        _heldCell = 0;
        _heldDead = 0;

        var bots = BotPopulation.Bots;

        // The ones past their hours, and how many of the ones in the world may be tired at once. See FloorShare.
        _due.Clear();
        _heldByFloor = 0;
        _heldTraders = 0;

        var tiredAlready = 0;
        var onCall = 0;

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false } || string.IsNullOrEmpty(bot.Name) || bot.Map == null || bot.Map == Map.Internal)
            {
                continue;
            }

            var record = Of(bot);

            // In the world is not resting, whatever an old record says.
            record.RestUntil = default;

            if (!Running)
            {
                bot.Tired = false;
                continue;
            }

            // On a call: it plays no hours, and tires when the fight it was called to is over and a grace has passed.
            if (record.CalledBy != null)
            {
                onCall++;

                if (CallStands(record.CalledBy))
                {
                    record.CallOverTick = 0;
                    continue;
                }

                if (record.CallOverTick == 0)
                {
                    record.CallOverTick = tick;
                }

                if (!bot.Tired && tick - record.CallOverTick >= CallGraceMs)
                {
                    bot.Tired = true;
                    record.TiredTick = tick;

                    logger.Information(
                        "{Name} the {Class}: the fight {Guild} called it back for is over; it goes back to its rest when it is free",
                        bot.Name,
                        bot.Class?.Name,
                        record.CalledBy
                    );
                }

                continue;
            }

            // <b>A bot sitting at a lit campfire rests there at the inn's rate instead of playing (Patrick's point 6 of
            // 29.09.2026).</b> Plain minutes for everybody else. See BotCamp.Played.
            // And at a cook's table after supper, at the tavern's own rate (07.10.2026, trades17). See BotTaverns.Played.
            record.PlayedMinutes = Math.Max(0.0, record.PlayedMinutes + (BotTaverns.Played(bot, minutes, record.Hours) ?? BotCamp.Played(bot, minutes, record.Hours)));

            var over = record.PlayedMinutes - record.Hours * 60;

            // <b>The daily cycle chooses who, not how many (PLAN-life A1).</b> A bot at its bedtime that has played enough of its
            // hours may tire before they are up, and goes ahead of the ones merely over their hours in the queue below; the
            // allowance that caps how many tire a look is untouched. One tired that way stays tired (AtBed) until it leaves.
            var atBed = BotDay.AtBed(bot, record.Hours > 0 ? record.PlayedMinutes / (record.Hours * 60) : 0.0, out _);

            if (!atBed)
            {
                record.HeldAtBed = false;
            }

            if (over < 0 && !atBed && !(bot.Tired && record.AtBed))
            {
                bot.Tired = false;
                record.TiredTick = 0;
                record.AtBed = false;
                continue;
            }

            if (bot.Tired)
            {
                tiredAlready++;
            }
            else
            {
                _due.Add((bot, record, over, atBed));
            }
        }

        // <b>The ones back on a call are over the floor, not part of it.</b> Counted in, they let the floor send as many
        // tired bots to bed while the fight lasted, and when the fight was over they went back to their rest too: on
        // build 208, at 10:58 on 24.09.2026, 37 of 88 were in the world against a floor of 44, after The Blade's and The
        // Lantern's calls of 09:52 had come and gone.
        var island = BotPopulation.Count + BotPopulation.Away.Count;

        // <b>The floor is the prime time's target now (02.10.2026, Patrick: 100 % by night, 75 % by day, 50 % by evening), and
        // FloorShare only when the schedule is off.</b> One number for both directions: over it the ones past their hours go,
        // under it the resting come back (below). See BotPrime.
        var share = BotPrime.Share(Core.Now, out var band, out var ramping);

        _floor = (int)Math.Ceiling(share * island);

        var allowance = BotPopulation.Count - onCall - _floor - tiredAlready;

        // <b>The traders have a floor of their own (01.10.2026, build 388; Patrick's word of 01.10: at least half of the traders
        // in the world).</b> Resting or already tired, no more than BotTrading.RestShare of them at once; the one past its hours
        // beyond that plays on, as the island's floor keeps bots on, and is counted.
        var tradersResting = Traders(out var tradersAll);
        var tradersMayRest = (int)Math.Floor(Math.Clamp(BotTrading.RestShare, 0, 1) * tradersAll);

        // <b>And the critical posts (02.10.2026): who fills what, in the world and not tired, before anybody is tired this
        // look.</b> A bot whose post would fall under its floor plays on. See BotCritical.
        BotCritical.Count();
        _heldCritical = 0;
        _heldPace = 0;

        var tiredNow = 0;

        // Longest past their hours first: when the floor binds, those who have played on the most go before the rest. And since
        // the daily cycle, the ones whose bedtime it is before any of them, so who is offline makes sense (BotDay).
        _due.Sort((a, b) => a.AtBed != b.AtBed ? b.AtBed.CompareTo(a.AtBed) : b.Over.CompareTo(a.Over));

        for (var i = 0; i < _due.Count; i++)
        {
            var (bot, record, over, atBed) = _due[i];

            if (allowance <= 0)
            {
                _heldByFloor++;

                // Once a bot a spell of being held, not once a look (review A of 13.10.2026, §3.2).
                if (atBed && !record.HeldAtBed)
                {
                    record.HeldAtBed = true;
                    BotDay.HeldPastBed++;
                }

                continue;
            }

            // The schedule's pace: so many a look at most, whatever the ramp lets through. See BotPrime.TirePerLook.
            if (BotPrime.Running && tiredNow >= Math.Max(1, BotPrime.TirePerLook))
            {
                _heldPace++;
                BotPrime.TiresPaced++;
                continue;
            }

            var trader = BotTrading.Trader(bot);

            if (trader && tradersResting >= tradersMayRest)
            {
                _heldTraders++;
                BotTrading.RestHeld++;
                continue;
            }

            if (BotCritical.Holds(bot, out var post))
            {
                _heldCritical++;
                BotCritical.Held++;

                // Once per bot per spell of being held, not once a look.
                if (record.HeldTick == 0)
                {
                    record.HeldTick = tick;

                    logger.Information(
                        "{Name} the {Class} is past its hours but plays on: the island would be under its {Floor} {Post}(s) without it",
                        bot.Name,
                        bot.Class?.Name,
                        BotCritical.Floor(post),
                        post
                    );
                }

                continue;
            }

            if (trader)
            {
                tradersResting++;
            }

            allowance--;
            tiredNow++;
            bot.Tired = true;
            record.TiredTick = tick;
            record.HeldTick = 0;
            record.AtBed = atBed && over < 0;
            record.HeldAtBed = false;
            BotCritical.Tired(bot);

            if (atBed)
            {
                BotDay.ToBedOnTime++;

                if (over < 0)
                {
                    BotDay.ToBedEarly++;
                }
            }

            logger.Information(
                "{Name} the {Class}, {Kind}, is tired after {Played:0.0}h of play{Bed} and takes on nothing new; it leaves when it is free",
                bot.Name,
                bot.Class?.Name,
                KindName(record),
                record.PlayedMinutes / 60.0,
                atBed ? (over < 0 ? " - its bedtime, before its hours" : " - its bedtime") : ""
            );
        }

        for (var i = 0; i < bots.Count; i++)
        {
            var bot = bots[i];

            if (bot is not { Deleted: false, Tired: true } || bot.Map == null || bot.Map == Map.Internal
                || !_records.TryGetValue(bot.Name ?? "", out var record))
            {
                continue;
            }

            _tired++;

            if (record.TiredTick == 0)
            {
                record.TiredTick = tick;
            }

            var held = Holds(bot);

            if (held == null)
            {
                // <b>A bot secure at a campfire in the wild sleeps where it sits, unpaid (Patrick's point 6 of 29.09.2026).</b>
                // See BotCamp.Beds.
                if (BotCamp.Beds(bot))
                {
                    Leave(bot, record, null);
                    continue;
                }

                // A bot with a house of its own goes home first and leaves the world from under its own roof. See
                // BotRepose.HomeToRest (Patrick's order of 26.09.2026: houses to pass the time in).
                if (BotRepose.HomeToRest(bot))
                {
                    continue;
                }

                // A bot whose timetable says the field sleeps by a fire before any inn is looked for (BotDay, PLAN-life A1); a
                // miss here is its own count, not the open ground's.
                if (BotDay.PrefersField(bot) && BotCamp.ToFire(bot, fieldFirst: true))
                {
                    BotDay.FieldFirst++;
                    continue;
                }

                // Or to the nearest inn or tavern, and the bed paid for. Patrick's order of 29.09.2026: "bots cannot simply go
                // offline". See BotInns.ToInn; a bot with no inn within reach leaves where it stands, and is counted.
                if (BotInns.ToInn(bot, RestHours(record)))
                {
                    continue;
                }

                // Or to a campfire, or to make one, before the open ground. See BotCamp.ToFire.
                if (BotCamp.ToFire(bot))
                {
                    continue;
                }

                Leave(bot, record, null);
            }
            else if (tick - record.TiredTick >= GraceMs && held is "working" or "in a company")
            {
                Leave(bot, record, held);
            }
        }

        var away = BotPopulation.Away;
        var now = Core.Now;

        for (var i = away.Count - 1; i >= 0; i--)
        {
            var bot = away[i];

            if (bot is not { Deleted: false })
            {
                continue;
            }

            _records.TryGetValue(bot.Name ?? "", out var record);

            if (!Running || record == null || now >= record.RestUntil)
            {
                Return(bot, record);
            }
        }

        // <b>Under the floor, those whose rest is nearly over come back early.</b> The floor only ever kept bots from
        // leaving; once the island was under it — after a call's bots went back to bed, or a wave of rests begun together —
        // nothing brought it back up before the rests ran out on their own. The ones due back soonest, within
        // EarlyMinutes, fill the gap, soonest first; nobody's rest is cut by more than that.
        //
        // <b>With the prime time on, the window is the whole rest (02.10.2026).</b> The night's target is the whole island,
        // and a bot that left at 17:00 for seven hours is wanted at midnight: whoever is due back soonest comes, whatever it
        // still owed, ReturnPerLook a look — three a minute against a ramp that asks about three — so the night fills over
        // the ramp rather than in one look. Everybody comes back with a fresh session, as any returning bot does.
        if (Running)
        {
            var present = BotPopulation.Count - onCall;
            var floor = _floor;
            var scheduled = BotPrime.Running;
            var window = scheduled ? double.MaxValue : EarlyMinutes;
            var most = scheduled ? Math.Max(1, BotPrime.ReturnPerLook) : int.MaxValue;

            if (present < floor)
            {
                List<(BotMobile Bot, Record Record, bool Awake)> soon = [];

                for (var i = 0; i < away.Count; i++)
                {
                    if (away[i] is { Deleted: false } bot && _records.TryGetValue(bot.Name ?? "", out var record)
                        && record.RestUntil != default && (record.RestUntil - now).TotalMinutes <= window)
                    {
                        soon.Add((bot, record, BotDay.Awake(bot)));
                    }
                }

                // Whose wake hour has come first (BotDay), then soonest due back, as before.
                soon.Sort((a, b) => a.Awake != b.Awake ? b.Awake.CompareTo(a.Awake) : a.Record.RestUntil.CompareTo(b.Record.RestUntil));

                var came = 0;

                for (var i = 0; i < soon.Count && present < floor && came < most; i++)
                {
                    var (bot, record, awake) = soon[i];
                    var early = (record.RestUntil - now).TotalMinutes;

                    if (awake)
                    {
                        BotDay.WokeOnTime++;
                    }

                    if (scheduled && early > EarlyMinutes)
                    {
                        Return(bot, record, $"back {early:0} minutes early: prime time ({band?.ToString() ?? "no band"}{(ramping ? ", ramping" : "")}) wants {floor} of the island's {island} in the world and it had {present}");
                        BotPrime.Returned++;
                    }
                    else
                    {
                        Return(bot, record, $"back {early:0} minutes early: the island was under its floor of {floor}");
                        Early++;
                    }

                    present++;
                    came++;
                }

                if (scheduled && present < floor && came >= most && soon.Count > came)
                {
                    BotPrime.ReturnsPaced++;
                }
            }

            BotPrime.Read(band, share, ramping, island, floor, BotPopulation.Count);
        }

        // <b>A critical post under its floor brings its own back, whatever they owed (02.10.2026).</b> The gate above keeps a
        // post from falling under its floor from inside; from outside — a restart that found the smiths resting, a call's
        // bots gone back to bed, a death — the one due back soonest comes, up to BotCritical.EarlyMinutes into its rest, as
        // many as the post is short. See BotCritical.
        if (Running && BotCritical.Running)
        {
            var posts = BotCritical.Posts;

            for (var p = 0; p < posts.Count; p++)
            {
                var post = posts[p];
                var shortBy = BotCritical.Short(post);

                if (shortBy <= 0)
                {
                    continue;
                }

                List<(BotMobile Bot, Record Record)> own = [];

                for (var i = 0; i < away.Count; i++)
                {
                    if (away[i] is { Deleted: false } bot && _records.TryGetValue(bot.Name ?? "", out var record)
                        && record.RestUntil != default && record.CalledBy == null
                        && (record.RestUntil - now).TotalMinutes <= BotCritical.EarlyMinutes && BotCritical.Fills(bot, post))
                    {
                        own.Add((bot, record));
                    }
                }

                if (own.Count == 0)
                {
                    continue;
                }

                own.Sort((a, b) => a.Record.RestUntil.CompareTo(b.Record.RestUntil));

                for (var i = 0; i < own.Count && shortBy > 0; i++)
                {
                    var (bot, record) = own[i];
                    var early = (record.RestUntil - now).TotalMinutes;

                    Return(bot, record, $"back {early:0} minutes early: the island had {BotCritical.Present(post)} of its {BotCritical.Floor(post)} {post}(s)");

                    BotCritical.Brought++;
                    shortBy--;
                }
            }
        }

        // <b>And the traders' own floor brings a trader back early when more than its share are resting (build 388).</b> The gate
        // above keeps the share from being passed from inside; passed from outside — a restart that found four of seven away, as
        // 23:14 on 387 did — the trader due back soonest comes back, within BotTrading.RestEarlyMinutes of its time, one a look.
        if (Running && BotTrading.RestShare < 1.0)
        {
            var resting = Traders(out var all);
            var mayRest = (int)Math.Floor(Math.Clamp(BotTrading.RestShare, 0, 1) * all);

            if (resting > mayRest)
            {
                BotMobile soonest = null;
                Record soonestRecord = null;

                for (var i = 0; i < away.Count; i++)
                {
                    if (away[i] is { Deleted: false } bot && BotTrading.Trader(bot) && _records.TryGetValue(bot.Name ?? "", out var record)
                        && record.RestUntil != default && (record.RestUntil - now).TotalMinutes <= BotTrading.RestEarlyMinutes
                        && (soonestRecord == null || record.RestUntil < soonestRecord.RestUntil))
                    {
                        soonest = bot;
                        soonestRecord = record;
                    }
                }

                if (soonest != null)
                {
                    var early = (soonestRecord.RestUntil - now).TotalMinutes;

                    Return(soonest, soonestRecord, $"back {early:0} minutes early: more than {BotTrading.RestShare:P0} of the traders were resting");
                    BotTrading.RestEarly++;
                }
            }
        }

        // Every fight that stands keeps its call topped up, not only the moment it began: a war read back off the save
        // at a boot, or a call whose bots have fallen, is answered here. Call counts the ones already back and brings
        // nobody past the most.
        if (Running && Calls)
        {
            foreach (var war in BotWar.Standing)
            {
                BotRest.Call(BotGuilds.Named(war.A), $"its war with {war.B}");
                BotRest.Call(BotGuilds.Named(war.B), $"its war with {war.A}");
            }

            foreach (var bid in BotClaim.Bids)
            {
                if (bid.From != null && BotClaim.Important(bid.From, bid.Map, bid.Middle))
                {
                    BotRest.Call(BotGuilds.Named(bid.From), $"{bid.Guild} laying claim to its square at {bid.Middle.X},{bid.Middle.Y}");
                }
            }
        }

        BotGrowth.Look(now);
    }

    /// <summary>What keeps a tired bot from leaving this minute, or null when nothing does.</summary>
    private static string Holds(BotMobile bot)
    {
        if (!bot.Alive || bot.Fallen)
        {
            _heldDead++;
            return "dead";
        }

        if (BotOutlaw.Jailed(bot))
        {
            _heldCell++;
            return "in a cell";
        }

        if (BotDelveParty.Delving(bot) || BotDungeon.Under(bot.Location))
        {
            _heldUnder++;
            return "underground";
        }

        if (BotDuel.Duelling(bot) || bot.Resolve.Standing is BotStanding.Failing or BotStanding.Hunted)
        {
            _heldFight++;
            return "in a fight";
        }

        if (bot.Squad != null)
        {
            _heldCompany++;
            return "in a company";
        }

        if (bot.Resolve.Deed != null)
        {
            _heldWorking++;
            return "working";
        }

        return null;
    }

    private static void Leave(BotMobile bot, Record record, string letGoOf)
    {
        if (letGoOf != null)
        {
            // A company is stationed on its leader, so a leader that goes breaks the company up rather than leaving
            // it to form on a body that is no longer in the world.
            if (bot.Squad is { } company)
            {
                if (ReferenceEquals(company.Leader, bot))
                {
                    BotSquads.Disband(company, $"{bot.Name} was too tired to lead it");
                }
                else
                {
                    BotSquads.Leave(bot);
                }
            }

            if (bot.Resolve.Deed != null)
            {
                bot.Journey.Discard();
                BotWill.PutDown(bot, "too tired to go on");
            }

            LetGo++;
        }

        var where = bot.Location;
        var map = bot.Map;
        var played = record.PlayedMinutes / 60.0;
        var called = record.CalledBy;
        var rest = called != null
            ? Math.Max(CalledLeastRestHours, record.OwedHours)
            : LeastRestHours + Utility.RandomDouble() * Math.Max(0, MostRestHours - LeastRestHours);

        if (called != null)
        {
            record.CalledBy = null;
            record.OwedHours = 0;
            record.CallOverTick = 0;
            Dismissed++;
        }

        bot.Journey.Discard();
        bot.Combatant = null;
        bot.Tired = false;

        record.PlayedMinutes = 0;
        record.TiredTick = 0;
        record.AtBed = false;
        record.HeldAtBed = false;
        record.Sessions++;
        record.LeftAt = Core.Now;
        record.RestUntil = Core.Now + TimeSpan.FromHours(rest);

        bot.LogoutLocation = where;
        bot.LogoutMap = map;

        // From an inn, or from the open ground; and whether the bed was paid for, which is what buys the bonus on return.
        var inn = BotInns.Settle(bot, out var paid);

        // The inn's name, and " after supper" when the bot ate at a cook's tavern before it slept (07.10.2026, trades17).
        var from = BotTaverns.Supped(bot, inn?.Name);

        // <b>Or from under its own roof, which is a paid night nobody pays for (Patrick's first addition of 29.09.2026).</b>
        if (inn == null && BotAbode.Of(bot) is { Deleted: false } house && house.Map == map && house.IsInside(bot))
        {
            from = "its own house";
            paid = 1;
            BotInns.NotedAtHome();
        }

        // <b>Or from beside a campfire in the wild, unpaid (Patrick's point 6 of 29.09.2026).</b> See BotCamp.Settle.
        var fire = inn == null && from == null ? BotCamp.Settle(bot) : null;

        if (fire != null)
        {
            from = fire;
        }

        record.Inn = from;
        record.Paid = paid;

        BotPopulation.Park(bot);
        bot.Internalize();

        Left++;

        logger.Information(
            "{Name} the {Class}, {Kind}, leaves the world at {Where}{Inn} after {Played:0.0}h of play{How} and rests {Rest:0.0}h, back about {Back:HH:mm}",
            bot.Name,
            bot.Class?.Name,
            KindName(record),
            where,
            from == null ? " on the open ground" : fire != null ? $" by {fire}, unpaid" : inn == null ? " in its own house" : paid > 0 ? $" in {inn.Name}, a bed paid for at {paid}gp" : $" in {inn.Name}, the bed unpaid",
            played,
            (called == null ? "" : $", the fight {called} called it back for being over")
            + (letGoOf == null ? "" : $", let go of what held it an hour ({letGoOf})"),
            rest,
            record.RestUntil.ToLocalTime()
        );
    }

    /// <summary>The hours this rest will run, for the bed's price: what the record says when it is set, else the least rest.</summary>
    private static double RestHours(Record record)
    {
        if (record == null)
        {
            return LeastRestHours;
        }

        if (record.CalledBy != null)
        {
            return Math.Max(CalledLeastRestHours, record.OwedHours);
        }

        return (LeastRestHours + MostRestHours) / 2.0;
    }

    private static void Return(BotMobile bot, Record record, string call = null)
    {
        var rested = record == null || record.LeftAt == default ? 0.0 : (Core.Now - record.LeftAt).TotalHours;

        if (record != null)
        {
            record.RestUntil = default;
            record.PlayedMinutes = 0;
            record.TiredTick = 0;
            record.AtBed = false;
            record.HeldAtBed = false;
        }

        bot.Tired = false;
        bot.Refusals = 0;
        bot.ReviveComplained = false;

        var own = BotPopulation.Unpark(bot);

        Returned++;

        if (!own)
        {
            ReturnedHome++;
        }

        // Back among the posts it fills, so a post counted short earlier this look is not filled twice. See BotCritical.
        BotCritical.Back(bot);

        // <b>A paid night at an inn buys a third of it back as regeneration, and the day's boredom falls.</b> Patrick's order
        // of 29.09.2026. Only when the rest actually ran — a bot called back to a war has slept nothing — and only once: the
        // buff's end is written into the record, so a restart neither loses it nor grants it again.
        var buff = "";

        if (record is { Paid: > 0, Inn: not null } && call == null && rested > 0.0)
        {
            // A night after supper buys a longer buff (07.10.2026, trades17). See BotTaverns.SupperFactor.
            BotInns.Rested(bot, rested * BotTaverns.SupperFactor(record.Inn), out var until);
            record.BuffUntil = until;
            buff = $", rested well at {record.Inn}: regenerating until {until.ToLocalTime():HH:mm}";
        }
        // A night by a campfire buys a smaller share (Patrick's point 6 of 29.09.2026). See BotCamp.Rested.
        else if (call == null && rested > 0.0 && BotCamp.Rested(bot, record?.Inn, rested, out var fireUntil))
        {
            record.BuffUntil = fireUntil;
            buff = $", rested by {record.Inn}: regenerating until {fireUntil.ToLocalTime():HH:mm}";
        }
        else if (record is { BuffUntil: var kept } && kept > Core.Now)
        {
            bot.WellRestedUntil = kept;
        }

        if (record != null)
        {
            record.Inn = null;
            record.Paid = 0;
        }

        logger.Information(
            "{Name} the {Class} is back after {Rested:0.0}h of rest, at {Where}{Home}{Call}{Buff}",
            bot.Name,
            bot.Class?.Name,
            rested,
            bot.Location,
            own ? "" : " (the ground it left would not take a body, so at home)",
            call == null ? "" : $" - {call}",
            buff
        );
    }

    /// <summary>
    /// A guild in a fight calls members back from their rest: up to <see cref="CallMost"/> of them, or
    /// <see cref="CallMostBig"/> when it is fighting more than one guild, counting the ones already back on its call;
    /// those who have rested longest first. Returns how many came. See <see cref="Calls"/>.
    /// </summary>
    public static int Call(Guild guild, string why)
    {
        if (!Running || !Calls || guild?.Name == null || guild.Disbanded)
        {
            return 0;
        }

        var most = BotWar.Fighting(guild.Name) > 1 || BotWar.InBig(guild.Name) ? CallMostBig : CallMost;
        var onCall = 0;
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false } bot && _records.TryGetValue(bot.Name ?? "", out var record)
                && string.Equals(record.CalledBy, guild.Name, StringComparison.OrdinalIgnoreCase))
            {
                onCall++;
            }
        }

        // <b>And as many more as the score asks for.</b> Patrick's order of 24.09.2026: the further behind a guild falls in
        // its war, the more of it is drawn in (BotWar.Wanted); what the world cannot supply is called back from rest.
        most = Math.Max(most, onCall + BotWar.FromRest(guild.Name));

        if (onCall >= most)
        {
            return 0;
        }

        List<(BotMobile Bot, Record Record)> resting = [];
        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            if (away[i] is { Deleted: false } bot && ReferenceEquals(bot.Guild, guild)
                && _records.TryGetValue(bot.Name ?? "", out var record) && record.CalledBy == null)
            {
                resting.Add((bot, record));
            }
        }

        resting.Sort((a, b) => a.Record.LeftAt.CompareTo(b.Record.LeftAt));

        var now = Core.Now;
        var came = 0;

        for (var i = 0; i < resting.Count && onCall + came < most; i++)
        {
            var (bot, record) = resting[i];

            record.OwedHours = record.RestUntil > now ? (record.RestUntil - now).TotalHours : 0.0;
            record.CalledBy = guild.Name;
            record.CallOverTick = 0;

            Return(bot, record, $"called back by {guild.Name}: {why}, owing {record.OwedHours:0.0}h of rest");

            came++;
            Called++;
        }

        if (came > 0)
        {
            logger.Warning(
                "{Guild} calls {Came} of its resting members back to fight - {Why}; {OnCall} on its call now, {Left} still resting",
                guild.Name,
                came,
                why,
                onCall + came,
                resting.Count - came
            );
        }

        return came;
    }

    /// <summary>Whether the fight a guild called its members back for still stands: a war, or a claim on its ground.</summary>
    private static bool CallStands(string guild) => BotWar.Fighting(guild) > 0 || BotClaim.Contested(guild);

    /// <summary>What the dashboard says of a bot out of the world: when it is back. Null for a bot in the world.</summary>
    public static string Offline(BotMobile bot)
    {
        if (bot?.Name == null || !_records.TryGetValue(bot.Name, out var record) || record.RestUntil == default)
        {
            return null;
        }

        return record.Inn == null
            ? $"resting, back about {record.RestUntil.ToLocalTime():HH:mm}"
            : $"resting at {record.Inn}{(record.Paid > 0 ? "" : " (unpaid)")}, back about {record.RestUntil.ToLocalTime():HH:mm}";
    }

    /// <summary>The guild that called this bot back from rest, or null.</summary>
    public static string CalledBy(BotMobile bot) =>
        bot?.Name != null && _records.TryGetValue(bot.Name, out var record) ? record.CalledBy : null;

    /// <summary>A new world: everybody's stamina and rest forgotten. See BotHand's reset.</summary>
    public static void Forget() => _records.Clear();

    public static string Describe()
    {
        var soonest = DateTime.MaxValue;
        var away = BotPopulation.Away;

        for (var i = 0; i < away.Count; i++)
        {
            if (away[i] is { Deleted: false } bot && _records.TryGetValue(bot.Name ?? "", out var record)
                && record.RestUntil < soonest)
            {
                soonest = record.RestUntil;
            }
        }

        var next = soonest == DateTime.MaxValue ? "nobody resting" : $"the next back about {soonest.ToLocalTime():HH:mm}";

        // And who tires next, so a line that says nobody is tired can be told from a clock that is not running.
        var tiring = double.MaxValue;
        string tiringName = null;
        var bots = BotPopulation.Bots;

        for (var i = 0; i < bots.Count; i++)
        {
            if (bots[i] is { Deleted: false, Tired: false } bot && _records.TryGetValue(bot.Name ?? "", out var record)
                && record.Hours > 0 && record.Hours * 60 - record.PlayedMinutes < tiring)
            {
                tiring = record.Hours * 60 - record.PlayedMinutes;
                tiringName = bot.Name;
            }
        }

        var tires = tiringName == null ? "" : $"; {tiringName} tires next, in {Math.Max(0, tiring):0} minutes";

        return $"{BotPopulation.Count} in the world, {away.Count} resting ({next}{tires}); {BotPrime.Describe()}; "
               + $"{_heldByFloor} past their hours and playing on so the island is not left under its floor of {_floor} ({_heldTraders} traders so no more than {BotTrading.RestShare:P0} of the traders rest at once, {_heldCritical} for a critical post, {_heldPace} put off a look by the pace); "
               + $"{BotCritical.Describe()}; {_tired} tired and not yet free "
               + $"({_heldWorking} finishing work, {_heldCompany} in a company, {_heldFight} in a fight, {_heldUnder} underground, "
               + $"{_heldCell} in a cell, {_heldDead} dead); this session {Left} left to rest ({LetGo} let go of what held them after "
               + $"{GraceMs / 60000} minutes) and {Returned} came back ({ReturnedHome} at home); {Called} called back from rest to fight "
               + $"and {Dismissed} gone back to it{(Calls ? "" : " (calls OFF)")}; {Early} back up to {EarlyMinutes} minutes early to hold the floor";
    }

    internal static void Save(IGenericWriter writer)
    {
        writer.WriteEncodedInt(_records.Count);

        foreach (var (name, record) in _records)
        {
            writer.Write(name);
            writer.WriteEncodedInt(record.Kind);
            writer.Write(record.Hours);
            writer.Write(record.PlayedMinutes);
            writer.Write(record.RestUntil);
            writer.Write(record.LeftAt);
            writer.WriteEncodedInt(record.Sessions);
            writer.Write(record.CalledBy ?? "");
            writer.Write(record.OwedHours);
            writer.Write(record.Inn ?? "");
            writer.WriteEncodedInt(record.Paid);
            writer.Write(record.BuffUntil);
        }
    }

    /// <param name="shape">1 had no kind and no hours: those are dealt at the start, and the play is kept. 2 had no calls.</param>
    internal static int Load(IGenericReader reader, int shape)
    {
        _records.Clear();

        var count = reader.ReadEncodedInt();

        for (var i = 0; i < count; i++)
        {
            var name = reader.ReadString();
            var kind = shape >= 2 ? reader.ReadEncodedInt() : -1;
            var hours = shape >= 2 ? reader.ReadDouble() : 0.0;
            var record = new Record
            {
                Kind = kind,
                Hours = hours,
                PlayedMinutes = reader.ReadDouble(),
                RestUntil = reader.ReadDateTime(),
                LeftAt = reader.ReadDateTime(),
                Sessions = reader.ReadEncodedInt()
            };

            if (shape >= 3)
            {
                var called = reader.ReadString();

                record.CalledBy = string.IsNullOrEmpty(called) ? null : called;
                record.OwedHours = reader.ReadDouble();
            }

            if (shape >= 4)
            {
                var inn = reader.ReadString();

                record.Inn = string.IsNullOrEmpty(inn) ? null : inn;
                record.Paid = reader.ReadEncodedInt();
                record.BuffUntil = reader.ReadDateTime();
            }

            if (!string.IsNullOrEmpty(name))
            {
                _records[name] = record;
            }
        }

        return _records.Count;
    }

    private sealed class RestTimer : Timer
    {
        public RestTimer(TimeSpan interval) : base(interval, interval)
        {
        }

        protected override void OnTick() => Look();
    }
}
