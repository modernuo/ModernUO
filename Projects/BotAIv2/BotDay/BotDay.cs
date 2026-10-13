using System;
using System.Text.Json;
using Server.Text;

namespace Server.BotAI.V2;

/// <summary>
/// The daily cycle: a shard clock of named parts of the day, and what each part is worth to each bot's timetable in the
/// auction. PLAN-life A1 (Patrick, 08.10 and 12.10.2026: "develop the daily cycle … a player at 22:00 finds a different
/// island from one at 10:00").
///
/// <para>
/// <b>The clock.</b> Local server time off <c>Core.Now</c>, as <see cref="BotPrime"/> reads its bands: dawn from <see cref="DawnAt"/>,
/// morning from <see cref="MorningAt"/>, noon from <see cref="NoonAt"/>, afternoon from <see cref="AfternoonAt"/>, evening from
/// <see cref="EveningAt"/>, night from <see cref="NightAt"/> round to dawn. Read on the module's two-second clock into
/// <see cref="Part"/>, so the auction reads a static and never the wall clock. A night owl (<see cref="BotTimetable.Owl"/>) lives
/// the same day <see cref="OwlShift"/> hours later: its evening is before dawn and its night is the morning.
/// </para>
///
/// <para>
/// <b>A weight on offers, never a wall.</b> <see cref="Factor"/> multiplies the score in <c>BotAppraisal.Weigh</c> outside the
/// root, beside the calling, the toll and the nature, so ×1.5 is ×1.5 and not its fifth root. The evening raises leisure
/// (supper, fireside, camp, home, chat, meet, keeping a tavern) — the habit's own kind by <see cref="Lift"/>, the rest by half of
/// it — and lowers long journeys; the night lowers journeys and raises the fire, the camp, home and the night's trades; the dawn
/// raises market work and lowers leisure; the morning lowers leisure; supper is raised within <see cref="MealWindowHours"/> of the
/// bot's own meal hours. The whole is held in <see cref="Least"/>–<see cref="Most"/>.
/// </para>
///
/// <para>
/// <b>The floor is Patrick's rule: a factor without a floor is a veto.</b> A markdown is not applied to paid work for a bot
/// that is broke — spendable gold under <see cref="BrokeUnder"/>, or short of what its own plans cost (<c>BotUrges.Need</c>) —
/// and is counted in <see cref="Floored"/>. A starving or broke bot still works at night; the cycle only ever raises what fits
/// the hour for it. Unpaid leisure is still marked down by day for everybody, which is the point of a day.
/// </para>
///
/// <para>
/// <b>Who rests, not how many.</b> The prime time's bands and shares (100/75/50 %) stay exactly Patrick's; the rest clock
/// (<c>BotRest.Look</c>) asks <see cref="AtBed"/> to put the bots whose bedtime it is first in the queue to tire, and lets one at
/// its bedtime tire before its hours once it has played <see cref="BedPlayedShare"/> of them; <see cref="Awake"/> puts the ones
/// whose wake hour has come first in the queue back; <see cref="PrefersField"/> sends a field sleeper to a fire before an inn.
/// Measured on the <c>Day:</c> line and under <c>day</c> in <c>/api/state</c>: offers taken by part of the day and by group,
/// tavern guests by hour, who is asleep where, and every gate's own counter.
/// </para>
/// </summary>
public static class BotDay
{
    /// <summary>Whether the cycle runs at all. Off, every factor is one and the rest clock orders by hours over, as before.</summary>
    public static bool Running { get; set; } = true;

    /// <summary>The hour, local time, from which it is dawn. Five.</summary>
    public static double DawnAt { get; set; } = 5.0;

    /// <summary>The hour from which it is morning. Seven.</summary>
    public static double MorningAt { get; set; } = 7.0;

    /// <summary>The hour from which it is noon. Twelve.</summary>
    public static double NoonAt { get; set; } = 12.0;

    /// <summary>The hour from which it is afternoon. Fourteen.</summary>
    public static double AfternoonAt { get; set; } = 14.0;

    /// <summary>The hour from which it is evening. Eighteen.</summary>
    public static double EveningAt { get; set; } = 18.0;

    /// <summary>The hour from which it is night, round to <see cref="DawnAt"/>. Twenty-two.</summary>
    public static double NightAt { get; set; } = 22.0;

