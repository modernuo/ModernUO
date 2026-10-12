using System;
using System.Collections.Generic;
using Server.BotAI.V2;
using Server.Text;

namespace Server.BotAI.Mind;

/// <summary>
/// The roll-call: every two minutes, three questions asked of every bot, and a hand laid on the ones that
/// answer no to all of them.
///
/// <para>
/// <b>It asks no model anything, and that is the first decision here.</b> The three questions — did it get
/// where it was going, did it finish what it took on, did anything change at all — are arithmetic. A model
/// is worth asking why a bot is stuck; it is not worth asking whether one is, and a check that depends on a
/// model is a check that does not run while the card is busy. This runs on a two-minute clock whatever else
/// is happening, costs one pass over the roll, and cannot fail to fire.
/// </para>
///
/// <para>
/// <b>It is also the point at which the debugger stops being only an observer, and that is worth saying out
/// loud.</b> Everything else in this folder was built so the watcher could not affect what it watched. From
/// here it can: it clears a stale route, and if that does not help it ends the piece of work. So every
/// intervention is written down with what was true before it and what was true two minutes after, and the
/// counts are reported separately from everything else. An intervention nobody measures is not a fix, it is
/// a second source of behaviour nobody can account for — and this shard already knows what that costs.
/// </para>
///
/// <para>
/// <b>Two strengths, escalated, because they mean different things.</b> A remind throws the route away and
/// keeps the destination: the bot still wants the same thing and draws a fresh path to it, which is the
/// whole cure when the plan has gone stale under it. A shake ends the undertaking as a failure, so the
/// ledger learns that place was no good and the auction offers something else. The first is cheap and
/// reversible and is tried first; the second throws away work in hand and is only for a bot that was
/// reminded and did not take it.
/// </para>
/// </summary>
public static class BotAudit
{
    /// <summary>How long a bot is given to get somewhere, finish something, or change in any way at all.</summary>
    public static int WindowMs { get; set; } = 120000;

    /// <summary>
    /// How many bots may be touched in one window.
    ///
    /// <para>
    /// <b>A cap, because the failure mode of this file is a population being shaken every two minutes by the
    /// thing that is supposed to be diagnosing it.</b> If more than a handful are stuck at once the fault is
    /// not in those bots and no amount of shaking them will help; what that wants is the finding, not the
    /// hand. So the worst few are touched, the rest are counted and named in the log, and the number that
    /// went untouched is reported — a cap nobody is told about is a silent truncation.
    /// </para>
    /// </summary>
    public static int MostTouched { get; set; } = 4;

    /// <summary>How long a bot is left alone after being touched, so a cure has time to work or not.</summary>
    public static int RestMs { get; set; } = 300000;



    /// <summary>What was true about one bot when the window opened.</summary>
    private sealed class Mark
    {
        public string Kind;

        /// <summary>Which piece of work it was, not what it was called. See <see cref="BotWatch.Takes"/>.</summary>
        public long Takes;

        public Point3D Where;

        public Point3D Goal;

        public bool Going;

        public int Worth;

        public double Progress;

        public double Mood;

        public bool Stuck;

        public bool Touched;

        public long TouchedTick;

        public int Reminds;

        public int Shakes;
    }

    private static readonly Dictionary<Serial, Mark> _marks = [];

    private static long _sweptTick;

    private static bool _opened;

    /// <summary>Windows run, bots found stuck, and what was done about it. Each counted apart.</summary>
    public static long Windows { get; private set; }

    public static long Stuck { get; private set; }

    public static long Reminded { get; private set; }

    public static long Shaken { get; private set; }

    /// <summary>
    /// Why a stuck bot was left alone, counted apart rather than summed.
    ///
    /// Merged into one "untouched" figure these say opposite things: a population where everybody is in a
    /// fight is healthy and one where the cap is being hit every window is not, and a single number cannot
    /// tell the two apart. Same rule as every other tally on this shard.
    /// </summary>
    public static long LeftFighting { get; private set; }

    public static long LeftResting { get; private set; }

    public static long LeftCapped { get; private set; }

