using System;
using System.Collections.Generic;
using System.Text.Json;
using Server.Guilds;
using Server.Logging;
using Server.Mobiles;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// Each town's steward: the head of the guild with the best standing in it, holding the town's coffer and its levers, and
/// answering for both.
///
/// <para>
/// <b>Patrick, approved 07.10.2026 (wave 17, "a livelier world"):</b> "A town elects a steward (the head of the guild with the
/// best standing in the town, or a thinking role like the duke). Real levers, each a decree announced in town and logged:
/// raise/lower the shops' tax (±1–3 %), hire the captain of the guard (a guild gets the post and pay), set a bounty on a head,
/// call the militia, bar an outcast guild from the town for a day, declare a holiday (an extra fair), order a caravan. Guilds
/// react (a higher tax sends traders to another town; a bounty draws hunters). The steward answers: an empty purse or a falling
/// town regard deposes it."
/// </para>
///
/// <para>
/// <b>Who.</b> In each town reached from home, the guild with the best standing there (<c>BotBurgh</c>: the cheapest prices,
/// then the most tasks done, then the fewest failed — the council's own order, <c>BotCouncil</c>), with <see cref="LeastDone"/>
/// tasks done, not put out of the town, not The Shadow and with a head. The steward is that guild's head, whoever it is now.
/// Chosen again every <see cref="ChooseMs"/>, and at once when the guild is gone, headless, put out or barred, when its head
/// falls, or when the steward is deposed. A deposed guild may not be chosen in that town for <see cref="DeposedMs"/>.
/// </para>
///
/// <para>
/// <b>The coffer is the treasury's coin, held for the town.</b> Every coin in a coffer is held out of the city's purse
/// (<see cref="BotCity.Escrow"/>), so the <c>City:</c> line counts it as held and a restart keeps it (<see cref="BotStewardStore"/>).
/// It fills two ways: an allowance of <see cref="Allowance"/>gp an hour while the treasury holds more than <see cref="CityKeep"/>,
/// and the town's tax — the part of every bill at the town's counters that the steward's tax added, paid into the treasury as
/// tax (<see cref="BotCity.Tax"/>) and held for the coffer. A tax cut is a rebate the coffer pays (<see cref="BotCity.Disbursed"/>),
/// suspended while the coffer holds under <see cref="CutCover"/>. Every decree that costs anything moves the coin to where it
/// works: the captain's and the guard line's watches are paid out of the purse per watch stood (the coin is handed back to the
/// purse and the town's wage budget raised by it, <c>BotCouncil.Appoint/Fund</c>); a bounty is paid out of the purse when the
/// head is taken (<c>BotCrime.Raise</c>); the militia's head money goes into the invasion's own hold (<c>BotSeasons.Hold</c>);
/// a caravan's hold out of the purse; a holiday's feast is bought from the town's shopkeepers (spent, like any NPC purchase).
/// Nothing is made and nothing vanishes unseen: the <c>Stewards:</c> line prints every flow and what the coffers hold now.
/// </para>
///
/// <para>
/// <b>The town's regard</b> of its steward is 50 with the steward's guild's prices there taken off (×1.05 is −10) plus the
/// town's mood: each decree moves it once (a tax raised −2 a point, cut +2 a point, the militia +5, a holiday +6, a caravan +3,
/// a bounty, a captain or a bar +2), and every round presses on it (a tax over nought −0.15 a point, each head at large posted
/// in the town −0.5, an invasion with no company come −2, a coffer under the floor −1), between −40 and +40. Deposed: the
/// coffer under <see cref="Floor"/> for <see cref="PoorMs"/>, or the regard not risen once in <see cref="FallMs"/> and lower by
/// <see cref="FallLeast"/> or more over it.
/// </para>
/// </summary>
public static partial class BotStewardship
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BotStewardship));

    // ---- dials -------------------------------------------------------------------------------------------------------------

    /// <summary>Whether towns have stewards at all. Off: no decrees, no tax, no bars; the coffers stay held.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>How often a steward decides: a quarter of an hour.</summary>
    public static int EveryMs { get; set; } = 900000;

    /// <summary>The first round this long after the first look, the towns a minute apart.</summary>
    public static int FirstMs { get; set; } = 600000;

    /// <summary>How often the steward is chosen again: two hours.</summary>
    public static int ChooseMs { get; set; } = 7200000;

    /// <summary>A town with no steward looks again this often.</summary>
    public static int RetryMs { get; set; } = 600000;

    /// <summary>Tasks done for the town before a guild may hold its stewardship.</summary>
    public static int LeastDone { get; set; } = 1;

    /// <summary>The lowest and highest shops' tax a steward may decree: −3 % and +5 %.</summary>
    public static double TaxFloor { get; set; } = -0.03;

    public static double TaxCeiling { get; set; } = 0.05;

    /// <summary>The most one decree moves the tax, in points.</summary>
    public static int TaxMostStep { get; set; } = 3;

    /// <summary>One tax decree a town in this long.</summary>
    public static int TaxEveryMs { get; set; } = 3600000;

    /// <summary>The coffer's allowance from the treasury, gold an hour a town.</summary>
    public static int Allowance { get; set; } = 100;

    /// <summary>The treasury keeps this much: no allowance under it.</summary>
    public static int CityKeep { get; set; } = 5000;

    /// <summary>The most a coffer is filled to by the allowance.</summary>
    public static int CofferMost { get; set; } = 5000;

    /// <summary>The coffer's floor: no decree spends under it, and a coffer under it for <see cref="PoorMs"/> deposes.</summary>
    public static int Floor { get; set; } = 50;

    /// <summary>A coffer this full cuts the tax and may keep a holiday.</summary>
    public static int Rich { get; set; } = 1200;

    /// <summary>A tax cut is suspended at the counters while the coffer holds under this.</summary>
    public static int CutCover { get; set; } = 20;

    public static int PoorMs { get; set; } = 3600000;

    public static int FallMs { get; set; } = 7200000;

    public static double FallLeast { get; set; } = 4.0;

    /// <summary>How long a deposed guild may not hold the same town's stewardship: six hours.</summary>
    public static int DeposedMs { get; set; } = 21600000;

    /// <summary>The captain's hire: watches funded and how long the post is kept through the council's own terms.</summary>
    public static int HireGold { get; set; } = 200;

    public static int HireMs { get; set; } = 7200000;

    /// <summary>What one decree adds to a head's bounty, how far over the crime folder's own cap, and how often per head.</summary>
    public static int BountyRaise { get; set; } = 150;

    public static int BountyAbove { get; set; } = 1000;

    public static int BountyWaitMs { get; set; } = 900000;

    public static int BountyAgainMs { get; set; } = 3600000;

    /// <summary>The militia's head money added to an invasion of the town, and how long a raid stands unanswered first.</summary>
    public static int MilitiaGold { get; set; } = 300;

    public static int MilitiaWaitMs { get; set; } = 300000;

    /// <summary>The guard line's wages for a bar, how long a bar stands, how many at once, and the regard that calls for one.</summary>
    public static int BarCost { get; set; } = 100;

    public static double BarHours { get; set; } = 24.0;

    public static int MostBars { get; set; } = 2;

    public static double BarRegard { get; set; } = -60.0;

    /// <summary>A holiday's feast, the hours since any fair before one, a town's hours between two, and the regard that wants one.</summary>
    public static int HolidayCost { get; set; } = 250;

    public static double HolidayGapHours { get; set; } = 8.0;

    public static double HolidayEveryHours { get; set; } = 24.0;

    public static double HolidayRegard { get; set; } = 45.0;

    /// <summary>A caravan's share paid from the coffer, and a town's time between two.</summary>
    public static int CaravanGold { get; set; } = 300;

    public static int CaravanEveryMs { get; set; } = 7200000;

    /// <summary>Whether the model chooses among the lawful decrees when the thinking assembly is in and free.</summary>
    public static bool ModelChooses { get; set; } = true;

    /// <summary>Whether a barred guild's walk into the town is turned back at the line (besides its work there being refused).</summary>
    public static bool WalkerBars { get; set; } = true;

    /// <summary>How often the <c>Stewards:</c> line is written.</summary>
    public static int SayEveryMs { get; set; } = 300000;

    /// <summary>Decrees each town remembers for the page and Argus.</summary>
    public static int Remembered { get; set; } = 8;

    /// <summary>The bounds of the town's mood.</summary>
    public static double MoodMost { get; set; } = 40.0;

    /// <summary>What each point of tax over nought takes off the town's regard every round: a point bears it, two do not for long.</summary>
    public static double TaxPress { get; set; } = 0.15;

    /// <summary>With nothing the rule would decree, the model is still asked every this many rounds of a town (an hour).</summary>
    public static int AskEvery { get; set; } = 4;

    // ---- the seam to the thinking assembly ---------------------------------------------------------------------------------

    /// <summary>
    /// The model as the steward: given a question, it answers through the callback with the decree's id (one of the options, or
    /// "none"), a line of speech and a reason, or nulls. Returns whether it will be asked. Filled by
    /// <c>mindedBots/debugger/BotStewardArgus</c>; null when the thinking assembly is off, and then the rules decide.
    /// </summary>
    public static Func<BotStewardAsk, Action<string, string, string>, bool> Ask { get; set; }

    // ---- the offices -------------------------------------------------------------------------------------------------------

    /// <summary>One decree as remembered.</summary>
    public sealed class Decree
    {
        public DateTime Utc;

        public string Kind;

        public string Text;

        public int Cost;

        public string By;
    }

    /// <summary>One town's stewardship.</summary>
    public sealed class Office
    {
        public string Town;

        /// <summary>The steward's guild, or null while the town has none.</summary>
        public string Guild;

        /// <summary>The head's name when last seen.</summary>
        public string Head;

        public bool HeadDown;

        public long Since;

        public long ChosenAt;

        public long NextRound;

        /// <summary>Coin held for the town out of the treasury.</summary>
        public int Coffer;

        /// <summary>Coin still held for a holiday whose fair has not been announced yet.</summary>
        public int Pending;

        public long PendingAt;

        public double Tax;

        public double Mood;

        public readonly List<double> Samples = [];

        public bool Low;

        public long LowSince;

        public bool Flowing;

        public bool TaxedOnce;

        public long TaxedAt;

        public DateTime HolidayUtc = DateTime.MinValue;

        public bool CaravanOnce;

        public long CaravanAt;

        public bool CaravanPending;

        public long CaravanMark;

        public string CaravanTo;

        public int CaravanPaid;

        public bool Asking;

        public long AskedAt;

        /// <summary>Rounds this town has had, for the model's hourly question.</summary>
        public long Held;

        /// <summary>The invasion the steward last called the militia against, by its start.</summary>
        public bool MilitiaCalled;

        public long MilitiaRaid;

        /// <summary>Bills taken at the town's counters under the tax this round, and the last round's.</summary>
        public long Takings;

        public long LastTakings;

        public readonly Dictionary<string, DateTime> Bars = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Heads whose bounty this steward raised, and when.</summary>
        public readonly Dictionary<string, long> Raised = new(StringComparer.Ordinal);

        public readonly List<Decree> Decrees = [];
    }

    private static readonly Dictionary<string, Office> _offices = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<(string Guild, string Town), long> _deposed = [];

    private static readonly HashSet<string> _barredGuilds = new(StringComparer.OrdinalIgnoreCase);

    private static readonly List<(BotMobile Bot, BotCrime.Sheet Sheet)> _heads = [];

    private static int _taxed;

    private static bool _opened;

    private static long _saidAt;

    private static long _heldAtLoad;

    // ---- counters ----------------------------------------------------------------------------------------------------------

    public static long Chosen { get; private set; }

    public static long Changed { get; private set; }

    public static long DeposedPoor { get; private set; }

    public static long DeposedFall { get; private set; }

    public static long Rounds { get; private set; }

    public static long Idle { get; private set; }

    public static long Decrees { get; private set; }

    public static long ByRule { get; private set; }

    public static long ByModel { get; private set; }

    public static long ByArgus { get; private set; }

    public static long Refused { get; private set; }

    public static long TaxUp { get; private set; }

    public static long TaxDown { get; private set; }

    public static long Captains { get; private set; }

    public static long Bounties { get; private set; }

    public static long Militias { get; private set; }

    public static long BarsSet { get; private set; }

    public static long Holidays { get; private set; }

    public static long HolidaysFailed { get; private set; }

    public static long Caravans { get; private set; }

    public static long CaravansFailed { get; private set; }

    public static long ModelAsked { get; private set; }

    public static long ModelAnswered { get; private set; }

    public static long ModelNone { get; private set; }

    public static long ModelUnread { get; private set; }

    public static long Drawn { get; private set; }

    public static long AllowanceDry { get; private set; }

    public static long TaxedGold { get; private set; }

    public static long TaxedBuys { get; private set; }

    public static long CutGold { get; private set; }

    public static long CutBuys { get; private set; }

    public static long Uncovered { get; private set; }

    public static long WageGold { get; private set; }

    public static long BountyGold { get; private set; }

    public static long MilitiaPaid { get; private set; }

    public static long FeastGold { get; private set; }

    public static long CaravanPaid { get; private set; }

    public static long CaravanBack { get; private set; }

    public static long Vetoed { get; private set; }

    public static long TurnedBack { get; private set; }

    /// <summary>Every office, for the page and Argus.</summary>
    public static IEnumerable<Office> Offices => _offices.Values;

    public static Office Of(string town) => town != null && _offices.TryGetValue(town, out var o) ? o : null;

    // ---- the clock ---------------------------------------------------------------------------------------------------------

    /// <summary>The politics module started: the coffers read back are counted as held by the treasury again.</summary>
    public static void Start()
    {
        BotStewardConfig.Load();

        if (_heldAtLoad > 0)
        {
            BotCity.Held((int)Math.Min(int.MaxValue, _heldAtLoad));

            logger.Information("Stewards: {Gold}gp in {Towns} towns' coffers read back and held out of the treasury again", _heldAtLoad, _offices.Count);

            _heldAtLoad = 0;
        }

        RecountTax();
        RebuildBars();

        logger.Information(
            "Stewards are {State}: chosen every {Choose:0.#}h by the towns' standing, a round of decrees every {Every}m; tax {Floor:+0;-0;0}% to {Ceiling:+0;-0;0}% in steps of up to {Step}, an allowance of {Allowance}gp an hour a town while the treasury holds over {Keep}gp; deposed under {CofferFloor}gp for {Poor}m or on a regard falling {Least:0.#} over {Fall}m; the model chooses {Model}",
            Running ? "ON" : "OFF",
            ChooseMs / 3600000.0,
            EveryMs / 60000,
            TaxFloor * 100,
            TaxCeiling * 100,
            TaxMostStep,
            Allowance,
            CityKeep,
            Floor,
            PoorMs / 60000,
            FallLeast,
            FallMs / 60000,
            ModelChooses ? Ask == null ? "when wired (not now)" : "when the model is free" : "never"
        );
    }

    /// <summary>The politics module reloaded: counters forgotten, the offices and their coffers kept.</summary>
    public static void Stop()
    {
        logger.Information("Stewards, before the reload: {Line}", Describe());

        foreach (var o in _offices.Values)
        {
            o.Asking = false;
        }

        ForgetCounters();
    }

    /// <summary>
    /// <b>Patrick's reset (07.10.2026, reset26): every town's stewardship ended and its coffer handed back to the treasury, so the
    /// save writes the stewardships empty.</b> <see cref="Stop"/> keeps the offices and their coffers for a reload, and the reset of
    /// 20:56 that night left <c>Saves/BotStewardship</c> whole: every town's old steward guild, its tax, its bars against guilds
    /// founded again under old names, its decrees and the guilds deposed.
    ///
    /// <para>
    /// <b>The books.</b> A coffer is coin held out of the city's purse for its town: drawn by <c>BotCity.Escrow</c>, counted in
    /// <c>BotCity.Escrowed</c> and not in <c>BotCity.Purse</c>, written by this store and counted as held again at the boot
    /// (<see cref="Start"/>). Forgotten without a word, the coin would be gone from the purse and still counted as held; written
    /// empty, the next boot would count nothing held and the coin would simply be gone. So each office's <see cref="Office.Coffer"/>
    /// and <see cref="Office.Pending"/> (a holiday's coin not yet spent on its fair) go back into the purse with
    /// <c>BotCity.Refund</c>, which raises the purse and lowers the held count by the same coin. A caravan's contribution went back
    /// into the purse when it was decreed (a caravan posted for it holds its own coin, which the events' wipe hands back), as did a
    /// captain's hire and a bounty's raise; a militia's head money is the invasion's pool. Nothing else is held by a steward.
    /// Returns the gold handed back.
    /// </para>
    /// </summary>
    public static int Wipe()
    {
        long back = 0;

        foreach (var o in _offices.Values)
        {
            var held = (long)Math.Max(0, o.Coffer) + Math.Max(0, o.Pending);

            if (held > 0)
            {
                BotCity.Refund((int)Math.Min(int.MaxValue, held));
                back += held;
            }

            o.Coffer = 0;
            o.Pending = 0;
        }

        if (back > 0)
        {
            logger.Information("Stewards: the world is wiped; {Gold}gp in {Towns} towns' coffers handed back to the treasury", back, _offices.Count);
        }

        _offices.Clear();
        _deposed.Clear();
        _barredGuilds.Clear();
        _heads.Clear();
        _turnedSaid.Clear();
        _lapsed.Clear();
        _taxed = 0;
        _opened = false;
        _heldAtLoad = 0;
        _quote = default;
        ForgetCounters();

        return (int)Math.Min(int.MaxValue, back);
    }

    private static void ForgetCounters()
    {
        Chosen = Changed = DeposedPoor = DeposedFall = Rounds = Idle = Decrees = ByRule = ByModel = ByArgus = Refused = 0;
        TaxUp = TaxDown = Captains = Bounties = Militias = BarsSet = Holidays = HolidaysFailed = Caravans = CaravansFailed = 0;
        ModelAsked = ModelAnswered = ModelNone = ModelUnread = 0;
        Drawn = AllowanceDry = TaxedGold = TaxedBuys = CutGold = CutBuys = Uncovered = 0;
        WageGold = BountyGold = MilitiaPaid = FeastGold = CaravanPaid = CaravanBack = Vetoed = TurnedBack = 0;
    }

    private static long _faults;

    /// <summary>Once a minute, from <c>BotPolitics.Look</c>. A fault here is logged and kept from the rest of politics.</summary>
    public static void Look(long now)
    {
        try
        {
            Turn(now);
        }
        catch (Exception e)
        {
            if (++_faults <= 3 || _faults % 100 == 0)
            {
                logger.Error(e, "The stewards' look failed ({Times} times so far)", _faults);
            }
        }
    }

    private static void Turn(long now)
    {
        if (!_opened)
        {
            _opened = true;
            _saidAt = now;
        }

        if (now - _saidAt >= Math.Max(60000, SayEveryMs))
        {
            _saidAt = now;
            logger.Information("Stewards: {Line}", Describe());
        }

        if (!Running || !BotTowns.Surveyed)
        {
            return;
        }

        Resolve(now);
        Expire();

        BotCrime.Wanted(_heads);

        var towns = BotTowns.All;

        for (var i = 0; i < towns.Count; i++)
        {
            var town = towns[i];

            if (!town.FromHome)
            {
                continue;
            }

            if (!_offices.TryGetValue(town.Name, out var o))
            {
                _offices[town.Name] = o = new Office
                {
                    Town = town.Name,
                    ChosenAt = now - Math.Max(0, ChooseMs),
                    NextRound = now + Math.Max(0, FirstMs) + i * 60000L
                };
            }

            Keep(o, town, now);

            if (o.Guild != null && now - o.NextRound >= 0 && !o.Asking)
            {
                Round(o, town, now);
            }

            // A model's word that never came back is let go after five minutes; the rules decide the next round.
            if (o.Asking && now - o.AskedAt >= 300000)
            {
                o.Asking = false;
                ModelUnread++;
            }
        }
    }

    // ---- who --------------------------------------------------------------------------------------------------------------

    /// <summary>The steward kept, or chosen again: on the clock, when none, and at once when its guild can no longer hold it.</summary>
    private static void Keep(Office o, BotTowns.Town town, long now)
    {
        if (o.Guild == null)
        {
            if (now - o.ChosenAt >= Math.Max(60000, RetryMs))
            {
                Choose(o, town, now, "the town had none");
            }

            return;
        }

        var guild = BotGuilds.Named(o.Guild);
        var head = BotGuilds.Head(guild);

        string why = null;

        if (guild is not { Disbanded: false })
        {
            why = $"{o.Guild} is no more";
        }
        else if (head == null)
        {
            why = $"{o.Guild} has no head";
        }
        else if (BotBurgh.Bars(guild, town))
        {
            why = $"{o.Guild} was put out of the town";
        }
        else if (!head.Alive && !o.HeadDown)
        {
            why = $"its head {head.Name} fell";
        }
        else if (now - o.ChosenAt >= Math.Max(60000, ChooseMs))
        {
            why = $"{ChooseMs / 3600000.0:0.#} hours have passed";
        }

        if (head != null)
        {
            o.HeadDown = !head.Alive;

            if (head.Name != o.Head)
            {
                logger.Information("{Head} is {Town}'s steward now, as the head of {Guild} after {Was}", head.Name, town.Name, o.Guild, o.Head ?? "nobody");
                o.Head = head.Name;
            }
        }

        if (why != null)
        {
            Choose(o, town, now, why);
        }
    }

    /// <summary>The guild with the best standing in the town that may hold it; the same order the council gives its posts by.</summary>
    private static void Choose(Office o, BotTowns.Town town, long now, string why)
    {
        o.ChosenAt = now;

        BotBurgh.Standing best = null;

        foreach (var st in BotBurgh.All)
        {
            if (st.Town != town.Name || st.Exiled || st.Done < LeastDone || BotGuilds.Named(st.Guild) is not { Disbanded: false } g
                || BotUnderworld.Band(g) || BotGuilds.Head(g) == null || Deposed(st.Guild, town.Name, now) || o.Bars.ContainsKey(st.Guild))
            {
                continue;
            }

            if (best == null || Better(st, best))
            {
                best = st;
            }
        }

        var was = o.Guild;

        if (best == null)
        {
            if (was != null)
            {
                logger.Information("{Town} has no steward now ({Why}): no guild has done {Least} of its tasks and may hold it", town.Name, why, LeastDone);
                o.Guild = null;
                o.Head = null;
            }

            return;
        }

        if (best.Guild == was)
        {
            return;
        }

        var head = BotGuilds.Head(BotGuilds.Named(best.Guild));

        o.Guild = best.Guild;
        o.Head = head?.Name;
        o.HeadDown = head is { Alive: false };
        o.Since = now;
        o.Mood = 0.0;
        o.Samples.Clear();
        o.Low = false;
        o.NextRound = now - o.NextRound > 0 ? now + 60000 : o.NextRound;
        Chosen++;

        if (was != null)
        {
            Changed++;
        }

        logger.Information(
            "{Head} of {Guild} is {Town}'s steward ({Why}): its standing there is ×{Factor:F3} with {Done} tasks done and {Failed} failed; before, {Was}; the coffer holds {Coffer}gp, the tax is {Tax}",
            head?.Name ?? "its head",
            best.Guild,
            town.Name,
            why,
            best.Factor,
            best.Done,
            best.Failed,
            was ?? "nobody",
            o.Coffer,
            Pct(o.Tax)
        );

        Speak(o, town, $"{head?.Name ?? best.Guild} of {best.Guild} is the steward of {town.Name} now.", false, "chosen");
    }

    private static bool Better(BotBurgh.Standing a, BotBurgh.Standing b) =>
        a.Factor < b.Factor - 0.0001 || Math.Abs(a.Factor - b.Factor) <= 0.0001 && (a.Done > b.Done || a.Done == b.Done && a.Failed < b.Failed);

    private static bool Deposed(string guild, string town, long now) =>
        _deposed.TryGetValue((guild, town), out var at) && now - at < Math.Max(0, DeposedMs);

    // ---- the round ---------------------------------------------------------------------------------------------------------

    private static void Round(Office o, BotTowns.Town town, long now)
    {
        o.NextRound = now + Math.Max(60000, EveryMs);
        Rounds++;
        o.Held++;

        Draw(o);
        Press(o, town, now);

        var regard = Regard(o, town);
        var keep = Math.Max(2, FallMs / Math.Max(60000, EveryMs)) + 1;

        o.Samples.Add(regard);

        while (o.Samples.Count > keep)
        {
            o.Samples.RemoveAt(0);
        }

        if (Answer(o, town, now, regard))
        {
            return;
        }

        if (o.Takings > 0)
        {
            logger.Information(
                "{Town}'s counters took {Takings}gp from bots over the last round at a tax of {Tax} (the round before: {Last}gp)",
                town.Name,
                o.Takings,
                Pct(o.Tax),
                o.LastTakings
            );
        }

        o.LastTakings = o.Takings;
        o.Takings = 0;

        List<Option> options = [];

        Options(o, town, now, options);

        if (options.Count == 0)
        {
            Idle++;

            return;
        }

        if (ModelChooses && Ask is { } ask && !AnyAsking() && (options[0].Rule || o.Held % Math.Max(1, AskEvery) == 0))
        {
            // The model is shown every lawful decree, the rule's and the rest; it may also answer "none".
            var q = Question(o, town, options, regard);

            if (ask(q, (id, speech, reason) => Answered(o.Town, options, id, speech, reason)))
            {
                o.Asking = true;
                o.AskedAt = now;
                ModelAsked++;

                return;
            }
        }

        if (!options[0].Rule)
        {
            Idle++;

            return;
        }

        Apply(o, town, options[0], "the rule", null, options[0].Why);
    }

    private static bool AnyAsking()
    {
        foreach (var o in _offices.Values)
        {
            if (o.Asking)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The model's word, on the game thread: one of the options, "none", or nothing readable (then the rule).</summary>
    private static void Answered(string townName, List<Option> options, string id, string speech, string reason)
    {
        if (!_offices.TryGetValue(townName, out var o) || !o.Asking)
        {
            return;
        }

        o.Asking = false;

        var town = BotTowns.Find(townName);

        if (town == null || o.Guild == null || !Running)
        {
            return;
        }

        if (string.IsNullOrEmpty(id))
        {
            ModelUnread++;
            Ruled(o, town, options);

            return;
        }

        ModelAnswered++;

        if (id == "none")
        {
            ModelNone++;
            logger.Information("{Head}, {Town}'s steward, decreed nothing this round by the model's word: {Reason}", o.Head, town.Name, reason ?? "no reason given");

            return;
        }

        for (var i = 0; i < options.Count; i++)
        {
            if (options[i].Id == id)
            {
                Apply(o, town, options[i], "the model", speech, reason ?? options[i].Why);

                return;
            }
        }

        ModelUnread++;
        Ruled(o, town, options);
    }

    /// <summary>The rule's decree when the model gave none that could be read: the first the rule would take, if any.</summary>
    private static void Ruled(Office o, BotTowns.Town town, List<Option> options)
    {
        if (options.Count > 0 && options[0].Rule)
        {
            Apply(o, town, options[0], "the rule", null, options[0].Why);
        }
    }

    /// <summary>The allowance for one round, while the treasury holds more than it keeps.</summary>
    private static void Draw(Office o)
    {
        var gold = (int)Math.Max(0L, (long)Allowance * Math.Max(60000, EveryMs) / 3600000);

        if (gold <= 0 || o.Coffer >= CofferMost)
        {
            o.Flowing = false;

            return;
        }

        if (BotCity.Purse - gold < Math.Max(0, CityKeep) || !BotCity.Escrow(gold))
        {
            o.Flowing = false;
            AllowanceDry++;

            return;
        }

        o.Flowing = true;
        o.Coffer += gold;
        Drawn += gold;
    }

    /// <summary>What presses on the town's regard every round. See the class note.</summary>
    private static void Press(Office o, BotTowns.Town town, long now)
    {
        var press = 0.0;

        if (o.Tax > 0.0)
        {
            press -= TaxPress * o.Tax * 100.0;
        }

        var heads = 0;

        for (var i = 0; i < _heads.Count; i++)
        {
            if (string.Equals(_heads[i].Sheet.Town, town.Name, StringComparison.OrdinalIgnoreCase))
            {
                heads++;
            }
        }

        press -= 0.5 * Math.Min(4, heads);

        if (BotInvasion.Now is { Over: false } raid && raid.Town == town && raid.Squads.Count == 0 && now - raid.Began >= MilitiaWaitMs)
        {
            press -= 2.0;
        }

        if (o.Coffer < Floor)
        {
            press -= 1.0;
        }

        Mood(o, press);
    }

    private static void Mood(Office o, double by) => o.Mood = Math.Clamp(o.Mood + by, -MoodMost, MoodMost);

    /// <summary>The town's regard of its steward now, 0 to 100.</summary>
    public static double Regard(Office o, BotTowns.Town town)
    {
        var factor = 1.0;

        if (o.Guild != null && town != null)
        {
            foreach (var st in BotBurgh.All)
            {
                if (st.Guild == o.Guild && st.Town == town.Name)
                {
                    factor = st.Factor;

                    break;
                }
            }
        }

        return Math.Clamp(50.0 + o.Mood - (factor - 1.0) * 200.0, 0.0, 100.0);
    }

    /// <summary>The regard's trend over the samples kept: the last minus the first.</summary>
    private static double Trend(Office o) => o.Samples.Count < 2 ? 0.0 : o.Samples[^1] - o.Samples[0];

    /// <summary>Whether the regard has not risen once over the window and is lower by <see cref="FallLeast"/>.</summary>
    private static bool Falling(Office o)
    {
        var need = Math.Max(2, FallMs / Math.Max(60000, EveryMs)) + 1;

        if (o.Samples.Count < need)
        {
            return false;
        }

        for (var i = o.Samples.Count - need + 1; i < o.Samples.Count; i++)
        {
            if (o.Samples[i] > o.Samples[i - 1] + 0.0001)
            {
                return false;
            }
        }

        return o.Samples[o.Samples.Count - need] - o.Samples[^1] >= FallLeast;
    }

    /// <summary>The steward answers for an empty purse and a falling regard. True when it was deposed.</summary>
    private static bool Answer(Office o, BotTowns.Town town, long now, double regard)
    {
        if (o.Coffer < Floor)
        {
            if (!o.Low)
            {
                o.Low = true;
                o.LowSince = now;
            }
        }
        else
        {
            o.Low = false;
        }

        string why = null;
        var poor = false;

        if (o.Low && now - o.LowSince >= PoorMs && now - o.Since >= PoorMs)
        {
            why = $"its coffer stood under {Floor}gp for {PoorMs / 60000} minutes ({o.Coffer}gp now)";
            poor = true;
        }
        else if (Falling(o))
        {
            var need = Math.Max(2, FallMs / Math.Max(60000, EveryMs)) + 1;

            why = $"the town's regard of it fell for {FallMs / 60000} minutes without rising once, from {o.Samples[o.Samples.Count - need]:F1} to {regard:F1}";
        }

        if (why == null)
        {
            return false;
        }

        if (poor)
        {
            DeposedPoor++;
        }
        else
        {
            DeposedFall++;
        }

        var guild = o.Guild;
        var head = o.Head;

        _deposed[(guild, town.Name)] = now;

        logger.Warning(
            "{Town} deposes its steward {Head} of {Guild}: {Why}; {Guild} may not hold it again for {Hours:0.#} hours; the coffer holds {Coffer}gp, the tax stays at {Tax}",
            town.Name,
            head,
            guild,
            why,
            guild,
            DeposedMs / 3600000.0,
            o.Coffer,
            Pct(o.Tax)
        );

        BotAlarm.Note("steward", $"{town.Name} deposed its steward {head} of {guild}: {why}", 1, 1, $"{DeposedMs / 3600000.0:0.#}h");

        if (BotGuilds.Head(BotGuilds.Named(guild)) is { Alive: true } fallen)
        {
            BotVoice.Say(fallen, "guild", poor ? $"{town.Name} has put me out of its stewardship: the coffer is empty." : $"{town.Name} has turned against me. I am steward no more.", true);
        }

        BotEvents.Post("politics", head ?? guild, null, guild, town.Square.X, town.Square.Y, $"{town.Name} deposed its steward {head} of {guild}: {why}", "steward", null);

        o.Guild = null;
        o.Head = null;
        Choose(o, town, now, $"{guild} was deposed");

        return true;
    }

    // ---- the counter: the tax ----------------------------------------------------------------------------------------------

    private static (Serial Buyer, double Levy, long Tick, Office Office) _quote;

    /// <summary>
    /// What a bot pays at this vendor's counter on top of everything else, as a multiple: one plus the town's tax. Asked by
    /// <c>BotBurgh.Factor</c> for every bot, guild or none; remembers the quote so the bill's settling can move the tax's share.
    /// A cut is quoted only while the coffer can pay for it.
    /// </summary>
    public static double Levy(Mobile buyer, BaseVendor vendor)
    {
        if (_taxed == 0)
        {
            return 1.0;
        }

        _quote = default;

        if (!Running || buyer is not BotMobile || vendor == null || BotTowns.Of(vendor.Location) is not { } town
            || !_offices.TryGetValue(town.Name, out var o) || o.Guild == null || Math.Abs(o.Tax) < 0.0001 || o.Tax < 0.0 && o.Coffer < CutCover)
        {
            return 1.0;
        }

        var levy = 1.0 + o.Tax;

        _quote = (buyer.Serial, levy, Core.TickCount, o);

        return levy;
    }

    /// <summary>
    /// A bill of <paramref name="bill"/> at <paramref name="factor"/> was settled with this buyer: the tax's share of it goes into
    /// the treasury and is held for the town's coffer; a cut's share is paid out of the coffer. From <c>BotBurgh.Settle</c>.
    /// </summary>
    public static void Settled(Mobile buyer, int bill, double factor)
    {
        var q = _quote;

        if (buyer == null || bill <= 0 || q.Office == null || q.Buyer != buyer.Serial || q.Tick != Core.TickCount || q.Levy <= 0.0)
        {
            return;
        }

        _quote = default;

        var o = q.Office;
        var share = bill * (factor - factor / q.Levy);

        o.Takings += bill;

        if (share > 0.0)
        {
            var gold = (int)Math.Min(bill, Math.Floor(share));

            if (gold <= 0)
            {
                return;
            }

            BotCity.Tax(gold);

            if (BotCity.Escrow(gold))
            {
                o.Coffer += gold;
                TaxedGold += gold;
                TaxedBuys++;
            }

            return;
        }

        var owed = (int)Math.Min(bill, Math.Ceiling(-share));
        var paid = Math.Min(owed, Math.Max(0, o.Coffer));

        CutBuys++;

        if (paid > 0)
        {
            o.Coffer -= paid;
            BotCity.Disbursed(paid);
            CutGold += paid;
        }

        Uncovered += owed - paid;
    }

    private static void RecountTax()
    {
        _taxed = 0;

        foreach (var o in _offices.Values)
        {
            if (o.Guild != null && Math.Abs(o.Tax) >= 0.0001)
            {
                _taxed++;
            }
        }
    }

    // ---- the guard line ------------------------------------------------------------------------------------------------------

    /// <summary>Whether this bot's guild is barred from the town that holds this place, by its steward's decree.</summary>
    public static bool Bars(Mobile bot, Map map, Point3D where, out string town)
    {
        town = null;

        if (_barredGuilds.Count == 0 || !Running || bot?.Guild is not Guild guild || guild.Name == null || !_barredGuilds.Contains(guild.Name)
            || map == null || map != BotPopulation.Home || BotTowns.Of(where) is not { } t || !_offices.TryGetValue(t.Name, out var o)
            || !o.Bars.TryGetValue(guild.Name, out var until) || DateTime.UtcNow >= until)
        {
            return false;
        }

        town = t.Name;

        return true;
    }

    /// <summary>The same for the auction: work in a town the bot's guild is barred from is refused, and counted.</summary>
    public static bool Refuses(Mobile bot, Map map, Point3D where, out string why)
    {
        why = null;

        if (!Bars(bot, map, where, out var town))
        {
            return false;
        }

        Vetoed++;
        why = $"lies in {town}, whose steward has barred {(bot.Guild as Guild)?.Name} from it";

        return true;
    }

    private static readonly Dictionary<Serial, long> _turnedSaid = [];

    /// <summary>
    /// The walker's check, once a step and cheap: a bot of a barred guild walking into the town from outside is turned back at
    /// the line. Not one in a fight: running is never kept out.
    /// </summary>
    public static bool TurnsBack(Mobile bot, Point3D target)
    {
        if (_barredGuilds.Count == 0 || !WalkerBars || bot == null || bot.Combatant != null || !Bars(bot, bot.Map, target, out var townName)
            || BotTowns.Find(townName) is not { } town || town.Bounds.Contains(bot.Location))
        {
            return false;
        }

        TurnedBack++;

        var now = Core.TickCount;

        if (!_turnedSaid.TryGetValue(bot.Serial, out var said) || now - said >= 600000)
        {
            _turnedSaid[bot.Serial] = now;

            logger.Information(
                "{Name} of {Guild} was turned back at {Town}'s guard line: barred by its steward's decree until {Until:HH:mm} UTC",
                bot.Name,
                (bot.Guild as Guild)?.Name,
                townName,
                Of(townName)?.Bars.GetValueOrDefault((bot.Guild as Guild)?.Name ?? ""));

            BotVoice.Say(bot, "local", $"The guard turns me back from {townName}. Its steward will not have us.", true);
        }

        return true;
    }

    private static void RebuildBars()
    {
        _barredGuilds.Clear();

        var now = DateTime.UtcNow;

        foreach (var o in _offices.Values)
        {
            foreach (var (guild, until) in o.Bars)
            {
                if (until > now)
                {
                    _barredGuilds.Add(guild);
                }
            }
        }
    }

    private static readonly List<string> _lapsed = [];

    /// <summary>Bars whose day is over are lifted.</summary>
    private static void Expire()
    {
        if (_barredGuilds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var any = false;

        foreach (var o in _offices.Values)
        {
            _lapsed.Clear();

            foreach (var (guild, until) in o.Bars)
            {
                if (until <= now)
                {
                    _lapsed.Add(guild);
                }
            }

            for (var i = 0; i < _lapsed.Count; i++)
            {
                o.Bars.Remove(_lapsed[i]);
                any = true;
                logger.Information("{Guild} may enter {Town} again: the steward's bar is over", _lapsed[i], o.Town);
            }
        }

        if (any)
        {
            RebuildBars();
        }
    }

    // ---- speaking ------------------------------------------------------------------------------------------------------------

    /// <summary>One of the steward's guild standing in the town, its head first, or null.</summary>
    private static Mobile Speaker(Office o, BotTowns.Town town)
    {
        var guild = BotGuilds.Named(o.Guild);

        if (BotGuilds.Head(guild) is { Alive: true } head && head.Map == BotPopulation.Home && town.Bounds.Contains(head.Location))
        {
            return head;
        }

        if (guild?.Members == null)
        {
            return null;
        }

        for (var i = 0; i < guild.Members.Count; i++)
        {
            if (guild.Members[i] is BotMobile { Deleted: false, Alive: true } m && m.Map == BotPopulation.Home && town.Bounds.Contains(m.Location))
            {
                return m;
            }
        }

        return null;
    }

    /// <summary>
    /// A decree said in the town: by the steward or one of its guild standing there; else, for a loud one, by the town's crier
    /// (Argus's herald when the debugger is on, <c>BotSeasons.Announce</c>); always on the dashboard's stream.
    /// </summary>
    private static void Speak(Office o, BotTowns.Town town, string text, bool loud, string kind)
    {
        var line = kind == "chosen" ? text : $"By the steward's decree: {text}";

        if (o.Guild != null && Speaker(o, town) is { } speaker)
        {
            BotVoice.Say(speaker, "local", line, true);
            BotEvents.Post("politics", speaker, line, "steward", BotJson.Object(w => w.WriteString("outcome", kind)));

            return;
        }

        if (loud)
        {
            BotSeasons.Announce("decree", BotPopulation.Home, town.Square, line);

            return;
        }

        BotEvents.Post("politics", o.Head ?? $"the steward of {town.Name}", null, o.Guild, town.Square.X, town.Square.Y, line, "steward", BotJson.Object(w => w.WriteString("outcome", kind)));
    }

    // ---- telling ------------------------------------------------------------------------------------------------------------

    public static string Pct(double tax) => tax > 0.00001 ? $"+{tax * 100:0.#}%" : tax < -0.00001 ? $"{tax * 100:0.#}%" : "0%";

    private static int Held()
    {
        long held = 0;

        foreach (var o in _offices.Values)
        {
            held += o.Coffer + o.Pending;
        }

        return (int)Math.Min(int.MaxValue, held);
    }

    /// <summary>The <c>Stewards:</c> line.</summary>
    public static string Describe()
    {
        using var say = ValueStringBuilder.Create(1024);

        say.Append(Running ? "" : "OFF; ");

        var governed = 0;

        foreach (var o in _offices.Values)
        {
            if (o.Guild != null)
            {
                governed++;
            }
        }

        say.Append($"{governed} of {_offices.Count} towns have a steward (");

        var first = true;

        foreach (var o in _offices.Values)
        {
            if (o.Guild == null)
            {
                continue;
            }

            var town = BotTowns.Find(o.Town);
            var regard = Regard(o, town);
            var trend = Trend(o);

            say.Append(first ? "" : "; ");
            first = false;
            say.Append(
                $"{o.Town} {o.Head ?? "?"} of {o.Guild}: tax {Pct(o.Tax)}, coffer {o.Coffer}gp{(o.Pending > 0 ? $" +{o.Pending} held" : "")}, regard {regard:F0}{(trend < -0.5 ? "↓" : trend > 0.5 ? "↑" : "")}{(o.Bars.Count > 0 ? $", bars {o.Bars.Count}" : "")}"
            );
        }

        say.Append(first ? "none)" : ")");
        say.Append($"; {Chosen} chosen ({Changed} changed hands), deposed {DeposedPoor} for an empty coffer and {DeposedFall} for a falling regard; ");
        say.Append(
            $"{Rounds} rounds, {Idle} with nothing lawful to decree; {Decrees} decrees ({TaxUp} tax raised, {TaxDown} cut, {Captains} captains hired, {Bounties} bounties raised, {Militias} militias called, {BarsSet} guilds barred, {Holidays} holidays ({HolidaysFailed} not held), {Caravans} caravans ({CaravansFailed} not sent)) by the rule {ByRule}, the model {ByModel}, Argus {ByArgus}, {Refused} refused as unlawful; "
        );
        say.Append($"the model asked {ModelAsked}, answered {ModelAnswered} ({ModelNone} \"none\"), {ModelUnread} unread or late; ");
        say.Append(
            $"coffers: {Drawn}gp drawn as allowance ({AllowanceDry} rounds the treasury kept it), {TaxedGold}gp taxed at the counters over {TaxedBuys} bills, {CutGold}gp of tax cuts paid over {CutBuys} bills ({Uncovered}gp the coffer could not cover); spent: {WageGold}gp to the guard's watches, {BountyGold}gp into bounties, {MilitiaPaid}gp into militia head money, {FeastGold}gp on feasts, {CaravanPaid}gp towards caravans ({CaravanBack}gp back); {Held()}gp held in the coffers now; "
        );
        say.Append($"barred guilds: {_barredGuilds.Count} now, {Vetoed} pieces of work refused in a barred town, {TurnedBack} walks turned back at the guard line");

        return say.ToString();
    }

    /// <summary>The town's steward and decrees inside its object in <c>/api/state</c>'s <c>towns</c>. From <c>BotWebSnapshot</c>.</summary>
    public static void Write(Utf8JsonWriter w, BotTowns.Town town)
    {
        if (town == null || !_offices.TryGetValue(town.Name, out var o))
        {
            return;
        }

        var now = Core.TickCount;

        w.WritePropertyName("steward");

        if (o.Guild == null)
        {
            w.WriteNullValue();
        }
        else
        {
            w.WriteStartObject();
            w.WriteString("guild", o.Guild);
            w.WriteString("head", o.Head);
            w.WriteNumber("minutes", (now - o.Since) / 60000);
            w.WriteNumber("regard", Math.Round(Regard(o, town), 1));
            w.WriteNumber("trend", Math.Round(Trend(o), 1));
            w.WriteNumber("coffer", o.Coffer);
            w.WriteNumber("pending", o.Pending);
            w.WriteNumber("tax", Math.Round(o.Tax * 100, 1));
            w.WriteBoolean("allowance", o.Flowing);
            w.WriteNumber("takings", o.LastTakings);
            w.WritePropertyName("bars");
            w.WriteStartArray();

            foreach (var (guild, until) in o.Bars)
            {
                w.WriteStartObject();
                w.WriteString("guild", guild);
                w.WriteNumber("hoursLeft", Math.Round(Math.Max(0.0, (until - DateTime.UtcNow).TotalHours), 1));
                w.WriteEndObject();
            }

            w.WriteEndArray();
            w.WriteEndObject();
        }

        w.WritePropertyName("decrees");
        w.WriteStartArray();

        for (var i = o.Decrees.Count - 1; i >= 0; i--)
        {
            var d = o.Decrees[i];

            w.WriteStartObject();
            w.WriteNumber("ago", Math.Round((DateTime.UtcNow - d.Utc).TotalMinutes));
            w.WriteString("kind", d.Kind);
            w.WriteString("text", d.Text);
            w.WriteNumber("cost", d.Cost);
            w.WriteString("by", d.By);
            w.WriteEndObject();
        }

        w.WriteEndArray();
    }

    // ---- the store ---------------------------------------------------------------------------------------------------------

    internal static void Save(IGenericWriter writer)
    {
        var now = Core.TickCount;

        writer.WriteEncodedInt(_offices.Count);

        foreach (var o in _offices.Values)
        {
            writer.Write(o.Town);
            writer.Write(o.Guild ?? "");
            writer.Write(o.Head ?? "");
            writer.Write(Math.Max(0L, now - o.Since));
            // A holiday's coin not yet spent is written as coffer: read back, it is held again with the coffer (Start), which is
            // where Resolve would have put it had the fair not come. So no held coin is lost at a restart (reset26 review, B3).
            writer.WriteEncodedInt(o.Coffer + o.Pending);
            writer.Write(o.Tax);
            writer.Write(o.Mood);
            writer.Write(o.HolidayUtc);
            writer.WriteEncodedInt(o.Bars.Count);

            foreach (var (guild, until) in o.Bars)
            {
                writer.Write(guild);
                writer.Write(until);
            }

            writer.WriteEncodedInt(o.Decrees.Count);

            for (var i = 0; i < o.Decrees.Count; i++)
            {
                var d = o.Decrees[i];

                writer.Write(d.Utc);
                writer.Write(d.Kind ?? "");
                writer.Write(d.Text ?? "");
                writer.WriteEncodedInt(d.Cost);
                writer.Write(d.By ?? "");
            }
        }

        writer.WriteEncodedInt(_deposed.Count);

        foreach (var ((guild, town), at) in _deposed)
        {
            writer.Write(guild);
            writer.Write(town);
            writer.Write(Math.Max(0L, now - at));
        }
    }

    internal static int Load(IGenericReader reader)
    {
        var now = Core.TickCount;
        var count = reader.ReadEncodedInt();

        _offices.Clear();
        _heldAtLoad = 0;

        for (var i = 0; i < count; i++)
        {
            var o = new Office
            {
                Town = reader.ReadString(),
                Guild = reader.ReadString(),
                Head = reader.ReadString()
            };

            var ago = reader.ReadLong();

            o.Coffer = reader.ReadEncodedInt();
            o.Tax = reader.ReadDouble();
            o.Mood = reader.ReadDouble();
            o.HolidayUtc = reader.ReadDateTime();

            var bars = reader.ReadEncodedInt();

            for (var b = 0; b < bars; b++)
            {
                var guild = reader.ReadString();
                var until = reader.ReadDateTime();

                if (!string.IsNullOrEmpty(guild))
                {
                    o.Bars[guild] = until;
                }
            }

            var decrees = reader.ReadEncodedInt();

            for (var d = 0; d < decrees; d++)
            {
                o.Decrees.Add(
                    new Decree
                    {
                        Utc = reader.ReadDateTime(),
                        Kind = reader.ReadString(),
                        Text = reader.ReadString(),
                        Cost = reader.ReadEncodedInt(),
                        By = reader.ReadString()
                    }
                );
            }

            if (string.IsNullOrEmpty(o.Guild))
            {
                o.Guild = null;
            }

            if (string.IsNullOrEmpty(o.Head))
            {
                o.Head = null;
            }

            // The term goes on where it was; the first round comes a few minutes after the boot, the towns spread.
            o.Since = now - Math.Clamp(ago, 0L, (long)int.MaxValue);
            o.ChosenAt = now;
            o.NextRound = now + Math.Max(0, FirstMs) + i * 60000L;
            o.Coffer = Math.Max(0, o.Coffer);
            _heldAtLoad += o.Coffer;

            if (!string.IsNullOrEmpty(o.Town))
            {
                _offices[o.Town] = o;
            }
        }

        var deposed = reader.ReadEncodedInt();

        for (var i = 0; i < deposed; i++)
        {
            var guild = reader.ReadString();
            var town = reader.ReadString();
            var ago = reader.ReadLong();

            _deposed[(guild, town)] = now - Math.Clamp(ago, 0L, (long)int.MaxValue);
        }

        RecountTax();
        RebuildBars();

        return _offices.Count;
    }
}

/// <summary>A question to the model as the steward: who, the facts, and the decrees it may give, each with what it does.</summary>
public sealed class BotStewardAsk
{
    public string Town;

    public string Guild;

    public string Head;

    /// <summary>The facts, plain lines.</summary>
    public string Brief;

    /// <summary>The ids it may answer, "none" first.</summary>
    public string[] Ids;

    /// <summary>What each id does, in the same order.</summary>
    public string[] Means;
}