    /// <summary>
    /// How far the part of the day leans a price that fits it: ×Lift up, ×(1/Lift) down; half the lean on leisure that is not the
    /// bot's own habit. 1.5: the nature's 1.15 was measured to move offers without swinging the island's totals, and an evening
    /// has to beat a trade's ×1.15 calling and the fireside's claim of 24/min against a hunt's 40 to be seen at all.
    /// </summary>
    public static double Lift { get; set; } = 1.5;

    /// <summary>The least the whole factor may be. A half: never nought, by Patrick's rule.</summary>
    public static double Least { get; set; } = 0.5;

    /// <summary>The most the whole factor may be. Two.</summary>
    public static double Most { get; set; } = 2.0;

    /// <summary>Spendable gold under which a bot is broke and no markdown touches its paid work. A hundred, the island's poverty line.</summary>
    public static int BrokeUnder { get; set; } = 100;

    /// <summary>How far either side of a meal hour supper is raised. An hour.</summary>
    public static double MealWindowHours { get; set; } = 1.0;

    /// <summary>
    /// A walk this many tiles long or longer is a journey whatever the work, and marked down in the evening and at night. Two
    /// hundred: the nature's far walk (<see cref="BotTraits.FarTiles"/> 120) reaches most of a town's fields and would have made
    /// nearly every out-of-town paid offer a journey (review A of 13.10.2026, §3.5); under the inn's reach of 400, the walk home.
    /// Its own dial so the "roads empty" effect can be widened or narrowed without touching the natures.
    /// </summary>
    public static int JourneyTiles { get; set; } = 200;

    /// <summary>How many hours later a night owl lives its day. Ten: to bed about six, up about one, its evening at three in the morning.</summary>
    public static double OwlShift { get; set; } = 10.0;

    /// <summary>The share of fighters (Warrior, Archer, Brawler, Tamer, the hybrids) drawn as night owls. A fifth.</summary>
    public static double OwlShareFighters { get; set; } = 0.2;

    /// <summary>The share of everybody else drawn as night owls; offices never. A twentieth.</summary>
    public static double OwlShareOthers { get; set; } = 0.05;

    /// <summary>
    /// The hour a day bot's bedtime is drawn from, and how many hours the draw spans: 21–24 before the nature leans it. Dials
    /// because the prime time's evening band begins at 16:00 and fills its offline share at once, so a bedtime after it waits for
    /// the 16:30 leavers to come back; set BedFrom to the band's start to have the band filled by bedtimes. A draw is kept in the
    /// store, so a change reaches newcomers and a reset world, never the bots already drawn.
    /// </summary>
    public static double BedFrom { get; set; } = 21.0;

    /// <summary>How many hours the day bots' bedtime draw spans from <see cref="BedFrom"/>: 21–24 at three, before the nature's lean.</summary>
    public static double BedSpan { get; set; } = 3.0;

    /// <summary>The same for a night owl: to bed between four and nine in the morning.</summary>
    public static double OwlBedFrom { get; set; } = 4.0;

    /// <summary>How many hours the owls' bedtime draw spans from <see cref="OwlBedFrom"/>. Five, so the owls leave over the morning band.</summary>
    public static double OwlBedSpan { get; set; } = 5.0;

    /// <summary>Whether a bot at its bedtime may tire before its hours are up. The number in the world stays the floor's.</summary>
    public static bool BedtimeTires { get; set; } = true;

    /// <summary>How long after its bed hour a bot counts as at its bedtime. Two hours.</summary>
    public static double BedWindowHours { get; set; } = 2.0;

    /// <summary>The share of its hours a bot must have played before its bedtime may tire it. A half: a bot back at 21:00 does not go to bed at 22:00.</summary>
    public static double BedPlayedShare { get; set; } = 0.5;

    /// <summary>How long after its wake hour a resting bot is one whose hour has come, and goes first under the floor. Three hours.</summary>
    public static double WakeWindowHours { get; set; } = 3.0;

    /// <summary>Offers whose price the hour raised.</summary>
    public static long Raised { get; private set; }

    /// <summary>Offers whose price the hour lowered, after the floor: a broke bot's paid work is not among them.</summary>
    public static long Lowered { get; private set; }

    /// <summary>Markdowns withheld from a broke bot's paid work: the floor.</summary>
    public static long Floored { get; private set; }