    /// <summary>
    /// Bots that were stuck, were touched, and were not stuck when the next window closed.
    ///
    /// <para>
    /// The only number here that says whether any of this is worth doing. Without it the log says "four bots
    /// were shaken" for ever and nobody can tell a cure from a habit.
    /// </para>
    /// </summary>
    public static long Freed { get; private set; }

    public static long StillStuck { get; private set; }

    /// <summary>
    /// Bots that answered no to every question while holding work that stands still on purpose, left to that work's own
    /// clock. See <see cref="BotDeed.Still"/>.
    ///
    /// <para>
    /// <b>The shard's own watch read the flag; this one did not.</b> <c>BotStall</c> passes over work that says it stands
    /// still — a lesson, a roll, a sentence — and this hand ended it. On build 101 Nessa Ashdown, caught by Gerda's patrol
    /// and put in a cell, had its sentence ended "the debugger found it stuck and ended it" at 04:50 and 04:58; each time
    /// it took a walk home out of the jail, where nobody may travel out, and failed "no way through to (1440, 1470, 0)"
    /// every half-minute until the breaker rested the walk — 23 walks home and 6 reclaims in half an hour, the largest
    /// failure of the window, out of one bot — and each time the outlaw clock pressed the sentence on it again.
    /// </para>
    /// </summary>
    public static long OnPurpose { get; private set; }

    /// <summary>What the last window found, in words, for the model and for the log.</summary>
    public static string Last { get; private set; } = "No window has closed yet.";

    public static void Reset()
    {
        _marks.Clear();
        _opened = false;
        _sweptTick = Core.TickCount;

        Windows = 0;
        Stuck = 0;
        Reminded = 0;
        Shaken = 0;
        LeftFighting = 0;
        LeftResting = 0;
        LeftCapped = 0;
        Freed = 0;
        StillStuck = 0;
        OnPurpose = 0;
        Last = "No window has closed yet.";
    }

    /// <summary>Whether the window has run out. Called every sample; true twice a minute at most.</summary>
    public static bool Due(long now)
    {
        if (!_opened)
        {
            // Seeded from a real tick rather than left at zero. On a host whose counter starts enormous,
            // zero is not "never", it is a moment eleven days in the past — and the first window would close
            // instantly on a population nobody had watched yet.
            _opened = true;
            _sweptTick = now;

            return false;
        }

        return now - _sweptTick >= WindowMs;
    }

    /// <summary>
    /// One window. Closes the last one, judges everybody, lays a hand on the worst, and opens the next.
    /// </summary>
    public static void Sweep(long now, IReadOnlyList<BotWatch> roll)
    {
        _sweptTick = now;
        Windows++;

        var sb = ValueStringBuilder.Create(1024);

        try
        {
            var seen = 0;
            var fresh = 0;
            var arrived = 0;
            var short_ = 0;
            var noGoal = 0;
            var movedOn = 0;
            var chasing = 0;
            var fighting = 0;
            var finished = 0;
            var holding = 0;
            var idle = 0;
            var moved = 0;
            var rooted = 0;
            var richer = 0;
            var flat = 0;
            var sadder = 0;

            List<(BotWatch Watch, Mark Mark, string Why)> stuck = [];

            for (var i = 0; i < roll.Count; i++)
            {
                var watch = roll[i];
                var bot = watch.Bot;

                if (bot is not { Deleted: false })
                {
                    continue;
                }

                // <b>Enrolled is not judged, and saying otherwise was this file's first defect.</b> A bot
                // seen for the first time has nothing to be compared against — there is no earlier mark, so
                // no question can be answered about it. The first roll-call counted all thirty-eight as
                // asked and then reported nought to every question, which is precisely the shape of summary
                // this whole project exists to refuse: it named a denominator it had not measured.
                if (!_marks.TryGetValue(bot.Serial, out var mark))
                {
                    fresh++;
                    _marks[bot.Serial] = Take(watch, now);

                    continue;
                }

                seen++;

                // 1. It took something on. Did it finish it? Asked first, because the answer decides
                // whether the next question means anything at all.
                // <b>The same piece of work, not the same name for one.</b> Comparing kinds says a bot that
                // finished an unload and took another unload never finished anything — and this file acts on
                // that answer: it throws routes away and ends work as failed. On 03.09.2026 at 05:56 it
                // reminded Lysa the Warrior about a walk it had been on for 26 seconds, because the unload
                // before it had the same name. Takes counts the pieces themselves.
                var over = mark.Takes != watch.Takes;

                if (mark.Kind == "-")
                {
                    idle++;
                }
                else if (over)
                {
                    finished++;
                }
                else
                {
                    holding++;
                }

                // 2. It was going somewhere. Did it get there?
                //
                // <b>Asked only of the bots still on the same piece of work, and the first version asked it
                // of everybody.</b> A bot that finished its job two minutes ago and took another is walking
                // somewhere else entirely; measuring it against the destination it has rightly abandoned
                // counted seventeen bots that had just succeeded as "still short of it". The number was true
                // — they are indeed not at that old place — and it answered a question nobody was asking.
                // Where it went is now told by the work question, which is where it belongs.
                var reached = false;

                // <b>Not asked of a bot that had nothing two minutes ago, and asking it was an off-by-one in
                // print.</b> Question one sorts every bot into idle, finished or holding; question two sorted
                // the same bots into movedOn and four destination cases — but it used only the "did the work
                // change" test, so a bot that held nothing then and holds nothing now answered "idle" above
                // and "not going anywhere" below, and a bot that held nothing then and something now was
                // counted as having ended a piece of work it never had. The two sentences then disagreed by
                // exactly that many: on 03.09.2026 at 00:17 the roll-call read "Of the 7 still on the same
                // piece of work ... 0 arrived, 5 short, 2 not going anywhere, 1 chasing", which is eight, and
                // the same shape appeared at 22:03 the evening before with 24 against 25. A summary whose own
                // two halves do not add up teaches whoever reads it to trust neither.
                //
                // With this, the halves are the same partition twice: finished equals movedOn, and holding
                // equals the four cases below.
                if (mark.Kind == "-")
                {
                    // It had no work to be going anywhere for. Counted as idle above and nowhere here.
                }
                else if (over)
                {
                    movedOn++;
                }
                else if (watch.Following)
                {
                    // Chasing something that moves. There is no place it was going to: the destination is
                    // wherever that creature now stands, and the mark holds where it stood two minutes ago.
                    chasing++;
                }
                else if (!mark.Going)
                {
                    noGoal++;
                }
                else if (bot.Map != null && Math.Max(Math.Abs(bot.X - mark.Goal.X), Math.Abs(bot.Y - mark.Goal.Y)) <= Math.Max(1, watch.Slack))
                {
                    arrived++;
                    reached = true;
                }
                else
                {
                    short_++;
                }

                // 3. Did anything at all change?
                var stirred = Math.Max(Math.Abs(bot.X - mark.Where.X), Math.Abs(bot.Y - mark.Where.Y)) > BotWatch.PacingSpan;

                if (stirred)
                {
                    moved++;
                }
                else
                {
                    rooted++;
                }

                var gained = watch.Worth > mark.Worth || watch.Progress > mark.Progress + 0.0005;

                if (gained)
                {
                    richer++;
                }
                else
                {
                    flat++;
                }

                if (watch.Mood < mark.Mood - 0.01)
                {
                    sadder++;
                }

                // <b>All of it, not any of it.</b> Standing still is ordinary, holding one job for two
                // minutes is ordinary, and earning nothing for two minutes is ordinary — a smith at an anvil
                // does all three and is working perfectly. What is not ordinary is a bot that did not
                // arrive, did not finish, did not move off its patch and is no better off than it was: four
                // ways of getting somewhere, and it took none of them.
                // <b>And it is not a fight, which cost a whole night to learn.</b> A bot trading blows is
                // standing next to something, so it has not "arrived" anywhere new, has not finished, does
                // not leave a two-tile patch, and is no richer until the thing dies — all four tests, failed,
                // by a bot doing exactly what it should. On the night of 01.09.2026 that produced 1010
                // reminders and shakes and freed nobody at all, because nobody was stuck: the debugger spent
                // the night interrupting fighters. Whether a fight is going anywhere is measured separately
                // and reported separately — see BotWatch.SwingingMs — and it is not this question.
                if (watch.Fighting)
                {
                    fighting++;
                }

                var wedged = !reached && !over && !stirred && !gained && !watch.Fighting && !watch.Following
                             && mark.Kind != "-";

                if (wedged && bot.Resolve?.Deed is { Still: true })
                {
                    OnPurpose++;
                    wedged = false;
                }

                // <b>The window's new mark is made and filed FIRST, and everything after this works on it.</b>
                // It used to be the other way round: the old mark went into the stuck list, the dictionary was
                // then given a fresh one built from it, and every note the hand made afterwards — that this
                // bot had been touched, when, how often — was written into an object nobody would ever read
                // again. So the five-minute rest between touches never applied, a remind never escalated to a
                // shake, and the one number that says whether any of this works could not be counted at all.
                //
                // Measured, 02.09.2026: Doran the Crafter was reminded at 10:28, 10:30, 10:32, 10:34 and
                // 10:36 — every window, two minutes apart, against a rest of five — while its mine went from
                // 200s to 561s held and it never moved off 1360,1559. And across the night before: 1010
                // reminders, 0 shakes, 0 freed. Neither figure was a fact about the population.
                //
                // The new mark carries Touched, Reminds, Shakes and Stuck over from the old one, so nothing
                // is lost by filing it early — and `current.Touched` still means "was it touched before this
                // window", which is the question the recovery test asks.
                var current = Take(watch, now, mark);

                _marks[bot.Serial] = current;

                Was(watch, current, now, wedged, ref stuck, current.Touched);
            }

            Say(ref sb, seen, fresh, arrived, short_, noGoal, movedOn, chasing, fighting, finished, holding, idle, moved, rooted, richer, flat, sadder, stuck.Count);

            Touch(now, stuck, ref sb);

            Last = sb.ToString();

            BotDebugLog.Rule();
            BotDebugLog.Block($"ROLL-CALL {Windows} — every bot, over the last {WindowMs / 1000} seconds", Last);
            BotDebugLog.Rule();
        }
        finally
        {
            sb.Dispose();
        }
    }