    /// <summary>Bots tired at their bedtime this session.</summary>
    public static long ToBedOnTime { get; internal set; }

    /// <summary>Of those, how many were tired before their hours were up — the bedtime alone sent them, not the session's length.</summary>
    public static long ToBedEarly { get; internal set; }

    /// <summary>Bots at their bedtime the floor kept in the world: once a bot a spell, not once a look (review A of 13.10.2026, §3.2).</summary>
    public static long HeldPastBed { get; internal set; }

    /// <summary>Resting bots brought back under the floor whose wake hour had come.</summary>
    public static long WokeOnTime { get; internal set; }

    /// <summary>Tired field sleepers sent to a fire before any inn was looked for.</summary>
    public static long FieldFirst { get; internal set; }

    /// <summary>Tired field sleepers that found no fire and could make none, and went on to the inn. Its own count, not the open ground's.</summary>
    public static long FieldFirstMissed { get; internal set; }

    /// <summary>The groups the table counts offers in. Work is everything the day does not touch.</summary>
    internal enum Group
    {
        Work,
        Leisure,
        Journey,
        Market,
        Night
    }

    private const int Parts = 6;

    private const int Groups = 5;

    /// <summary>Offers taken, by the clock's part of the day and the group of the work.</summary>
    private static readonly long[,] _taken = new long[Parts, Groups];

    /// <summary>Guests served at a cook's tavern, by the clock's hour.</summary>
    private static readonly long[] _guests = new long[24];

    private static BotDayPart _part = BotDayPart.Noon;

    private static double _hour = 12.0;

    /// <summary>The clock's part of the day, as last read.</summary>
    public static BotDayPart Part => _part;

    /// <summary>The clock's hour of the day, as last read, from nought to twenty-four.</summary>
    public static double Hour => _hour;

    /// <summary>Reads the clock. From the module's tick; local time, as the prime time reads it.</summary>
    public static void Refresh(DateTime now)
    {
        var local = now.Kind == DateTimeKind.Utc ? now.ToLocalTime() : now;

        _hour = local.TimeOfDay.TotalHours;
        _part = PartAt(_hour);
    }

    /// <summary>The part of the day this hour falls in. The hours are checked in order at the load, so a plain ladder serves.</summary>
    public static BotDayPart PartAt(double hour)
    {
        hour = BotTimetables.Wrap(hour);

        if (hour >= NightAt || hour < DawnAt)
        {
            return BotDayPart.Night;
        }

        if (hour < MorningAt)
        {
            return BotDayPart.Dawn;
        }

        if (hour < NoonAt)
        {
            return BotDayPart.Morning;
        }

        if (hour < AfternoonAt)
        {
            return BotDayPart.Noon;
        }

        if (hour < EveningAt)
        {
            return BotDayPart.Afternoon;
        }

        return BotDayPart.Evening;
    }

    /// <summary>Whether the six hours stand in order inside the day. Checked at the load; out of order, the code's own stand.</summary>
    public static bool HoursInOrder() =>
        DawnAt >= 0.0 && DawnAt < MorningAt && MorningAt < NoonAt && NoonAt < AfternoonAt && AfternoonAt < EveningAt && EveningAt < NightAt && NightAt <= 24.0;

    /// <summary>
    /// Whether this bot lives by night: drawn so, or a thief of The Shadow now (<c>BotCrime.IsThief</c>, asked once a minute a
    /// bot rather than once an offer).
    /// </summary>
    internal static bool Owl(BotMobile bot, BotTimetable table)
    {
        if (table.Owl)
        {
            return true;
        }

        var now = Core.TickCount;

        if (!table.ThiefChecked || now - table.ThiefCheckedTick >= 60000)
        {
            table.ThiefChecked = true;
            table.ThiefCheckedTick = now;
            table.Thief = BotCrime.IsThief(bot);
        }

        return table.Thief;
    }

    /// <summary>The bot's own part of the day: the clock's, or ten hours earlier for a night owl.</summary>
    private static BotDayPart PartFor(bool owl) => owl ? PartAt(_hour - OwlShift) : _part;

    /// <summary>A drawn hour, shifted for a bot that became an owl after its draw.</summary>
    private static double OwnHour(BotTimetable table, bool owl, double hour) =>
        owl && !table.Owl ? BotTimetables.Wrap(hour + OwlShift) : hour;