    /// <summary>
    /// Books the verdict on one bot, and asks whether a hand laid on it last window did any good.
    /// </summary>
    private static void Was(
        BotWatch watch,
        Mark mark,
        long now,
        bool wedged,
        ref List<(BotWatch, Mark, string)> stuck,
        bool touched
    )
    {
        // Read before it is overwritten: the question "was it stuck last window" and the answer "is it stuck
        // now" are the same field a moment apart, and taking them in the wrong order makes every recovery
        // invisible.
        var was = mark.Stuck;

        mark.Stuck = wedged;

        if (wedged)
        {
            Stuck++;

            if (touched)
            {
                StillStuck++;
            }

            var why = watch.Kind == "-"
                ? "it holds nothing and has not moved"
                : $"held {watch.Kind} for {watch.HeldMs / 1000}s, did not arrive, did not finish, did not leave a {BotWatch.PacingSpan}-tile patch at {watch.Where.X},{watch.Where.Y}, and is no better off than {mark.Worth}gp";

            stuck.Add((watch, mark, why));

            return;
        }

        // It is not stuck now. If it was stuck and was touched, that is the only evidence this file produces
        // that any of it works.
        if (touched && was)
        {
            Freed++;

            BotDebugLog.Write(
                $"    {watch.Name} was {(mark.Shakes > 0 ? "shaken" : "reminded")} and is going again: it now holds {watch.Kind} at {watch.Where.X},{watch.Where.Y}, worth {watch.Worth}gp against {mark.Worth}gp"
            );
        }
    }