    private static bool Near(double hour, double at) =>
        Math.Min(BotTimetables.Wrap(hour - at), BotTimetables.Wrap(at - hour)) <= Math.Max(0.0, MealWindowHours);

    /// <summary>What the part of the day says to a kind of work.</summary>
    internal static Group GroupOf(string kind, Mobile body, BotDeed deed)
    {
        switch (kind)
        {
            case "supper":
            case "fireside":
            case "camp":
            case "home":
            case "chat":
            case "meet":
                return Group.Leisure;
            case "travel":
            case "voyage":
            case "venture":
            case "expedition":
            case "caravan":
            case "delve":
            case "scout":
            case "scout-rumour":
            case "prospect":
            case "colony-run":
            case "escort":
            case "guard":
                return Group.Journey;
            case "trade":
            case "peddle":
            case "shopkeep":
            case "restock":
            case "resell":
            case "order":
            case "acquire":
            case "supply":
            case "haul":
            case "deliver":
            case "unload":
            case "fetch":
            case "fair":
            case "keep-store":
            // The cook's keeping (BotKeepTavern) is paid work with a counter: market work, filed under leisure it was marked
            // down ×0.67 for every solvent cook at 05–12, the hours the markets should fill (review A of 13.10.2026, §3.1).
            case "tavern":
                return Group.Market;
            case "watch":
            case "guard-duty":
            case "ward":
            case "skulk":
            case "steal":
            case "burgle":
            case "rob":
            case "waylay":
            case "ambush":
            case "lielow":
            case "manhunt":
                return Group.Night;
        }

        // A long walk is a journey whatever it is for. See JourneyTiles.
        if (deed != null && body != null && deed.Map == body.Map && deed.Where != Point3D.Zero
            && Math.Max(Math.Abs(body.Location.X - deed.Where.X), Math.Abs(body.Location.Y - deed.Where.Y)) >= JourneyTiles)
        {
            return Group.Journey;
        }

        return Group.Work;
    }

    /// <summary>Whether a kind of leisure is this bot's own evening habit. A homebody with no house is a tavern guest.</summary>
    private static bool Fits(BotTimetable table, string kind, BotMobile bot)
    {
        var habit = table.Habit;

        if (habit == BotHabit.Home && BotAbode.Of(bot) == null)
        {
            habit = BotHabit.Tavern;
        }

        return habit switch
        {
            BotHabit.Tavern => kind is "supper",
            BotHabit.Fireside => kind is "fireside" or "camp",
            BotHabit.Home => kind is "home",
            _ => false
        };
    }

    /// <summary>Whether a bot is short of money: under the poverty line in hand, or short of what its own plans cost.</summary>
    private static bool Broke(BotMobile bot) =>
        BotYield.Spendable(bot) < BrokeUnder || (bot.Resolve?.Urges?.Need ?? 0.0) > 0.0;

    /// <summary>
    /// The hour's multiplier on one offer, and why (for the take line). One when the cycle is off, for anything not a bot, and
    /// for work the hour does not touch, which is most of it.
    /// </summary>
    public static double Factor(Mobile body, BotDeed deed, out string why)
    {
        why = null;

        if (!Running || deed == null || body is not BotMobile bot)
        {
            return 1.0;
        }

        var table = BotTimetables.Of(bot);

        if (table == null)
        {
            return 1.0;
        }

        var kind = deed.Kind;
        var group = GroupOf(kind, body, deed);
        var owl = Owl(bot, table);
        var own = PartFor(owl);
        var lift = Math.Max(1.0, Lift);
        var half = 1.0 + (lift - 1.0) / 2.0;
        var drop = 1.0 / lift;
        var factor = 1.0;

        switch (own)
        {
            case BotDayPart.Dawn:
                if (group == Group.Market)
                {
                    factor = lift;
                    why = "the dawn market";
                }
                else if (group == Group.Leisure)
                {
                    factor = drop;
                    why = "the dawn, no hour to sit";
                }

                break;
            case BotDayPart.Morning:
                if (group == Group.Leisure)
                {
                    factor = drop;
                    why = "the morning's work";
                }

                break;
            case BotDayPart.Evening:
                // The prowler before the journey test: a far prowl is its evening, not a journey (review A, §3.6).
                if (table.Habit == BotHabit.Prowl && kind is "prowl" or "stroll")
                {
                    factor = lift;
                    why = "the evening (prowl)";
                }
                else if (group == Group.Leisure)
                {
                    var fits = Fits(table, kind, bot);

                    factor = fits ? lift : half;
                    why = fits ? $"the evening ({BotTimetable.Word(table.Habit)})" : "the evening";
                }
                else if (kind == "tavern")
                {
                    // The cook's full house: market work by day, lifted in the evening, never marked down.
                    factor = lift;
                    why = "the evening's guests";
                }
                else if (group == Group.Journey)
                {
                    factor = drop;
                    why = "the evening, too late to set out";
                }

                break;
            case BotDayPart.Night:
                if (group == Group.Journey)
                {
                    factor = drop;
                    why = "the night, no hour to set out";
                }
                else if (kind is "fireside" or "camp" or "home")
                {
                    factor = lift;
                    why = "the night, by a fire or at home";
                }

                break;
        }

        // The night's trades go by the clock's night, owl or not: thieves come out and the watch walks when it is dark.
        if (group == Group.Night && _part == BotDayPart.Night && factor < lift)
        {
            factor = lift;
            why = "the night, when the watch walks";
        }

        // Supper at the bot's own meal hours, whatever the part.
        if (kind == "supper" && factor < lift && (Near(_hour, OwnHour(table, owl, table.Meal)) || Near(_hour, OwnHour(table, owl, table.Supper))))
        {
            factor = lift;
            why = "its meal hour";
        }

        if (factor == 1.0)
        {
            why = null;

            return 1.0;
        }

        // The floor: a broke bot's paid work is never marked down by the hour.
        if (factor < 1.0 && !deed.Unpaid && Broke(bot))
        {
            Floored++;
            why = null;

            return 1.0;
        }

        factor = Math.Clamp(factor, Math.Min(Least, 1.0), Math.Max(Most, 1.0));

        if (factor > 1.0)
        {
            Raised++;
        }
        else
        {
            Lowered++;
        }

        return factor;
    }

    /// <summary>
    /// Work was taken up: counted by the clock's part and the work's group. From <c>BotWill</c> at the auction's take and at a
    /// press (<c>BotWill.Press</c>: the walks to bed — lodge, home to rest, sleep by a fire — are presses, and without them the
    /// evening's leisure read low; review A of 13.10.2026, §3.4).
    /// </summary>
    public static void Took(Mobile body, BotDeed deed)
    {
        if (!Running || deed == null)
        {
            return;
        }

        _taken[(int)_part, (int)GroupOf(deed.Kind, body, deed)]++;
    }

    /// <summary>A guest was served at a cook's tavern. From <c>BotTaverns</c>.</summary>
    public static void Guest()
    {
        if (Running)
        {
            _guests[Math.Clamp((int)_hour, 0, 23)]++;
        }
    }

    /// <summary>
    /// Whether it is this bot's bedtime — within <see cref="BedWindowHours"/> of its bed hour — and it has played enough of its hours
    /// (<see cref="BedPlayedShare"/>) to go. From <c>BotRest.Look</c>; <paramref name="pastBed"/> is how far into the window it is.
    /// </summary>
    public static bool AtBed(BotMobile bot, double playedShare, out double pastBed)
    {
        pastBed = 0.0;

        if (!Running || !BedtimeTires || BotTimetables.Of(bot) is not { } table)
        {
            return false;
        }

        var past = BotTimetables.Wrap(_hour - OwnHour(table, Owl(bot, table), table.Bed));

        if (past >= Math.Max(0.0, BedWindowHours) || playedShare < BedPlayedShare)
        {
            return false;
        }

        pastBed = past;

        return true;
    }

    /// <summary>Whether this resting bot's wake hour has come, within <see cref="WakeWindowHours"/>. From <c>BotRest.Look</c>.</summary>
    public static bool Awake(BotMobile bot)
    {
        if (!Running || BotTimetables.Of(bot) is not { } table)
        {
            return false;
        }

        return BotTimetables.Wrap(_hour - OwnHour(table, Owl(bot, table), table.Wake)) < Math.Max(0.0, WakeWindowHours);
    }