    /// <summary>
    /// Lays a hand on the worst of them, gently first.
    ///
    /// <para>
    /// Only on the two rungs where a bot is choosing its own business. A bot that is bleeding, being hit or
    /// marching with a company is not stuck — it is busy with something no opinion of this file's is wanted
    /// in, and ending its work mid-fight would be the debugger causing exactly the kind of harm it exists to
    /// find.
    /// </para>
    /// </summary>
    private static void Touch(long now, List<(BotWatch Watch, Mark Mark, string Why)> stuck, ref ValueStringBuilder sb)
    {
        if (stuck.Count == 0)
        {
            return;
        }

        // Worst first, so a cap spends itself on the bots that have been stuck longest rather than on
        // whoever happens to come first in the roll.
        stuck.Sort((a, b) => b.Watch.Suspicion.CompareTo(a.Watch.Suspicion));

        var touched = 0;
        var fighting = 0;
        var resting = 0;
        var capped = 0;

        for (var i = 0; i < stuck.Count; i++)
        {
            var (watch, mark, why) = stuck[i];
            var bot = watch.Bot;

            if (bot is not { Deleted: false, Alive: true })
            {
                continue;
            }

            var standing = bot.Resolve?.Standing ?? BotStanding.Dead;

            if (standing is not (BotStanding.Free or BotStanding.Busy))
            {
                fighting++;

                continue;
            }

            if (mark.Touched && now - mark.TouchedTick < RestMs)
            {
                resting++;

                continue;
            }

            if (touched >= MostTouched)
            {
                capped++;

                continue;
            }

            touched++;

            mark.Touched = true;
            mark.TouchedTick = now;

            // Gently first. The route is thrown away and the destination kept, so the bot draws a fresh path
            // to the same place — which is the whole cure when a plan has gone stale under it, and costs
            // nothing when it is not.
            if (mark.Reminds == 0)
            {
                mark.Reminds++;
                Reminded++;

                bot.Journey?.Discard();

                sb.AppendLine($"  REMINDED {watch.Name} the {watch.Class} of where it was going — {why}. Its route is thrown away; the destination stands.");

                continue;
            }

            // It was reminded last window and is still here. End the work as a failure, which is not merely
            // dropping it: the ledger learns this place was no good, so the same errand to the same spot is
            // worth less next time and the loop does not simply start again with a longer period.
            mark.Shakes++;
            Shaken++;

            // <b>The work is given its own chance to learn before it is ended, and leaving this out made the
            // debugger the cause of the loop it was reporting.</b>
            //
            // Every undertaking that walks somewhere has a Bend: the hook the walk layer calls when a road
            // turns out not to exist, where the deed writes down that the place was no good. BotPeddle marks
            // the shopkeeper, BotDig marks the seam, BotForge marks the smithy — and the shard's vendor
            // choice then skips a shop the ledger is cautious about. That is how a bot learns to go somewhere
            // else.
            //
            // Abandon ends the deed through BotWill, which never touches the walk layer, so Bend never fired
            // and nothing was ever written down. Measured 02.09.2026: 40-odd attempts to carry three Fancy
            // Shirts to Phyllis, every one ended by the line "the debugger found it stuck and ended it", every
            // one re-offered within seconds at 72/min — while Melina buys the same shirts at 235/min and is
            // reached without trouble. Phyllis is simply nearer, and nearer is what the choice weighs when
            // nothing has marked her down.
            //
            // So Bend first. If it returns true the deed has found somewhere else to go and there is nothing
            // to end — the shake becomes "try elsewhere", which is a better cure and a cheaper one.
            var work = bot.Resolve?.Deed;

            if (work != null && work.Bend(bot))
            {
                sb.AppendLine($"  BENT {watch.Name} the {watch.Class} onto somewhere else — {why}. Its work found another place to go, so nothing was ended.");

                continue;
            }

            BotWill.Abandon(bot, "the debugger found it stuck and ended it");
            bot.Journey?.Finish();
            bot.Refusals = 0;

            sb.AppendLine($"  SHOOK {watch.Name} the {watch.Class} — {why}. It had already been reminded once. Its {mark.Kind} is ended as a failure so the ledger marks that ground down.");
        }

        LeftFighting += fighting;
        LeftResting += resting;
        LeftCapped += capped;

        // Said even when it is nought, and said with the three reasons apart. A cap nobody is told about is
        // a silent truncation, and "left alone because it is in a fight" and "left alone because I had
        // already touched four" are opposite facts about the shard.
        if (fighting + resting + capped > 0)
        {
            sb.AppendLine(
                $"  Left alone: {fighting} were in a fight or with a company, {resting} had been touched inside the last {RestMs / 60000} minutes, {capped} were past the cap of {MostTouched} a window."
            );
        }
    }