    /// <summary>Whether this bot would rather sleep by a fire than at an inn. From <c>BotRest.Look</c>, after the walk home.</summary>
    public static bool PrefersField(BotMobile bot) =>
        Running && BotTimetables.Of(bot) is { Place: BotBedPlace.Field };

    private static readonly string[] PartWords = ["dawn", "morning", "noon", "afternoon", "evening", "night"];

    private static readonly string[] GroupWords = ["work", "leisure", "journey", "market", "night"];

    public static string Word(BotDayPart part) => PartWords[(int)part];

    /// <summary>The hours, for the boot line: "dawn 05–07, morning 07–12, …, night 22–05".</summary>
    public static string Schedule() =>
        $"dawn {DawnAt:00}–{MorningAt:00}, morning {MorningAt:00}–{NoonAt:00}, noon {NoonAt:00}–{AfternoonAt:00}, afternoon {AfternoonAt:00}–{EveningAt:00}, evening {EveningAt:00}–{NightAt:00}, night {NightAt:00}–{DawnAt:00}";

    /// <summary>The hour as a clock reads it.</summary>
    private static string Clock(double hour) => $"{(int)hour:00}:{(int)((hour - Math.Floor(hour)) * 60.0):00}";

    /// <summary>How the timetables fall: owls, habits and beds.</summary>
    private static void Mix(out int owls, out int thieves, Span<int> habits, Span<int> places)
    {
        owls = 0;
        thieves = 0;

        foreach (var table in BotTimetables.All)
        {
            if (table.Owl)
            {
                owls++;
            }
            else if (table.Thief)
            {
                thieves++;
            }

            habits[(int)table.Habit]++;
            places[(int)table.Place]++;
        }
    }

    public static string Describe()
    {
        if (!Running)
        {
            return "the daily cycle is OFF: every hour is one";
        }

        Span<int> habits = stackalloc int[4];
        Span<int> places = stackalloc int[4];

        Mix(out var owls, out var thieves, habits, places);
        BotRest.Sleepers(out var home, out var inn, out var field, out var ground);

        using var line = ValueStringBuilder.Create(768);

        line.Append($"{Word(_part)} at {Clock(_hour)} ({Schedule()}); {BotTimetables.Count} timetables ({owls} night owls");

        if (thieves > 0)
        {
            line.Append($" and {thieves} thieves living by night");
        }

        line.Append($"; evenings: tavern {habits[0]}, fireside {habits[1]}, home {habits[2]}, prowl {habits[3]}; beds: inn {places[1]}, field {places[2]}); ");
        line.Append($"offers raised {Raised} and lowered {Lowered} (×{1.0 / Math.Max(1.0, Lift):F2}–×{Lift:F2}, held in ×{Least:F2}–×{Most:F2}), {Floored} markdowns withheld from broke bots (under {BrokeUnder}gp or in need); taken by part:");

        for (var p = 0; p < Parts; p++)
        {
            var total = 0L;

            for (var g = 0; g < Groups; g++)
            {
                total += _taken[p, g];
            }

            line.Append(p == 0 ? " " : ", ");
            line.Append($"{PartWords[p]} {total} (");

            for (var g = 0; g < Groups; g++)
            {
                line.Append(g == 0 ? "" : " ");
                line.Append($"{GroupWords[g]} {_taken[p, g]}");
            }

            line.Append(')');
        }

        line.Append("; tavern guests by hour:");

        var any = false;

        for (var h = 0; h < 24; h++)
        {
            if (_guests[h] > 0)
            {
                line.Append(any ? ", " : " ");
                line.Append($"{h:00}h {_guests[h]}");
                any = true;
            }
        }

        if (!any)
        {
            line.Append(" none");
        }

        line.Append($"; asleep now: {home} at home, {inn} at an inn, {field} by a fire, {ground} on the open ground; ");
        line.Append($"this session {ToBedOnTime} went to bed at their bedtime ({ToBedEarly} before their hours, {HeldPastBed} held past it by the floor), {WokeOnTime} woke at their hour, {FieldFirst} chose a fire over an inn and {FieldFirstMissed} found none and went to the inn");

        return line.ToString();
    }