    private static void Say(
        ref ValueStringBuilder sb,
        int seen,
        int fresh,
        int arrived,
        int short_,
        int noGoal,
        int movedOn,
        int chasing,
        int fighting,
        int finished,
        int holding,
        int idle,
        int moved,
        int rooted,
        int richer,
        int flat,
        int sadder,
        int stuck
    )
    {
        sb.Append(seen);
        sb.AppendLine(" bots could be asked three questions. Every case is counted below and none of them is a leftover.");

        if (fresh > 0)
        {
            sb.Append(fresh);
            sb.AppendLine(" more were seen for the first time this window, so there is nothing yet to compare them against. They are counted in nothing below.");
        }

        if (seen == 0)
        {
            sb.AppendLine("Nobody has been watched for a whole window yet, so every answer below would be nought and none of them would mean it.");

            return;
        }

        sb.Append("Did it finish what it took on? ");
        sb.Append(finished);
        sb.Append(" ended a piece of work, ");
        sb.Append(holding);
        sb.Append(" are holding the same one they held two minutes ago, ");
        sb.Append(idle);
        sb.AppendLine(" held nothing to finish.");

        sb.Append("Of the ");
        sb.Append(arrived + short_ + noGoal + chasing);
        sb.Append(" still on the same piece of work, did it get where it was going? ");
        sb.Append(arrived);
        sb.Append(" arrived, ");
        sb.Append(short_);
        sb.Append(" are still short of it, ");
        sb.Append(noGoal);
        sb.Append(" were not going anywhere, ");
        sb.Append(chasing);
        sb.Append(" were chasing something that moves, so there is no fixed place to have reached. The other ");
        sb.Append(movedOn);
        sb.AppendLine(" ended their work in this window, so the destination they had two minutes ago is not a question about them.");

        sb.Append("In a fight this moment: ");
        sb.Append(fighting);
        sb.AppendLine(". None of them can be called stuck by these questions — a bot trading blows fails all four of them while doing exactly what it should.");

        sb.Append("Did anything change? ");
        sb.Append(moved);
        sb.Append(" left the ground they stood on, ");
        sb.Append(rooted);
        sb.Append(" did not; ");
        sb.Append(richer);
        sb.Append(" are better off, ");
        sb.Append(flat);
        sb.Append(" are not; ");
        sb.Append(sadder);
        sb.AppendLine(" are less content than they were.");

        sb.Append("Answered no to all of it: ");
        sb.Append(stuck);
        sb.AppendLine(".");
    }

    private static Mark Take(BotWatch watch, long now, Mark was = null) =>
        new()
        {
            Kind = watch.Kind,
            Takes = watch.Takes,
            Where = watch.Where,
            Goal = watch.Wants,
            Going = watch.WantsAway > 0,
            Worth = watch.Worth,
            Progress = watch.Progress,
            Mood = watch.Mood,
            Stuck = was?.Stuck ?? false,
            Touched = was?.Touched ?? false,
            TouchedTick = was?.TouchedTick ?? 0,
            Reminds = was?.Reminds ?? 0,
            Shakes = was?.Shakes ?? 0
        };

    /// <summary>One line about what the roll-call has done, for the shard's own log.</summary>
    public static string Describe() =>
        Windows == 0
            ? "no roll-call has run yet"
            : $"{Windows} roll-calls, {Stuck} times a bot answered no to all three questions; "
              + $"{Reminded} reminded, {Shaken} shaken, {LeftFighting} left fighting, {LeftResting} resting, {LeftCapped} over the cap; "
              + $"{Freed} were going again by the next window and {StillStuck} were not; {OnPurpose} left to work that stands still on purpose";
}