    /// <summary>The <c>day</c> key of <c>/api/state</c>: the same as the line, as numbers.</summary>
    public static void Write(Utf8JsonWriter w)
    {
        w.WritePropertyName("day");
        w.WriteStartObject();
        w.WriteBoolean("running", Running);
        w.WriteString("part", Word(_part));
        w.WriteNumber("hour", Math.Round(_hour, 2));

        w.WritePropertyName("parts");
        w.WriteStartObject();
        w.WriteNumber("dawn", DawnAt);
        w.WriteNumber("morning", MorningAt);
        w.WriteNumber("noon", NoonAt);
        w.WriteNumber("afternoon", AfternoonAt);
        w.WriteNumber("evening", EveningAt);
        w.WriteNumber("night", NightAt);
        w.WriteEndObject();

        Span<int> habits = stackalloc int[4];
        Span<int> places = stackalloc int[4];

        Mix(out var owls, out var thieves, habits, places);

        w.WriteNumber("timetables", BotTimetables.Count);
        w.WriteNumber("owls", owls);
        w.WriteNumber("thieves", thieves);

        w.WritePropertyName("habits");
        w.WriteStartObject();
        w.WriteNumber("tavern", habits[0]);
        w.WriteNumber("fireside", habits[1]);
        w.WriteNumber("home", habits[2]);
        w.WriteNumber("prowl", habits[3]);
        w.WriteEndObject();

        w.WritePropertyName("beds");
        w.WriteStartObject();
        w.WriteNumber("inn", places[1]);
        w.WriteNumber("field", places[2]);
        w.WriteEndObject();

        w.WritePropertyName("taken");
        w.WriteStartArray();

        for (var p = 0; p < Parts; p++)
        {
            w.WriteStartObject();
            w.WriteString("part", PartWords[p]);

            for (var g = 0; g < Groups; g++)
            {
                w.WriteNumber(GroupWords[g], _taken[p, g]);
            }

            w.WriteEndObject();
        }

        w.WriteEndArray();

        w.WritePropertyName("guestsByHour");
        w.WriteStartArray();

        for (var h = 0; h < 24; h++)
        {
            w.WriteNumberValue(_guests[h]);
        }

        w.WriteEndArray();

        BotRest.Sleepers(out var home, out var inn, out var field, out var ground);

        w.WritePropertyName("asleep");
        w.WriteStartObject();
        w.WriteNumber("home", home);
        w.WriteNumber("inn", inn);
        w.WriteNumber("field", field);
        w.WriteNumber("ground", ground);
        w.WriteEndObject();

        w.WriteNumber("raised", Raised);
        w.WriteNumber("lowered", Lowered);
        w.WriteNumber("floored", Floored);
        w.WriteNumber("toBedOnTime", ToBedOnTime);
        w.WriteNumber("toBedEarly", ToBedEarly);
        w.WriteNumber("heldPastBed", HeldPastBed);
        w.WriteNumber("wokeOnTime", WokeOnTime);
        w.WriteNumber("fieldFirst", FieldFirst);
        w.WriteNumber("fieldFirstMissed", FieldFirstMissed);
        w.WriteEndObject();
    }

    /// <summary>A bot's timetable on its page. Called beside <c>BotCharacterWeb.Write</c>.</summary>
    public static void WriteBot(Utf8JsonWriter w, BotMobile bot)
    {
        if (bot?.Name == null || BotTimetables.Find(bot.Name) is not { Drawn: true } table)
        {
            return;
        }

        w.WritePropertyName("day");
        w.WriteStartObject();
        w.WriteNumber("wake", Math.Round(table.Wake, 1));
        w.WriteNumber("bed", Math.Round(table.Bed, 1));
        w.WriteNumber("meal", Math.Round(table.Meal, 1));
        w.WriteNumber("supper", Math.Round(table.Supper, 1));
        w.WriteString("habit", BotTimetable.Word(table.Habit));
        w.WriteBoolean("owl", table.Owl || table.Thief);
        w.WriteString("bedPlace", BotTimetable.Word(table.Place));
        w.WriteEndObject();
    }

    /// <summary>The session's counters forgotten, on a reload. The timetables themselves are the store's.</summary>
    public static void Forget()
    {
        Raised = 0;
        Lowered = 0;
        Floored = 0;
        ToBedOnTime = 0;
        ToBedEarly = 0;
        HeldPastBed = 0;
        WokeOnTime = 0;
        FieldFirst = 0;
        FieldFirstMissed = 0;
        Array.Clear(_taken);
        Array.Clear(_guests);
    }
}
